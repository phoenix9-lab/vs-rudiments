using Rudiments.SRC.Common.BlockEntities;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// The read half of the placed-ware tier preservation described in
    /// <see cref="BlockEntityBehaviorWareTier"/>: restamps tier and glaze onto whatever the block
    /// hands back when it is picked or broken.
    ///
    /// Both hooks are additive — they let the base implementation build the stack and then edit it,
    /// so container types that put their own data on the drop (a planter's plant, a mold's contents)
    /// are untouched. Blocks whose class overrides <c>OnPickBlock</c> without calling base never
    /// reach this behavior; the storage vessel is the one such case in vanilla and it is handled by
    /// <see cref="BlockWareStorageVessel"/> instead.
    /// </summary>
    internal class BlockBehaviorWareTier : BlockBehavior
    {
        public BlockBehaviorWareTier(Block block) : base(block) { }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorWareTier>();
            if (be == null) return base.OnPickBlock(world, pos, ref handling);

            ItemStack stack = new ItemStack(block, 1);
            be.ApplyTo(stack);

            handling = EnumHandling.PreventDefault;
            return stack;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorWareTier>();
            if (be == null) return null;

            // Roll the block's own drop table, then edit the result in place — molds and planters
            // put their contents in here and we must not throw those away.
            handling = EnumHandling.PreventDefault;

            ItemStack[] drops = BaseDrops(world, pos, byPlayer, dropChanceMultiplier);

            if (drops != null)
            {
                foreach (ItemStack drop in drops)
                {
                    if (drop?.Collectible == block) be.ApplyTo(drop);
                }
            }

            return drops;
        }

        /// <summary>
        /// <c>BreakageIncludesPlacedContainers</c>: widen "use" to opening a placed vessel. Off by
        /// default. When it fires the vessel shatters before it opens rather than after, so there
        /// is never a dialog attached to a block entity that no longer exists.
        /// </summary>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side != EnumAppSide.Server) return false;
            if (!RudimentsModSystem.Config.BreakageIncludesPlacedContainers) return false;

            BlockPos pos = blockSel.Position;
            ItemStack asStack = block.OnPickBlock(world, pos);
            if (asStack == null) return false;

            double chance = WareTier.BreakChance(WareTier.Get(asStack), RudimentsModSystem.Config);
            if (chance <= 0 || world.Rand.NextDouble() >= chance) return false;

            Vec3d center = pos.ToVec3d().Add(0.5, 0.5, 0.5);
            (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer)?.Inventory?.DropAll(center);

            ClayWare.Shatter(world, center, asStack, 1);
            world.BlockAccessor.SetBlock(0, pos);

            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        /// <summary>
        /// Reproduces <c>Block.GetDrops</c>'s default branch (roll the JSON <c>Drops</c> table)
        /// without re-entering behavior dispatch, which would recurse.
        /// </summary>
        private ItemStack[] BaseDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier)
        {
            // Matches Block.GetDrops exactly, null return included. In practice BlockType fills an
            // unspecified `drops` with a single self-drop, so this branch is defensive only.
            if (block.Drops == null) return null;

            var todrop = new System.Collections.Generic.List<ItemStack>();
            foreach (BlockDropItemStack dstack in block.Drops)
            {
                ItemStack stack = dstack.ToRandomItemstackForPlayer(byPlayer, world, dropChanceMultiplier);
                if (stack == null) continue;
                todrop.Add(stack);
                if (dstack.LastDrop) break;
            }

            return todrop.ToArray();
        }
    }
}
