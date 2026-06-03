using Rudiments.SRC.Common.BlockEntities;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    internal class BlockBreak : Block
    {
        private float defaultDuration = 0.5f;
        private int defaultAmount = 1;

        static SimpleParticleProperties flaxShiveParticle = new();


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            defaultDuration = Attributes?["breakDuration"]?.AsFloat(0.5f) ?? 0.5f;
            defaultAmount = Attributes?["breakAmount"]?.AsInt(1) ?? 1;

            flaxShiveParticle = new SimpleParticleProperties(10, 20, ColorUtil.ToRgba(255, 170, 162, 108), new Vec3d(), new Vec3d(), new Vec3f(-1f, -0.25f, -1f), new Vec3f(1f, 1f, 1f), 1f, 1f, 0.1f, 0.2f, EnumParticleModel.Quad);
            flaxShiveParticle.WithTerrainCollision = true;
            flaxShiveParticle.AddPos.Set(0.3f, 0, 0.3f);
            flaxShiveParticle.AddQuantity = 20 * defaultAmount;
            flaxShiveParticle.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack?.Collectible?.Code.Domain == "rudiments" &&
                slot.Itemstack.Collectible.Variant?["type"] == "dried")
            {
                if (world.Side == EnumAppSide.Client)
                {
                    var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBreak;
                    be?.Activate();
                }
                return true;
            }

            

            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack.Collectible.Code.Domain != "rudiments" ||
                slot.Itemstack.Collectible.Variant?["type"] != "dried")
            {
                return false;
            }

            float completed = MathF.Floor(secondsUsed / defaultDuration);

            int alreadyProcessed = byPlayer.Entity.Attributes.GetInt("brokenCount");

            if (world.Side == EnumAppSide.Server)
            {
                if (completed > alreadyProcessed)
                {
                    int amountInStack = slot.StackSize;

                    int finalAmount = defaultAmount;

                    if (amountInStack < defaultAmount)
                        finalAmount = amountInStack;

                    // Carry fibre quality from the dried bundle onto the broken bundle, and stay fibre-agnostic
                    // (flaxbundle-dried -> flaxbundle-broken, nettlebundle-dried -> nettlebundle-broken).
                    int quality = Rudiments.Utils.FiberQuality.Get(slot.Itemstack);
                    var driedCode = slot.Itemstack.Collectible.Code;
                    var brokenCode = driedCode.CopyWithPath(driedCode.Path.Replace("-dried", "-broken"));

                    slot.TakeOut(finalAmount);
                    slot.MarkDirty();

                    Item brokenFlax = world.GetItem(brokenCode);
                    if (brokenFlax != null)
                    {
                        ItemStack stack = new ItemStack(brokenFlax, finalAmount);
                        Rudiments.Utils.FiberQuality.Set(stack, quality);
                        if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                        {
                            world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.85, 0.5));
                        }
                    }

                    flaxShiveParticle.MinPos.Set(blockSel.Position.X + 0.35f, blockSel.Position.Y + 0.9375f, blockSel.Position.Z + 0.35f);
                    api.World.SpawnParticles(flaxShiveParticle);

                    world.PlaySoundAt(new AssetLocation("sounds/block/planks"), byPlayer, null, true, 8f);

                    byPlayer.Entity.Attributes.SetInt("brokenCount", (int)completed);
                }
            }

            if (slot.StackSize <= 0) return false;

            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("brokenCount");

            if (world.Side == EnumAppSide.Client)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBreak;
                be?.Deactivate();
            }

            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("brokenCount");

            if (world.Side == EnumAppSide.Client)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBreak;
                be?.Deactivate();
            }

            base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
            return true;
        }
    }
}
