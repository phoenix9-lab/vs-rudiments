using System;
using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Everything the Rudiments kilns have in common: the fuel gate, the loading and unloading,
    /// the burn timer, and turning greenware into fired ware.
    ///
    /// The fuel gate is the bloomery's, verbatim: <c>BurnTemperature &gt;= 1200 &amp;&amp;
    /// BurnDuration &gt; 30</c>. That admits charcoal, coke, bituminous and anthracite coal, and
    /// refuses lignite, peat and every wood. Cold fuel is refused **at insertion** with an ingame
    /// error rather than accepted and then disappointing, which is how every vanilla fuel gate
    /// behaves — nothing in vanilla lets a player wait ten in-game hours for a worse result.
    ///
    /// Ware admission is the generic <c>SmeltingType == Fire</c> contract rather than a list of
    /// codes, so Clayworks greenware and any other mod's fires here with no compat file at all, and
    /// the output is read off the input block's own combustible properties rather than a hardcoded
    /// ware code. That is why one code path serves every domain.
    ///
    /// Capacity is measured in <b>quarter-tile units</b> rather than slots, which is vanilla's own
    /// ground-storage layout model: a <c>SingleCenter</c> item — a storage vessel, a big crock — is
    /// one whole tile and costs 4, everything else costs 1. A kiln with 8 units therefore holds two
    /// large pieces or eight small ones, or any honest mix, with no capacity table anywhere.
    ///
    /// <b>Lighting is the bloomery's too</b>: hold a torch or firestarter, sneak, and hold right-click.
    /// The block implements <c>IIgnitable</c> and <see cref="CanLight"/> is the silent test the
    /// igniter calls every tick. An earlier build used sneak + right-click on the kiln itself, which
    /// cannot work: with anything in hand the client routes a sneaking right-click to block placement
    /// or the held item and the block never sees it, so the kiln was unlightable unless your hand
    /// happened to be empty.
    /// </summary>
    public abstract class BlockEntityKilnBase : BlockEntity
    {
        protected InventoryGeneric inventory;

        /// <summary>Total game hours at which the current burn finishes. 0 = not lit.</summary>
        protected double burnUntilTotalHours;
        protected bool burning;

        /// <summary>Set for the duration of one firing's output pass. See <see cref="ApplySaltGlaze"/>.</summary>
        private bool salting;

        private AssetLocation insertSound;
        private SimpleParticleProperties fireParticles;

        // --- subclass contract ---
        protected abstract string InvKey { get; }
        protected abstract string LangPrefix { get; }
        /// <summary>Number of ware slots. The fuel slot always follows them.</summary>
        protected abstract int WareSlots { get; }
        /// <summary>Ware capacity in quarter-tile units. 4 = one large piece, 8 = two.</summary>
        protected abstract int WareCapacityUnits { get; }
        protected abstract float BurnHours { get; }

        /// <summary>
        /// Code fragment of a chimney this kiln needs directly above it, or null for kilns that need
        /// none. Read off the blocktype's own <c>chimneyCode</c> attribute rather than a constant, so
        /// the block JSON that places the chimney and the ignition check that requires it can never
        /// disagree. Matched with <c>Code.Path.Contains</c>, the bloomery's own one-line trick.
        /// </summary>
        public string ChimneyCode => Block?.Attributes?["chimneyCode"]?.AsString(null);

        /// <summary>Ware slots, then fuel, then salt. Two service slots on top of the ware.</summary>
        private int SlotCount => WareSlots + 2;

        public int FuelSlotIndex => WareSlots;
        public int SaltSlotIndex => WareSlots + 1;
        public ItemSlot FuelSlot => inventory[FuelSlotIndex];
        public ItemSlot SaltSlot => inventory[SaltSlotIndex];
        public bool IsBurning => burning;

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null) inventory = new InventoryGeneric(SlotCount, InvKey + "-" + Pos, api);

            base.Initialize(api);
            inventory.LateInitialize(InvKey + "-" + Pos, api);

            insertSound = new AssetLocation("game", "sounds/block/ceramicplace");

            if (api.Side == EnumAppSide.Server) RegisterGameTickListener(OnServerTick, 1000);
            else RegisterGameTickListener(OnClientTick, 150);
        }

        // ── Admission ────────────────────────────────────────────────────────────────

        /// <summary>Anything a kiln is supposed to fire, by the generic contract rather than a code list.</summary>
        public static bool IsFirableWare(ItemStack stack)
        {
            return stack?.Collectible?.CombustibleProps?.SmeltingType == EnumSmeltType.Fire;
        }

        /// <summary>The bloomery's fuel gate. See the class summary for what it admits.</summary>
        public static bool IsHotFuel(ItemStack stack)
        {
            CombustibleProperties props = stack?.Collectible?.CombustibleProps;
            if (props == null) return false;
            return props.BurnTemperature >= RudimentsModSystem.Config.KilnMinFuelTemperature && props.BurnDuration > 30;
        }

        /// <summary>
        /// Salt for salt-glazing, by opt-in attribute rather than an item code, so another mod's salt
        /// joins in with a one-line patch. Rudiments stamps <c>rudimentskilnsalt</c> onto
        /// <c>game:salt</c> in <c>patches/glaze-salt.json</c>.
        /// </summary>
        public static bool IsKilnSalt(ItemStack stack)
        {
            return stack?.Collectible?.Attributes?["rudimentskilnsalt"].AsBool(false) == true;
        }

        /// <summary>True for fuel that burns but nowhere near hot enough — firewood, peat, lignite.</summary>
        private static bool IsColdFuel(ItemStack stack)
        {
            CombustibleProperties props = stack?.Collectible?.CombustibleProps;
            return props != null && props.BurnTemperature > 0 && props.BurnDuration > 0;
        }

        /// <summary>
        /// What one piece of this ware costs in quarter-tile units. Read straight off the item's own
        /// ground-storage layout, so a mod's big vessel is large here for the same reason it is
        /// large on the floor.
        /// </summary>
        public static int UnitCost(ItemStack stack)
        {
            var props = stack?.Collectible?.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
            return props != null && props.Layout == EnumGroundStorageLayout.SingleCenter ? 4 : 1;
        }

        public int UsedUnits()
        {
            int used = 0;
            for (int i = 0; i < WareSlots; i++)
            {
                if (!inventory[i].Empty) used += UnitCost(inventory[i].Itemstack) * inventory[i].StackSize;
            }
            return used;
        }

        // ── Interaction ──────────────────────────────────────────────────────────────

        public virtual bool OnInteract(IPlayer byPlayer)
        {
            if (burning)
            {
                Refuse(byPlayer, "burning", LangPrefix + "-burning");
                return true;
            }

            ItemSlot heldSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            ItemStack held = heldSlot?.Itemstack;

            if (held == null) return TryUnload(byPlayer);
            if (IsHotFuel(held)) return TryInsert(byPlayer, heldSlot, FuelSlot, heldSlot.StackSize);
            if (IsKilnSalt(held)) return TryInsert(byPlayer, heldSlot, SaltSlot, heldSlot.StackSize);
            if (IsFirableWare(held)) return TryInsertWare(byPlayer, heldSlot);

            if (IsColdFuel(held))
            {
                Refuse(byPlayer, "coldfuel", LangPrefix + "-coldfuel");
                return true;
            }

            Refuse(byPlayer, "notware", LangPrefix + "-notware");
            return true;
        }

        private bool TryInsertWare(IPlayer byPlayer, ItemSlot heldSlot)
        {
            int cost = UnitCost(heldSlot.Itemstack);
            int room = (WareCapacityUnits - UsedUnits()) / cost;
            if (room <= 0)
            {
                Refuse(byPlayer, "full", LangPrefix + "-full");
                return true;
            }

            int wanted = Math.Min(room, byPlayer.Entity.Controls.CtrlKey ? heldSlot.StackSize : 1);

            for (int i = 0; i < WareSlots; i++)
            {
                ItemSlot slot = inventory[i];
                if (!slot.Empty && slot.Itemstack.Collectible != heldSlot.Itemstack.Collectible) continue;
                if (TryInsert(byPlayer, heldSlot, slot, wanted)) return true;
            }

            Refuse(byPlayer, "full", LangPrefix + "-full");
            return true;
        }

        private bool TryInsert(IPlayer byPlayer, ItemSlot heldSlot, ItemSlot target, int quantity)
        {
            int moved = heldSlot.TryPutInto(Api.World, target, Math.Max(1, quantity));
            if (moved <= 0) return false;

            heldSlot.MarkDirty();
            target.MarkDirty();
            MarkDirty(true);
            Api.World.PlaySoundAt(insertSound, Pos, 0, byPlayer, false);
            return true;
        }

        /// <summary>
        /// Takes <b>one slot</b> out per click — ware first, newest first, and the leftover fuel only
        /// once the chamber is clear. Deliberately not "empty the whole kiln": an empty hand is the
        /// commonest thing to be holding, and one misplaced click should not unload a loaded kiln.
        /// </summary>
        private bool TryUnload(IPlayer byPlayer)
        {
            for (int i = WareSlots - 1; i >= 0; i--)
            {
                if (TryTakeOut(byPlayer, inventory[i])) return true;
            }
            return TryTakeOut(byPlayer, FuelSlot) || TryTakeOut(byPlayer, SaltSlot);
        }

        private bool TryTakeOut(IPlayer byPlayer, ItemSlot slot)
        {
            if (slot.Empty) return false;
            if (!byPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack.Clone())) return false;

            slot.Itemstack = null;
            slot.MarkDirty();
            MarkDirty(true);
            Api.World.PlaySoundAt(insertSound, Pos, 0, byPlayer, false);
            return true;
        }

        // ── Ignition ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether it would light right now, and why not if it would not. Silent and side-effect free:
        /// the igniter calls this on every tick of a held right-click, on both sides.
        /// </summary>
        public bool CanLight(out string langKey)
        {
            langKey = null;

            if (burning) { langKey = LangPrefix + "-burning"; return false; }
            if (FuelSlot.Empty || !IsHotFuel(FuelSlot.Itemstack)) { langKey = LangPrefix + "-nofuel"; return false; }
            if (UsedUnits() == 0) { langKey = LangPrefix + "-noware"; return false; }

            string chimney = ChimneyCode;
            if (chimney != null && Api.World.BlockAccessor.GetBlock(Pos.UpCopy())?.Code?.Path.Contains(chimney) != true)
            {
                langKey = LangPrefix + "-nochimney";
                return false;
            }

            return true;
        }

        /// <summary>Light it. Refuses with a reason rather than failing silently.</summary>
        public virtual bool TryIgnite(IPlayer byPlayer)
        {
            if (!CanLight(out string reason))
            {
                Refuse(byPlayer, "cantlight", reason);
                return false;
            }

            burning = true;
            burnUntilTotalHours = Api.World.Calendar.TotalHours + BurnHours;
            FuelSlot.TakeOut(1);
            FuelSlot.MarkDirty();
            MarkDirty(true);
            return true;
        }

        // ── Firing ───────────────────────────────────────────────────────────────────

        private void OnServerTick(float dt)
        {
            if (!burning) return;
            if (Api.World.Calendar.TotalHours < burnUntilTotalHours) return;

            burning = false;
            burnUntilTotalHours = 0;

            // Salt is thrown in at peak temperature and vapour-glazes everything in the chamber at
            // once — that is the whole point of it, and why one handful does a load where lead needs
            // a nugget per pot. Decided before firing so every slot in this load agrees.
            salting = !SaltSlot.Empty;
            if (salting)
            {
                SaltSlot.TakeOut(1);
                SaltSlot.MarkDirty();
            }

            for (int i = 0; i < WareSlots; i++)
            {
                if (!inventory[i].Empty) FireSlot(inventory[i]);
            }

            salting = false;
            MarkDirty(true);
        }

        /// <summary>
        /// Stamps the salt glaze on a firing's output, if this firing is a salted one. Never overrides
        /// a glaze the piece was already dusted with: a lead-glazed pot that happens to share a kiln
        /// with salt stays lead-glazed.
        /// </summary>
        protected void ApplySaltGlaze(ItemStack fired)
        {
            if (salting && fired != null && WareTier.GetGlaze(fired) == null) WareTier.SetGlaze(fired, "salt");
        }

        /// <summary>Fire at the mouth while it burns. Without this there is no way to tell it is lit.</summary>
        private void OnClientTick(float dt)
        {
            if (!burning) return;

            if (fireParticles == null)
            {
                fireParticles = new SimpleParticleProperties(
                    1, 2,
                    ColorUtil.ToRgba(160, 255, 190, 60),
                    new Vec3d(), new Vec3d(),
                    new Vec3f(-0.05f, 0.05f, -0.05f), new Vec3f(0.05f, 0.25f, 0.05f),
                    0.6f, 0f, 0.3f, 0.7f, EnumParticleModel.Quad)
                {
                    VertexFlags = 128,
                    AddPos = new Vec3d(0.4, 0.15, 0.4)
                };
            }

            fireParticles.MinPos.Set(Pos.X + 0.3, Pos.Y + 0.15, Pos.Z + 0.3);
            Api.World.SpawnParticles(fireParticles);
        }

        /// <summary>
        /// Converts one slot of greenware. The output comes off the input block's own combustible
        /// properties — never a hardcoded ware code. Porcelain needs no special case here: its own
        /// smeltedStack is already shards, because an ordinary firing cannot give it the sealed soak
        /// it wants. Kilns that can do better override this.
        /// </summary>
        protected virtual void FireSlot(ItemSlot slot)
        {
            ItemStack raw = slot.Itemstack;
            ItemStack result = SmeltOne(raw, raw.StackSize);
            if (result == null) return;

            // Glaze rides through on the smelted stack already — BlockGlazableClayware clones it per
            // stack — so only the tier is stamped here.
            WareTier.Set(result, EnumWareTier.Stoneware);
            ApplySaltGlaze(result);

            slot.Itemstack = result;
            slot.MarkDirty();
        }

        /// <summary>The fired form of <paramref name="raw"/>, at the given input count. Null if it does not fire.</summary>
        protected ItemStack SmeltOne(ItemStack raw, int inputCount)
        {
            CombustibleProperties props = raw.Collectible.GetCombustibleProperties(Api.World, raw, null);
            ItemStack fired = props?.SmeltedStack?.ResolvedItemstack;
            if (fired == null) return null;

            ItemStack result = fired.Clone();
            result.StackSize = Math.Max(1, inputCount / Math.Max(1, props.SmeltedRatio));
            return result;
        }

        /// <summary>Puts a stack in the first free ware slot, or drops it at the kiln if there is none.</summary>
        protected void PutOrDrop(ItemStack stack)
        {
            if (stack == null) return;

            for (int i = 0; i < WareSlots; i++)
            {
                if (inventory[i].Empty)
                {
                    inventory[i].Itemstack = stack;
                    inventory[i].MarkDirty();
                    return;
                }
            }

            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        // ── Plumbing ─────────────────────────────────────────────────────────────────

        protected void Refuse(IPlayer byPlayer, string code, string langKey)
        {
            if (langKey == null) return;
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, Lang.Get(langKey));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            // Base first: it is what sets Pos, and Pos is part of the inventory id below.
            base.FromTreeAttributes(tree, worldForResolving);

            if (inventory == null) inventory = new InventoryGeneric(SlotCount, InvKey + "-" + Pos, Api ?? worldForResolving?.Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            burning = tree.GetBool("burning");
            burnUntilTotalHours = tree.GetDouble("burnUntilTotalHours");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetBool("burning", burning);
            tree.SetDouble("burnUntilTotalHours", burnUntilTotalHours);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (burning)
            {
                double left = Math.Max(0, burnUntilTotalHours - Api.World.Calendar.TotalHours);
                dsc.AppendLine(Lang.Get(LangPrefix + "-firing", left));
                return;
            }

            int used = UsedUnits();
            if (used == 0 && FuelSlot.Empty && SaltSlot.Empty)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-empty"));
                return;
            }

            dsc.AppendLine(Lang.Get(LangPrefix + "-loaded", used, WareCapacityUnits));
            dsc.AppendLine(FuelSlot.Empty
                ? Lang.Get(LangPrefix + "-nofuel-info")
                : Lang.Get(LangPrefix + "-fuel", FuelSlot.Itemstack.GetName(), FuelSlot.StackSize));

            if (!SaltSlot.Empty) dsc.AppendLine(Lang.Get("rudiments:kiln-salt", SaltSlot.StackSize));

            // Name whatever is still missing, unless it is the fuel — the line above already said so.
            if (CanLight(out string reason)) dsc.AppendLine(Lang.Get(LangPrefix + "-ready", BurnHours));
            else if (reason != LangPrefix + "-nofuel") dsc.AppendLine(Lang.Get(reason));
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
