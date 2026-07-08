using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Hand cards: interactive carding in the style of the Immersive Fibercraft drop spindle.
    /// Hold washed wool fibers (wool:fibers-*) in the off hand, the cards in the active hand,
    /// and hold right mouse to brush the fibers into a rolag (rudiments:rolag-*).
    /// </summary>
    public class ItemHandCards : Item
    {
        const float CardSeconds = 2f;
        const float StrokesPerSecond = 1.6f;

        WorldInteraction[] interactions;
        readonly Random rand = new Random();

        // One client-side sound handle per player, stopped when the interaction ends —
        // the scrape sample is ~5s long, so fire-and-forget instances would keep ringing
        // well after the ~2s carding action (same management as the IF drop spindle).
        readonly Dictionary<string, ILoadedSound> cardingSounds = new Dictionary<string, ILoadedSound>();

        // fpHandTransform is dead JSON in this engine version — EntityShapeRenderer.RenderHeldItem
        // always requests EnumItemRenderTarget.HandTp/HandTpOff regardless of camera mode (see
        // CollectibleType.FpHandTransform, marked [Obsolete("Use TpHandTransform instead")]), so first
        // and third person render the exact same 3D orientation from tpHandTransform, just through a
        // different camera FOV. Third person reads correctly at the values in handcards.json; up close
        // in first person the same orientation reads as pointing at the floor. Since there's no
        // separate JSON lever for fp-only, this rolls the item 180 degrees about its own forward axis
        // (rotation.Z += 180 — a Z-rotation never moves the Z axis, so this flips the paddle face
        // without changing which way the boards point) but only for this player's own first-person
        // render, leaving the shared tpHandTransform — and everyone else's view of it — untouched.
        // One-shot diagnostic: this override has silently no-op'd twice before (once because
        // fpHandTransform turned out to be dead JSON, once because the InSlot gate was probably never
        // true). If the itemstack-identity gate below is STILL wrong, log exactly which check failed
        // on the very first HandTp call instead of shipping a fourth blind guess.
        static bool loggedOnce = false;

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

            if (target != EnumItemRenderTarget.HandTp) return;

            bool isFp = capi.World.Player?.CameraMode == EnumCameraMode.FirstPerson;
            ItemStack myStack = capi.World.Player?.Entity?.RightHandItemSlot?.Itemstack;
            bool stackMatches = myStack != null && ReferenceEquals(itemstack, myStack);

            if (isFp && !loggedOnce)
            {
                loggedOnce = true;
                capi.Logger.Notification("[Rudiments] handcards fp-pitch check: cameraMode={0}, stackMatches={1}", capi.World.Player?.CameraMode, stackMatches);
            }

            if (!isFp || !stackMatches) return;

            renderinfo.Transform = renderinfo.Transform.Clone();
            renderinfo.Transform.Rotation.Z += 180;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api is not ICoreClientAPI) return;

            interactions = ObjectCacheUtil.GetOrCreate(api, "handCardsInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                foreach (Item item in api.World.Items)
                {
                    if (ResolveRolag(item) != null) stacks.Add(new ItemStack(item));
                }
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:heldhelp-card",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }

        /// <summary>Maps a cardable fiber item (wool:fibers-X) to its rolag (rudiments:rolag-X), or null.</summary>
        Item ResolveRolag(Item fiberItem)
        {
            if (fiberItem?.Code == null || api == null) return null;
            if (fiberItem.Code.Domain != "wool" || !fiberItem.Code.Path.StartsWith("fibers-")) return null;
            return api.World.GetItem(new AssetLocation("rudiments", "rolag-" + fiberItem.Code.Path.Substring("fibers-".Length)));
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!firstEvent) return;

            ItemSlot offSlot = byEntity.LeftHandItemSlot;
            if (offSlot == null || offSlot.Empty || ResolveRolag(offSlot.Itemstack.Item) == null)
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "nofibers", Lang.Get("rudiments:handcards-need-fibers"));
                return;
            }

            if (api is ICoreClientAPI capi && byEntity is EntityPlayer plr && plr.Player != null)
            {
                StopSound(plr.Player.PlayerUID);
                ILoadedSound sound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game", "sounds/player/scrape"),
                    ShouldLoop = true,
                    Position = byEntity.Pos.XYZ.ToVec3f(),
                    DisposeOnFinish = false,
                    Volume = 0.5f,
                    Range = 8f
                });
                sound?.Start();
                if (sound != null) cardingSounds[plr.Player.PlayerUID] = sound;
            }

            handling = EnumHandHandling.PreventDefaultAction;
        }

        void StopSound(string playerUid)
        {
            if (playerUid != null && cardingSounds.TryGetValue(playerUid, out ILoadedSound sound))
            {
                sound?.Stop();
                sound?.Dispose();
                cardingSounds.Remove(playerUid);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                float stroke = GameMath.Sin(secondsUsed * StrokesPerSecond * GameMath.TWOPI);

                // Brushing motion: cycle through the stroke-pose shape alternates, same
                // technique the Immersive Fibercraft drop spindle uses for its spin.
                // 0 = nested idle pair (never shown mid-stroke); 1..3 = working poses with the
                // lower card + fleece web baked in (renderVariant N picks alternates[N-1]):
                // 1 = mid sweep, 2 = full extension, 3 = lifted return with rolag forming.
                // Third person additionally plays the "rudimentscarding" seraph animation
                // (patches/player-carding-anim.json), whose 19-frame cycle matches
                // StrokesPerSecond (1.6/s at 30 fps) so arms and card poses stay in step.
                int variant = stroke < -0.33f ? 3 : stroke < 0.33f ? 1 : 2;
                int prevVariant = slot.Itemstack.TempAttributes.GetInt("renderVariant", 0);
                slot.Itemstack.TempAttributes.SetInt("renderVariant", variant);
                if (variant != prevVariant)
                {
                    (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
                }

                // Wool bundle in the off hand rocks slightly against the strokes
                ModelTransform lf = new ModelTransform();
                lf.EnsureDefaultValues();
                lf.Translation.Set(0.05f * stroke, 0.015f * stroke, 0.04f * stroke);
                lf.Rotation.Set(5f * stroke, 0f, 3f * stroke);
                byEntity.Controls.LeftUsingHeldItemTransformBefore = lf;

                int strokeNo = (int)(secondsUsed * StrokesPerSecond * 2f);
                int prevStroke = slot.Itemstack.TempAttributes.GetInt("cardStroke", -1);
                if (strokeNo != prevStroke)
                {
                    slot.Itemstack.TempAttributes.SetInt("cardStroke", strokeNo);
                    SpawnFluff(byEntity);
                }
            }

            return secondsUsed < CardSeconds;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            slot.Itemstack?.TempAttributes.RemoveAttribute("cardStroke");
            slot.Itemstack?.TempAttributes.RemoveAttribute("renderVariant");
            if (api.Side == EnumAppSide.Client) StopSound((byEntity as EntityPlayer)?.Player?.PlayerUID);

            if (secondsUsed < CardSeconds - 0.1f) return;
            if (api.Side != EnumAppSide.Server) return;

            IPlayer player = (byEntity as EntityPlayer)?.Player;
            ItemSlot offSlot = byEntity.LeftHandItemSlot;
            if (player == null || offSlot?.Itemstack?.Item == null) return;

            Item rolag = ResolveRolag(offSlot.Itemstack.Item);
            if (rolag == null) return;

            offSlot.TakeOut(1);
            offSlot.MarkDirty();

            DamageItem(byEntity.World, byEntity, slot, 1);
            slot.MarkDirty();

            ItemStack outStack = new ItemStack(rolag);
            if (!player.InventoryManager.TryGiveItemstack(outStack))
            {
                byEntity.World.SpawnItemEntity(outStack, byEntity.Pos.XYZ);
            }
            byEntity.World.PlaySoundAt(new AssetLocation("game", "sounds/player/collect"), byEntity, null, true, 8f);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            slot.Itemstack?.TempAttributes.RemoveAttribute("cardStroke");
            slot.Itemstack?.TempAttributes.RemoveAttribute("renderVariant");
            if (api.Side == EnumAppSide.Client) StopSound((byEntity as EntityPlayer)?.Player?.PlayerUID);
            return true;
        }

        void SpawnFluff(EntityAgent byEntity)
        {
            Vec3d pos = byEntity.Pos.XYZ.Add(0, byEntity.LocalEyePos.Y - 0.9, 0)
                .Ahead(0.45, byEntity.Pos.Pitch, byEntity.Pos.Yaw);

            int quantity = 2 + rand.Next(3);
            var particles = new SimpleParticleProperties(
                quantity, quantity,
                ColorUtil.ColorFromRgba(240, 238, 230, 190),
                pos.AddCopy(-0.08, -0.05, -0.08), pos.AddCopy(0.08, 0.05, 0.08),
                new Vec3f(-0.15f, 0.05f, -0.15f), new Vec3f(0.15f, 0.25f, 0.15f),
                0.4f, 0.03f, 0.05f, 0.12f, EnumParticleModel.Quad
            )
            {
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -140f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.05f)
            };
            byEntity.World.SpawnParticles(particles);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
