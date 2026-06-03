using Rudiments.Utils;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    internal class BlockRipple : Block
    {
        private float defaultDuration = 0.5f;
        private int defaultAmount = 1;
        private float defaultFlaxSeedDropAvg = 1.2f;
        private float defaultFlaxGrainDropAvg = 6;
        private float defaultFlaxGrainDropVar = 1;

        static SimpleParticleProperties flaxSeedParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            defaultDuration = Attributes?["rippleDuration"]?.AsFloat(0.5f) ?? 0.5f;
            defaultAmount = Attributes?["rippleAmount"]?.AsInt(1) ?? 1;
            defaultFlaxSeedDropAvg = Attributes?["flaxSeedDropAvg"]?.AsFloat(1.2f) ?? 1.2f;
            defaultFlaxGrainDropAvg = Attributes?["flaxGrainDropAvg"]?.AsFloat(6f) ?? 6f;
            defaultFlaxGrainDropVar = Attributes?["flaxGrainDropVar"]?.AsFloat(1f) ?? 1f;
            
            flaxSeedParticle = new SimpleParticleProperties(0, 1, ColorUtil.ToRgba(255, 73, 58, 9), new Vec3d(), new Vec3d(), new Vec3f(0f, 0f, 0f), new Vec3f(0f, 1f, 0f), 0.4f, 1f, 0.3f, 1f, EnumParticleModel.Cube);
            flaxSeedParticle.WithTerrainCollision = true;
            flaxSeedParticle.AddQuantity = 1 + defaultAmount;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack?.Collectible?.Code.Domain == "rudiments" &&
                slot.Itemstack.Collectible.Code.Path.StartsWith("flaxbundle") &&
                slot.Itemstack.Collectible.Variant?["type"] == "cured")
            {
                return true;
            }

            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (!slot.Itemstack.Collectible.Code.Path.StartsWith("flaxbundle") ||
                slot.Itemstack.Collectible.Variant?["type"] != "cured")
            {
                return false;
            }

            float completed = MathF.Floor(secondsUsed / defaultDuration);

            int alreadyProcessed = byPlayer.Entity.Attributes.GetInt("rippledCount");

            if (world.Side == EnumAppSide.Server)
            {
                if (completed > alreadyProcessed)
                {
                    int amountInStack = slot.StackSize;

                    int finalAmount = defaultAmount;

                    if (amountInStack < defaultAmount)
                        finalAmount = amountInStack;

                    slot.TakeOut(finalAmount);
                    slot.MarkDirty();

                    GiveOrDropItems(new ItemStack(world.GetItem(new AssetLocation("rudiments:flaxbundle-rippled")), finalAmount), world, byPlayer, blockSel);
                    GiveOrDropItems(new ItemStack(world.GetItem(new AssetLocation("game:seeds-flax")), GameMath.RoundRandom(world.Rand,defaultFlaxSeedDropAvg)*finalAmount), world, byPlayer, blockSel);
                    GiveOrDropItems(new ItemStack(world.GetItem(new AssetLocation("game:grain-flax")), GameMath.RoundRandom(world.Rand, (float)MathUtility.NextGaussian(api.World.Rand, defaultFlaxGrainDropAvg, defaultFlaxGrainDropVar))*finalAmount), world, byPlayer, blockSel);

                    var randomSound = api.World.Rand.Next(1,4);
                    world.PlaySoundAt(new AssetLocation($"rudiments:sounds/block/ripple{randomSound}"), byPlayer, null, true, 1f, 0.8f);

                    byPlayer.Entity.Attributes.SetInt("rippledCount", (int)completed);
                }
            }

            if(api.World.Rand.Next(0,5) == 1)
            {
                flaxSeedParticle.AddVelocity.Set(api.World.Rand.Next(-4, 4), 0, api.World.Rand.Next(-4, 4));
                flaxSeedParticle.MinPos.Set(blockSel.Position.X + 0.5f, blockSel.Position.Y + 0.75f, blockSel.Position.Z + 0.5f);
                api.World.SpawnParticles(flaxSeedParticle);
            }


            if (slot.StackSize <= 0) return false;

            return true;
        }

        private void GiveOrDropItems(ItemStack stack ,IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.75, 0.5));
            }
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("rippledCount");

            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("rippledCount");

            base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
            return true;
        }
    }
}
