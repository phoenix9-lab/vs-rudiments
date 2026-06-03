using System;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    internal class BlockHatchel : Block
    {
        private float defaultDuration = 0.5f;
        private int defaultAmount = 1;
        private float defaultFlaxDropAvg = 5;
        private float defaultFlaxDropVar = 2;

        static SimpleParticleProperties flaxTowParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            defaultDuration = Attributes?["hatchelDuration"]?.AsFloat(0.5f) ?? 0.5f;
            defaultAmount = Attributes?["hatchelAmount"]?.AsInt(1) ?? 1;
            defaultFlaxDropAvg = Attributes?["flaxDropAvg"]?.AsFloat(5f) ?? 5f;
            defaultFlaxDropVar = Attributes?["flaxDropVar"]?.AsFloat(2f) ?? 2f;

            flaxTowParticle = new SimpleParticleProperties(1, 3, ColorUtil.ToRgba(255, 170, 162, 108), new Vec3d(), new Vec3d(), new Vec3f(0f, 0f, 0f), new Vec3f(0f, 0.0f, 0f), 0.3f, 0.1f, 0.8f, 1f, EnumParticleModel.Cube);
            flaxTowParticle.WithTerrainCollision = true;
            flaxTowParticle.AddPos.Set(0.3f,0,0.3f);
            flaxTowParticle.AddQuantity = 3 + defaultAmount;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack?.Collectible?.Code.Domain == "rudiments" &&
                slot.Itemstack.Collectible.Variant?["type"] == "scutched")
            {
                return true;
            }

            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack == null) return false;

            if (slot.Itemstack.Collectible.Code.Domain != "rudiments" ||
                slot.Itemstack.Collectible.Variant?["type"] != "scutched")
            {
                return false;
            }

            float completed = MathF.Floor(secondsUsed / defaultDuration);

            int alreadyProcessed = byPlayer.Entity.Attributes.GetInt("hatchedCount");

            if (world.Side == EnumAppSide.Server)
            {
                if (completed > alreadyProcessed)
                {
                    int amountInStack = slot.StackSize;

                    int finalAmount = defaultAmount;

                    if (amountInStack < defaultAmount)
                        finalAmount = amountInStack;

                    // Quality determines which fibre item is produced:
                    //   Coarse (0) → coarsefibers  (rope/cordage use only)
                    //   Standard (1) → game:flaxfibers or rudiments:nettlefiber (vanilla uses)
                    //   Fine (2) → finefibers  (used to craft fine cord → durability bonuses)
                    bool nettle = slot.Itemstack.Collectible.Code.Path.StartsWith("nettlebundle");
                    int quality = FiberQuality.Get(slot.Itemstack);
                    float qualityMul = FiberQuality.YieldMultiplier(quality);

                    AssetLocation fibreCode;
                    if (quality == FiberQuality.Coarse)
                        fibreCode = new AssetLocation("rudiments:coarsefibers");
                    else if (quality == FiberQuality.Fine)
                        fibreCode = new AssetLocation("rudiments:finefibers");
                    else
                        fibreCode = nettle ? new AssetLocation("rudiments:nettlefiber") : new AssetLocation("game:flaxfibers");

                    slot.TakeOut(finalAmount);
                    slot.MarkDirty();

                    float randomDropValue = (float)MathUtility.NextGaussian(api.World.Rand, defaultFlaxDropAvg, defaultFlaxDropVar) * qualityMul;

                    int dropAmount = GameMath.RoundRandom(world.Rand, randomDropValue)*finalAmount;
                    if (dropAmount < finalAmount) dropAmount = finalAmount;

                    Item flaxFiber = world.GetItem(fibreCode);
                    if (flaxFiber != null)
                    {
                        ItemStack stack = new ItemStack(flaxFiber, dropAmount);
                        FiberQuality.Set(stack, quality); // carry quality attr onto fiber output
                        if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                        {
                            world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.75, 0.5));
                        }
                    }

                    var randomSound = api.World.Rand.Next(1, 4);
                    world.PlaySoundAt(new AssetLocation($"rudiments:sounds/block/hatch{randomSound}"), byPlayer, null, true, 1f, 0.7f);

                    byPlayer.Entity.Attributes.SetInt("hatchedCount", (int)completed);
                }
            }

            if (api.World.Rand.Next(0, 5) == 1)
            {
                flaxTowParticle.MinPos.Set(blockSel.Position.X + 0.35f, blockSel.Position.Y + 0.9375f, blockSel.Position.Z + 0.35f);
                api.World.SpawnParticles(flaxTowParticle);
            }

            if (slot.StackSize <= 0) return false;

            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("hatchedCount");
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("hatchedCount");
            base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
            return true;
        }
    }
}
