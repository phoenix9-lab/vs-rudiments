using Rudiments.Utils;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Scutch board — the manual step between breaking and hatcheling. The player holds broken
    /// bundles and right-clicks the board to scrape off the woody shives, producing scutched
    /// bundles. Quality carries through unchanged; the mechscutcher does this step automatically.
    /// </summary>
    internal class BlockScutchBoard : Block
    {
        private float defaultDuration = 0.5f;
        private int defaultAmount = 1;

        static SimpleParticleProperties shiveParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            defaultDuration = Attributes?["scutchDuration"]?.AsFloat(0.5f) ?? 0.5f;
            defaultAmount   = Attributes?["scutchAmount"]?.AsInt(1) ?? 1;

            shiveParticle = new SimpleParticleProperties(
                8, 16,
                ColorUtil.ToRgba(255, 180, 160, 100),
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.8f, -0.2f, -0.8f), new Vec3f(0.8f, 0.9f, 0.8f),
                0.9f, 0.8f, 0.08f, 0.18f,
                EnumParticleModel.Quad);
            shiveParticle.WithTerrainCollision = true;
            shiveParticle.AddPos.Set(0.3f, 0, 0.3f);
            shiveParticle.AddQuantity = 16 * defaultAmount;
            shiveParticle.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            return slot.Itemstack?.Collectible?.Code.Domain == "rudiments"
                && slot.Itemstack.Collectible.Variant?["type"] == "broken";
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack.Collectible.Code.Domain != "rudiments"
                || slot.Itemstack.Collectible.Variant?["type"] != "broken")
            {
                return false;
            }

            float completed     = MathF.Floor(secondsUsed / defaultDuration);
            int alreadyDone     = byPlayer.Entity.Attributes.GetInt("scutchedCount");

            if (world.Side == EnumAppSide.Server && completed > alreadyDone)
            {
                int finalAmount = Math.Min(defaultAmount, slot.StackSize);

                int quality         = FiberQuality.Get(slot.Itemstack);
                var brokenCode      = slot.Itemstack.Collectible.Code;
                var scutchedCode    = brokenCode.CopyWithPath(brokenCode.Path.Replace("-broken", "-scutched"));

                slot.TakeOut(finalAmount);
                slot.MarkDirty();

                Item scutchedItem = world.GetItem(scutchedCode);
                if (scutchedItem != null)
                {
                    ItemStack outStack = new ItemStack(scutchedItem, finalAmount);
                    FiberQuality.Set(outStack, quality);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(outStack))
                        world.SpawnItemEntity(outStack, blockSel.Position.ToVec3d().Add(0.5, 0.75, 0.5));
                }

                shiveParticle.MinPos.Set(
                    blockSel.Position.X + 0.35f,
                    blockSel.Position.Y + 0.9375f,
                    blockSel.Position.Z + 0.35f);
                world.SpawnParticles(shiveParticle);

                world.PlaySoundAt(new AssetLocation("game:sounds/block/planks"), byPlayer, null, true, 8f);

                byPlayer.Entity.Attributes.SetInt("scutchedCount", (int)completed);
            }

            if (slot.StackSize <= 0) return false;
            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("scutchedCount");
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("scutchedCount");
            base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
            return true;
        }
    }
}
