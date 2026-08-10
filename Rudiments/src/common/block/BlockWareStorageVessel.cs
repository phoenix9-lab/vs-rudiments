using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// The storage vessel is the one placeable clay ware whose class overrides both
    /// <c>OnPickBlock</c> and <c>GetDrops</c> without calling base, so
    /// <see cref="BlockBehaviorWareTier"/> can never see it — vanilla
    /// <c>BlockGenericTypedContainer</c> builds a fresh <c>ItemStack</c> and copies only the
    /// container <c>type</c> onto it. This subclass calls base and restamps ware tier and glaze
    /// from <see cref="BlockEntityBehaviorWareTier"/>.
    ///
    /// Patched in via <c>class</c> on <c>game:blocktypes/clay/fired/storagevessel</c>. Nothing else
    /// changes; every other vessel behaviour is inherited untouched.
    /// </summary>
    internal class BlockWareStorageVessel : BlockGenericTypedContainer
    {
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = base.OnPickBlock(world, pos);
            world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorWareTier>()?.ApplyTo(stack);
            return stack;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (drops == null) return null;

            var be = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorWareTier>();
            if (be == null) return drops;

            foreach (ItemStack drop in drops)
            {
                if (drop?.Collectible is BlockWareStorageVessel) be.ApplyTo(drop);
            }

            return drops;
        }
    }
}
