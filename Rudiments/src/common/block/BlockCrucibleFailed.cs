using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A crucible that cracked mid-smelt (see <c>BlockEntityBehaviorSmeltingFailure</c>). Inert — it
    /// no longer accepts ore or pours metal — and carries the same "output"/"units" stack attributes
    /// a working <c>BlockSmeltedContainer</c> does, so the pending recovery travels with the item
    /// until <see cref="Rudiments.SRC.Common.Items.CollectibleBehaviorCrucibleCrack"/> pays it out.
    /// </summary>
    public class BlockCrucibleFailed : Block
    {
        public void SetContents(ItemStack stack, ItemStack output, int units)
        {
            stack.Attributes.SetItemstack("output", output);
            stack.Attributes.SetInt("units", units);
        }

        public KeyValuePair<ItemStack, int> GetContents(IWorldAccessor world, ItemStack stack)
        {
            ItemStack outstack = stack.Attributes.GetItemstack("output");
            outstack?.ResolveBlockOrItem(world);
            return new KeyValuePair<ItemStack, int>(outstack, stack.Attributes.GetInt("units"));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            KeyValuePair<ItemStack, int> contents = GetContents(world, inSlot.Itemstack);

            if (contents.Key != null)
            {
                dsc.AppendLine(Lang.GetWithFallback("rudiments:crucible-failed-blockinfo",
                    "Cracked open — needs a hammer and chisel to work loose whatever {0} survived inside.",
                    contents.Key.GetName().ToLower()));
            }

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
