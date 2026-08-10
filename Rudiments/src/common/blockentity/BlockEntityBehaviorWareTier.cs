using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Keeps a placed vessel's ware tier and glaze alive across place → break.
    ///
    /// Most fired ware is <c>Unplaceable</c> + <c>GroundStorable</c>, so the real itemstack lives in
    /// a ground-storage inventory and its attributes survive untouched. Five vanilla blocktypes are
    /// genuinely placeable — flowerpot, planter, ingotmold, toolmold, storagevessel — and their
    /// drops are rebuilt from scratch, which would silently turn a stoneware vessel back into
    /// earthenware. This behavior parks the two attributes in the block-entity tree on placement;
    /// <see cref="Rudiments.SRC.Common.Blocks.BlockBehaviorWareTier"/> puts them back on the drop.
    ///
    /// Storing them in the BE tree is also what makes CarryOn work: it serialises the whole tree
    /// when a block is picked up, so a carried vessel keeps its tier with no CarryOn-specific code.
    /// </summary>
    public class BlockEntityBehaviorWareTier : BlockEntityBehavior
    {
        private string tier;
        private string glaze;

        public BlockEntityBehaviorWareTier(BlockEntity blockentity) : base(blockentity) { }

        /// <summary>Stamps this block entity's remembered tier and glaze onto an outgoing stack.</summary>
        public void ApplyTo(ItemStack stack)
        {
            if (stack?.Attributes == null) return;

            if (tier != null) stack.Attributes.SetString(WareTier.AttrKey, tier);
            else stack.Attributes.RemoveAttribute(WareTier.AttrKey);

            if (glaze != null) stack.Attributes.SetString(WareTier.GlazeAttrKey, glaze);
            else stack.Attributes.RemoveAttribute(WareTier.GlazeAttrKey);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            tier = byItemStack?.Attributes?.GetString(WareTier.AttrKey);
            glaze = byItemStack?.Attributes?.GetString(WareTier.GlazeAttrKey);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            tier = tree.GetString(WareTier.AttrKey);
            glaze = tree.GetString(WareTier.GlazeAttrKey);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            if (tier != null) tree.SetString(WareTier.AttrKey, tier);
            if (glaze != null) tree.SetString(WareTier.GlazeAttrKey, glaze);
        }
    }
}
