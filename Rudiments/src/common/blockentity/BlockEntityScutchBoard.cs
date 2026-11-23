using Rudiments.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Interactive scutching. The board holds a batch of broken bundles; the player strikes them
    /// with a scutching sword (left mouse) and flips the bundle halfway through (sneak + right).
    ///
    /// Two per-side boon meters decay asymptotically toward <see cref="SideFloor"/>, which gives the
    /// historically documented diminishing returns for free — "not all the boon will come off with
    /// scutching; the rest is done with hackles". Once a side is clean the blade has no boon cushion
    /// left and starts biting the fibre itself, converting long *line* into short *tow*: the yield
    /// penalty for over-beating is emergent from the same curve, not a separate timer.
    ///
    /// Retting stays the quality ceiling — scutching can only lose what retting granted.
    /// </summary>
    public class BlockEntityScutchBoard : BlockEntity
    {
        /// <summary>Boon each side asymptotes to. Never reaches zero: hackling finishes the job.</summary>
        public const float SideFloor = 0.025f;

        /// <summary>Boon on an untouched side. Both sides start here, so a fresh load is 0% clean.</summary>
        public const float SideStart = 0.5f;

        /// <summary>Total cleanliness below which the batch grades out Coarse.</summary>
        private const float CoarseCeiling = 0.50f;

        /// <summary>Total cleanliness at or above which the batch grades out Fine.</summary>
        private const float FineFloor = 0.775f;

        protected InventoryGeneric inventory;

        private float boonNear = SideStart;
        private float boonFar = SideStart;
        private float integrity = 1f;
        private bool workingFar;
        private int strokes;

        // Hidden per-end toughness, as a multiplier on boon cleared per stroke. Rolled on the server
        // when a load starts and persisted, so a reload or chunk unload never rerolls a batch. Never
        // shown: the flip cue is the only way to tell an end is clean. 1.0 = today's fixed curve,
        // which is also what saves from before 2.4.0 load with.
        private float sideFactorNear = 1f;
        private float sideFactorFar = 1f;

        /// <summary>Gaussian rolls are clamped to mean ± this many standard deviations, and never
        /// below <see cref="MinRollFactor"/>, so a freak roll can't make an end uncleanable.</summary>
        private const float RollClampSigmas = 2.5f;
        private const float MinRollFactor = 0.2f;

        private AssetLocation interactSound;

        // ── Public surface, consumed by BlockScutchBoard and ItemScutchSword ──────────

        public ItemSlot BundleSlot => inventory?[0];
        public bool IsEmpty => BundleSlot?.Empty != false;
        public bool WorkingFar => workingFar;
        public int Strokes => strokes;

        /// <summary>Total cleanliness of the batch, 0 (untouched) to 0.95 (both sides at the floor).</summary>
        public float Cleanliness => GameMath.Clamp(1f - (boonNear + boonFar), 0f, 1f - 2f * SideFloor);

        /// <summary>Fraction of the batch that will come off as long line rather than tow, 0..1.</summary>
        public float Integrity => GameMath.Clamp(integrity, 0f, 1f);

        /// <summary>Cleanliness of the side currently facing the player, 0..1. Damage is keyed to this,
        /// not to the total — the blade only bites bare fibre where the boon cushion is already gone.</summary>
        public float LocalCleanliness => LocalCleanlinessOf(workingFar);

        /// <summary>The tell: the worked side is clean, so further strokes shred line into tow. Doubles
        /// as the cue to flip, which is why one signal can teach both lessons.</summary>
        public bool BladeOnBareFiber => LocalCleanliness >= RudimentsModSystem.Config.ScutchSafeCleanliness;

        // ── Lifecycle ────────────────────────────────────────────────────────────────

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null) inventory = new InventoryGeneric(1, "scutchboard-" + Pos, api);
            base.Initialize(api);
            inventory.LateInitialize("scutchboard-" + Pos, api);

            interactSound = new AssetLocation("game", "sounds/block/planks");
        }

        /// <summary>Nettle's coarse, tangled stems don't care how fine the notch is cut, so a batch
        /// isn't bottlenecked by board tier — the whole stack can go on at once. Flax still respects
        /// the board's craftsmanship cap, scaled by <c>ScutchBoardCapacityMultiplier</c> and never more
        /// than a full stack.</summary>
        private int CapacityFor(ItemStack stack)
        {
            if (IsNettle(stack)) return stack.Collectible.MaxStackSize;

            float mult = Math.Max(0f, RudimentsModSystem.Config.ScutchBoardCapacityMultiplier);
            int capacity = Math.Max(1, (int)Math.Round(BaseFlaxCapacity * mult, MidpointRounding.AwayFromZero));
            int maxStack = stack?.Collectible?.MaxStackSize ?? 0;
            return maxStack > 0 ? Math.Min(capacity, maxStack) : capacity;
        }

        /// <summary>The tier's own flax capacity from the block attributes, before the config multiplier.</summary>
        private int BaseFlaxCapacity => Math.Max(1, Block?.Attributes?["scutchCapacity"]?.AsInt(4) ?? 4);

        private int Capacity => CapacityFor(BundleSlot?.Itemstack);

        /// <summary>Per-stroke share of the remaining boon this board knocks free. Craftsmanship in the
        /// notch scales it (a better-cut notch holds the bundle steadier); nettle divides it down; a
        /// flax batch beyond the tier's base capacity is spread thinner under the blade. The hidden
        /// per-end toughness and per-stroke jitter are applied in <see cref="ApplyStroke"/>.</summary>
        private float BoonPerStroke
        {
            get
            {
                RudimentsConfig cfg = RudimentsModSystem.Config;
                ItemStack batch = BundleSlot?.Itemstack;
                float k = (Block?.Attributes?["boonPerStroke"]?.AsFloat(0.16f) ?? 0.16f)
                        * cfg.ScutchBoonPerStrokeMultiplier;

                if (IsNettle(batch))
                {
                    float div = Math.Max(0.01f, cfg.ScutchNettleBoonMultiplier);
                    k /= div;
                }
                else if (batch != null && cfg.ScutchLargeBatchEffort > 0f && batch.StackSize > BaseFlaxCapacity)
                {
                    // Only loads above the tier's base capacity pay, so with the default capacity
                    // multiplier this never fires and the curve is exactly the pre-2.4.0 one.
                    k *= (float)Math.Pow((double)BaseFlaxCapacity / batch.StackSize, cfg.ScutchLargeBatchEffort);
                }

                return GameMath.Clamp(k, 0.001f, 0.95f);
            }
        }

        /// <summary>A Gaussian multiplier around 1.0, clamped to 1 ± <see cref="RollClampSigmas"/>
        /// standard deviations and at least <see cref="MinRollFactor"/>. Exactly 1 when
        /// <paramref name="stdDev"/> is 0 (or negative), so a zero setting restores the fixed curve.</summary>
        private float RollFactor(float stdDev)
        {
            if (!(stdDev > 0f) || Api?.World?.Rand == null) return 1f;

            float spread = RollClampSigmas * stdDev;
            float roll = (float)MathUtility.NextGaussian(Api.World.Rand, 1.0, stdDev);
            return GameMath.Clamp(roll, Math.Max(MinRollFactor, 1f - spread), 1f + spread);
        }

        private static bool IsNettle(ItemStack stack) =>
            stack?.Collectible?.Code != null && stack.Collectible.Code.Path.StartsWith("nettlebundle");

        private static bool IsBroken(ItemStack stack) =>
            stack?.Collectible?.Code != null
            && stack.Collectible.Code.Domain == "rudiments"
            && stack.Collectible.Variant?["type"] == "broken";

        /// <summary>Loading is validated server-side, so refusals have to be pushed back to the
        /// player's client rather than raised locally.</summary>
        private void Refuse(IPlayer byPlayer, string code, string langKey)
        {
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, Lang.Get(langKey));
            else (Api as ICoreClientAPI)?.TriggerIngameError(this, code, Lang.Get(langKey));
        }

        private float LocalCleanlinessOf(bool far)
        {
            float boon = far ? boonFar : boonNear;
            return GameMath.Clamp((SideStart - boon) / (SideStart - SideFloor), 0f, 1f);
        }

        // ── Player actions ───────────────────────────────────────────────────────────

        /// <summary>Right-click with broken bundles: transfer as many as the board can hold.</summary>
        public bool TryLoad(IPlayer byPlayer)
        {
            ItemSlot heldSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            ItemStack held = heldSlot?.Itemstack;
            if (!IsBroken(held)) return false;

            ItemSlot slot = BundleSlot;
            if (slot == null) return false;

            if (!slot.Empty)
            {
                // Adding to a batch that has already been worked would launder unscutched bundles
                // through a finished one — the meters are per session, not per bundle.
                if (strokes > 0)
                {
                    Refuse(byPlayer, "inprogress", "rudiments:scutchboard-inprogress");
                    return false;
                }

                if (!slot.Itemstack.Collectible.Code.Equals(held.Collectible.Code)
                    || FiberQuality.Get(slot.Itemstack) != FiberQuality.Get(held))
                {
                    Refuse(byPlayer, "wrongquality", "rudiments:scutchboard-wrongquality");
                    return false;
                }
            }

            int capacity = CapacityFor(slot.Empty ? held : slot.Itemstack);
            int room = capacity - (slot.Empty ? 0 : slot.Itemstack.StackSize);
            if (room <= 0)
            {
                Refuse(byPlayer, "full", "rudiments:scutchboard-full");
                return false;
            }

            int transferable = Math.Min(held.StackSize, room);
            if (transferable <= 0) return false;

            if (slot.Empty)
            {
                slot.Itemstack = held.Clone();
                slot.Itemstack.StackSize = transferable;
                ResetProgress();
            }
            else
            {
                slot.Itemstack.StackSize += transferable;
            }

            heldSlot.TakeOut(transferable);
            heldSlot.MarkDirty();
            slot.MarkDirty();
            MarkDirty(true);

            Api.World.PlaySoundAt(interactSound, byPlayer, null, false, 8f, 0.8f);
            return true;
        }

        /// <summary>Sneak + right-click: turn the bundle so the far end faces you.</summary>
        public bool Flip(IPlayer byPlayer)
        {
            if (IsEmpty) return false;

            workingFar = !workingFar;
            MarkDirty(true);

            Api.World.PlaySoundAt(new AssetLocation("game", "sounds/block/leafy-picking"), byPlayer, null, false, 8f, 0.9f);
            return true;
        }

        /// <summary>Right-click empty-handed: grade the batch and hand it over.</summary>
        public bool Collect(IPlayer byPlayer)
        {
            ItemSlot slot = BundleSlot;
            if (slot == null || slot.Empty) return false;

            ItemStack batch = slot.Itemstack;
            int loadedCount = batch.StackSize;

            AssetLocation brokenCode = batch.Collectible.Code;
            AssetLocation scutchedCode = brokenCode.CopyWithPath(brokenCode.Path.Replace("-broken", "-scutched"));
            Item scutchedItem = Api.World.GetItem(scutchedCode);
            if (scutchedItem == null) return false;

            // Retting is never overridden — scutching can only lose what retting granted.
            int outQuality = Math.Min(FiberQuality.Get(batch), GradeFor(Cleanliness));

            RudimentsConfig cfg = RudimentsModSystem.Config;

            // Two sources of tow. Over-scutch damage turns whole bundles into tow (and costs line).
            // Cleaning itself always shakes short fibre out with the boon: extra tow in proportion
            // to how clean the batch got, which costs no line. So stopping early saves no tow and
            // still grades lower: a proper clean beats an under-scutched one on every count.
            int lineCount = GameMath.Clamp(
                GameMath.RoundRandom(Api.World.Rand, loadedCount * Integrity), 0, loadedCount);
            int damagedCount = loadedCount - lineCount;
            int perBundle = Math.Max(0, cfg.ScutchTowFibersPerBundle);
            float baselinePerBundle = Math.Max(0f, cfg.ScutchBaselineTowPerBundle);
            int baselineTow = GameMath.RoundRandom(Api.World.Rand, loadedCount * Cleanliness * baselinePerBundle);
            int towFibres = damagedCount * perBundle + baselineTow;

            slot.Itemstack = null;
            slot.MarkDirty();
            ResetProgress();
            MarkDirty(true);

            Vec3d dropPos = Pos.ToVec3d().Add(0.5, 0.9, 0.5);

            if (lineCount > 0)
            {
                ItemStack lineStack = new ItemStack(scutchedItem, lineCount);
                FiberQuality.Set(lineStack, outQuality);
                if (!byPlayer.InventoryManager.TryGiveItemstack(lineStack))
                    Api.World.SpawnItemEntity(lineStack, dropPos);
            }

            ItemStack towStack = FiberQuality.TowStack(Api.World, towFibres);
            if (towStack != null && !byPlayer.InventoryManager.TryGiveItemstack(towStack))
                Api.World.SpawnItemEntity(towStack, dropPos);

            Api.World.PlaySoundAt(new AssetLocation("game", "sounds/player/collect"), byPlayer, null, false, 8f);
            return true;
        }

        /// <summary>
        /// One sword stroke. SERVER ONLY — the meters are authoritative here and pushed to clients
        /// with <see cref="BlockEntity.MarkDirty"/>, so no client keeps its own copy. Callers read
        /// <see cref="BladeOnBareFiber"/> afterwards to pick particles and strike pitch.
        /// </summary>
        public bool ApplyStroke(IPlayer byPlayer)
        {
            if (IsEmpty) return false;

            RudimentsConfig cfg = RudimentsModSystem.Config;

            // Each end clears at its own hidden rate, and every stroke lands a little differently,
            // so the right stroke count changes from load to load and end to end. The bleed onto
            // the held end follows that end's toughness, not the worked one's.
            float k = BoonPerStroke * RollFactor(cfg.ScutchStrokeJitter);
            float bleedShare = GameMath.Clamp(cfg.ScutchCrossSideBleed, 0f, 1f);
            float kWorked = GameMath.Clamp(k * (workingFar ? sideFactorFar : sideFactorNear), 0f, 0.95f);
            float kHeld = GameMath.Clamp(k * bleedShare * (workingFar ? sideFactorNear : sideFactorFar), 0f, 0.95f);

            if (workingFar)
            {
                boonFar -= kWorked * (boonFar - SideFloor);
                boonNear -= kHeld * (boonNear - SideFloor);
            }
            else
            {
                boonNear -= kWorked * (boonNear - SideFloor);
                boonFar -= kHeld * (boonFar - SideFloor);
            }

            boonNear = GameMath.Clamp(boonNear, SideFloor, SideStart);
            boonFar = GameMath.Clamp(boonFar, SideFloor, SideStart);
            strokes++;

            // Damage is keyed to the worked side's cleanliness *after* the stroke: under-scutching
            // costs nothing, and past the safe point the cost ramps to the full per-stroke figure.
            float safe = GameMath.Clamp(cfg.ScutchSafeCleanliness, 0f, 0.999f);
            float localClean = LocalCleanliness;
            if (localClean > safe)
            {
                integrity -= cfg.ScutchDamagePerStroke * (localClean - safe) / (1f - safe);
                integrity = GameMath.Clamp(integrity, 0f, 1f);
            }

            MarkDirty();
            return true;
        }

        private static int GradeFor(float cleanliness)
        {
            if (cleanliness < CoarseCeiling) return FiberQuality.Coarse;
            if (cleanliness < FineFloor) return FiberQuality.Standard;
            return FiberQuality.Fine;
        }

        private void ResetProgress()
        {
            boonNear = SideStart;
            boonFar = SideStart;
            integrity = 1f;
            workingFar = false;
            strokes = 0;

            // Loading and collecting only run on the server (BlockScutchBoard returns early on the
            // client), but guard anyway: a client must never roll its own copy.
            bool server = Api?.Side == EnumAppSide.Server;
            float variance = RudimentsModSystem.Config.ScutchSideVariance;
            sideFactorNear = server ? RollFactor(variance) : 1f;
            sideFactorFar = server ? RollFactor(variance) : 1f;
        }

        // ── Info text ────────────────────────────────────────────────────────────────

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (IsEmpty)
            {
                dsc.AppendLine(Lang.Get("rudiments:scutchboard-empty"));
                return;
            }

            ItemStack batch = BundleSlot.Itemstack;
            dsc.AppendLine(Lang.Get("rudiments:scutchboard-loaded", batch.StackSize, batch.GetName(), Capacity));
            dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(FiberQuality.Get(batch))));
            dsc.AppendLine(Lang.Get(workingFar ? "rudiments:scutchboard-side-far" : "rudiments:scutchboard-side-near"));

            bool showMeters = RudimentsModSystem.Config.ScutchShowMeters;
            if (showMeters)
            {
                dsc.AppendLine(Lang.Get("rudiments:scutchboard-cleanliness", (int)Math.Round(Cleanliness * 100f)));
                dsc.AppendLine(Lang.Get("rudiments:scutchboard-integrity", (int)Math.Round(Integrity * 100f)));
            }
            else
            {
                dsc.AppendLine(Lang.Get("rudiments:scutchboard-cleanliness-vague", Vague(Cleanliness / 0.95f)));
                dsc.AppendLine(Lang.Get("rudiments:scutchboard-integrity-vague", Vague(Integrity)));
            }

            dsc.AppendLine(Lang.Get("rudiments:scutchboard-grade",
                FiberQuality.Name(Math.Min(FiberQuality.Get(batch), GradeFor(Cleanliness)))));

            // The tell fires on the worked side. While the other half is still filthy it reads as the
            // instruction to flip; once both halves are clean there is nothing left to flip to, so the
            // same signal switches to telling the player to stop and collect — otherwise the only
            // in-game cue goes quiet exactly when over-scutching starts turning line into tow.
            if (BladeOnBareFiber)
            {
                bool otherSideDone = LocalCleanlinessOf(!workingFar) >= RudimentsModSystem.Config.ScutchSafeCleanliness;
                dsc.AppendLine(Lang.Get(otherSideDone
                    ? "rudiments:scutchboard-donewarning"
                    : "rudiments:scutchboard-flipprompt"));
            }
        }

        /// <summary>Maps a 0..1 meter onto one of the four qualitative words used when
        /// <c>ScutchShowMeters</c> is off.</summary>
        private static string Vague(float value)
        {
            int bucket = GameMath.Clamp((int)(GameMath.Clamp(value, 0f, 1f) * 4f), 0, 3);
            return Lang.Get("rudiments:scutchboard-vague-" + bucket);
        }

        // ── Rendering ────────────────────────────────────────────────────────────────

        private MeshData bundleMesh;
        private string bundleMeshKey;

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            bool skipDefault = base.OnTesselation(mesher, tesselator);

            if (Api is not ICoreClientAPI capi || IsEmpty) return skipDefault;

            string key = BundleSlot.Itemstack.Collectible.Code + "|" + workingFar;
            if (bundleMesh == null || bundleMeshKey != key)
            {
                bundleMesh = GenBundleMesh(capi, BundleSlot.Itemstack, workingFar);
                bundleMeshKey = key;
            }

            if (bundleMesh != null) mesher.AddMeshData(bundleMesh);
            return skipDefault;
        }

        /// <summary>
        /// Drapes the loaded bundle's own item shape into the board's notch, mirrored end-for-end when
        /// the far side is being worked. Using the item's shape means flax and nettle both render with
        /// no new assets, and any future bundle art follows automatically.
        /// </summary>
        private MeshData GenBundleMesh(ICoreClientAPI capi, ItemStack stack, bool far)
        {
            if (stack?.Item?.Shape == null) return null;

            // TesselateItem's 2-arg overload resolves UVs against the item texture atlas, but this
            // mesh is baked straight into the chunk's terrain mesh, which is drawn against the block
            // atlas. Without remapping through a texture source backed by BlockTextureAtlas, the UVs
            // point at whatever happens to sit at those coordinates in the wrong atlas — the bundle
            // tesselates without error but never actually appears. Same fix vanilla tool racks and
            // display cases use for contained items baked into terrain mesh.
            Dictionary<string, AssetLocation> textures = new Dictionary<string, AssetLocation>();
            foreach (var kv in stack.Item.Textures) textures[kv.Key] = kv.Value.Baked.BakedName;
            ITexPositionSource texSource = new ContainedTextureSource(
                capi, capi.BlockTextureAtlas, textures, "scutchboard bundle " + stack.Collectible.Code);

            MeshData mesh;
            try
            {
                capi.Tesselator.TesselateItem(stack.Item, out mesh, texSource);
            }
            catch (Exception e)
            {
                Api.World.Logger.Warning("[rudiments] Failed to tesselate scutch board bundle mesh for {0}: {1}", stack.Collectible.Code, e);
                return null;
            }
            if (mesh == null) return null;

            for (int p = 0; p < mesh.RenderPassesAndExtraBits.Length; p++)
            {
                mesh.RenderPassesAndExtraBits[p] = (short)EnumChunkRenderPass.BlendNoCull;
            }

            Vec3f pivot = new Vec3f(0.5f, 0f, 0.5f);

            // Tip it so the worked end hangs down the near face of the board, then mirror the whole
            // thing when the bundle has been turned.
            mesh.Rotate(pivot, -22f * GameMath.DEG2RAD, 0f, 0f);
            if (far) mesh.Rotate(pivot, 0f, GameMath.PI, 0f);

            // Into the notch: the sawn gap sits at x 9..12, y 17..18 of the 16-unit shape grid.
            mesh.Scale(pivot, 0.9f, 0.9f, 0.9f);
            mesh.Translate(0.15f, 1.02f, 0f);

            // Finally follow the placed block's own facing.
            float rotateY = Block?.Shape?.rotateY ?? 0f;
            if (rotateY != 0f) mesh.Rotate(pivot, 0f, rotateY * GameMath.DEG2RAD, 0f);

            return mesh;
        }

        // ── Persistence ──────────────────────────────────────────────────────────────

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null) inventory = new InventoryGeneric(1, "scutchboard-" + Pos, Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            boonNear = tree.GetFloat("boonNear", SideStart);
            boonFar = tree.GetFloat("boonFar", SideStart);
            integrity = tree.GetFloat("integrity", 1f);
            workingFar = tree.GetBool("workingFar");
            strokes = tree.GetInt("strokes");
            // Saves from before 2.4.0 have no rolls: they keep the fixed curve until the next load.
            sideFactorNear = tree.GetFloat("sideFactorNear", 1f);
            sideFactorFar = tree.GetFloat("sideFactorFar", 1f);

            // The server pushes new meters every stroke, so this runs often on the client; the mesh
            // cache key covers the only two things that can change what is drawn.
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetFloat("boonNear", boonNear);
            tree.SetFloat("boonFar", boonFar);
            tree.SetFloat("integrity", integrity);
            tree.SetBool("workingFar", workingFar);
            tree.SetInt("strokes", strokes);
            tree.SetFloat("sideFactorNear", sideFactorNear);
            tree.SetFloat("sideFactorFar", sideFactorFar);
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server && inventory != null)
            {
                foreach (ItemSlot slot in inventory)
                {
                    if (!slot.Empty) Api.World.SpawnItemEntity(slot.Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
