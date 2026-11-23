using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Fine-textile behaviour for vanilla linen (a block). Same as the <c>fabric</c> role of
    /// <see cref="Rudiments.SRC.Common.Items.CollectibleBehaviorFineTextile"/>, plus: fine linen
    /// refuses to be laid down. Linen is placed as decor, and decor keeps no attributes, so a fine
    /// roll would come back plain. Patched in at index 0 so this runs before <c>Decor</c>.
    /// </summary>
    public class BlockBehaviorFineLinen : BlockBehavior, IFineTextile
    {
        public FineRole Role => FineRole.Fabric;

        public BlockBehaviorFineLinen(Block block) : base(block) { }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            if (FineTextile.IsFine(itemstack))
            {
                handling = EnumHandling.PreventSubsequent;
                failureCode = "rudiments-fine-linen-noplace";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling)
        {
            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
            FineTextile.ApplyCraftMark(allInputSlots, outputSlot);
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            base.GetHeldItemName(sb, itemStack);
            FineTextile.WrapName(sb, itemStack);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            FineTextile.AppendInfo(inSlot, dsc);
        }
    }
}
