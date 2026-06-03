using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Seed oil press. Hold flax seeds against it to slowly crush them into linseed oil, leaving a pressed
    /// seed cake behind. Tiers (primitive/advanced via JSON attributes) press more seeds per stroke and
    /// waste fewer. Consistent with the mod's other "hold right-mouse on the tool" interactions.
    /// </summary>
    internal class BlockOilPress : Block
    {
        private float pressDuration = 0.8f;
        private int seedsPerPress = 3;
        private float cakeChance = 0.35f;

        static SimpleParticleProperties oilParticle = new();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            pressDuration = Attributes?["pressDuration"]?.AsFloat(0.8f) ?? 0.8f;
            seedsPerPress = Attributes?["seedsPerPress"]?.AsInt(3) ?? 3;
            cakeChance = Attributes?["cakeChance"]?.AsFloat(0.35f) ?? 0.35f;

            oilParticle = new SimpleParticleProperties(2, 5, ColorUtil.ToRgba(220, 196, 168, 64), new Vec3d(), new Vec3d(), new Vec3f(-0.5f, 0f, -0.5f), new Vec3f(0.5f, 0.2f, 0.5f), 1f, 0.6f, 0.1f, 0.2f, EnumParticleModel.Cube);
            oilParticle.WithTerrainCollision = true;
            oilParticle.AddPos.Set(0.3f, 0, 0.3f);
        }

        private bool IsFlaxSeeds(ItemSlot slot)
        {
            var code = slot?.Itemstack?.Collectible?.Code;
            return code != null && code.Domain == "game" && code.Path.StartsWith("seeds-flax");
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return IsFlaxSeeds(byPlayer.InventoryManager.ActiveHotbarSlot);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!IsFlaxSeeds(slot)) return false;

            float completed = MathF.Floor(secondsUsed / pressDuration);
            int alreadyProcessed = byPlayer.Entity.Attributes.GetInt("pressedCount");

            if (world.Side == EnumAppSide.Server && completed > alreadyProcessed)
            {
                int consume = Math.Min(seedsPerPress, slot.StackSize);
                if (consume <= 0) return false;

                slot.TakeOut(consume);
                slot.MarkDirty();

                // Oil scales with how many seeds we managed to crush this stroke.
                int oilAmount = Math.Max(1, consume * 2 / Math.Max(1, seedsPerPress));
                GiveOrDrop(world, byPlayer, blockSel, new AssetLocation("rudiments:linseedoil"), oilAmount);

                if (world.Rand.NextDouble() < cakeChance)
                {
                    GiveOrDrop(world, byPlayer, blockSel, new AssetLocation("rudiments:linseedcake"), 1);
                }

                oilParticle.MinPos.Set(blockSel.Position.X + 0.35f, blockSel.Position.Y + 0.85f, blockSel.Position.Z + 0.35f);
                api.World.SpawnParticles(oilParticle);

                world.PlaySoundAt(new AssetLocation("game:sounds/block/squeeze"), byPlayer, null, true, 8f, 0.8f);

                byPlayer.Entity.Attributes.SetInt("pressedCount", (int)completed);
            }

            return slot.StackSize > 0;
        }

        private void GiveOrDrop(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, AssetLocation code, int amount)
        {
            Item item = world.GetItem(code);
            if (item == null || amount <= 0) return;
            ItemStack stack = new ItemStack(item, amount);
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.85, 0.5));
            }
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("pressedCount");
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.Attributes.RemoveAttribute("pressedCount");
            base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
            return true;
        }
    }
}
