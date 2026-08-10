using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A clay pot that comes out of the fire as the same pot it went in as.
    ///
    /// <c>BlockCookingContainer.DoSmelt</c> builds the cooked pot with
    /// <c>new ItemStack(CodeWithVariant("type", "cooked"), 1)</c> and never looks at the raw pot again,
    /// so a stoneware pot cooked itself back down to earthenware and a lead-glazed one came out of the
    /// firepit clean. It is virtual, which is all that is needed.
    ///
    /// This is where <b>cooked in</b> enters the lead chain: whatever the pot leaches into the stew
    /// travels with the stew from here on, through decanting, serving and finally eating, no matter
    /// how clean the bowl at the end of it is.
    /// </summary>
    internal class BlockWareCookingContainer : BlockCookingContainer
    {
        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            WareKeep pot = WareKeep.Of(inputSlot);
            bool leaded = LeadGlaze.MealIsLeaded(inputSlot?.Itemstack);

            base.DoSmelt(world, cookingSlotsProvider, inputSlot, outputSlot);

            // Ordinary recipes leave the cooked pot in the output slot; a `CooksInto` recipe leaves it
            // in the input slot instead. Restoring both is cheaper than working out which ran, and
            // RestoreTo only writes where the collectible actually changed.
            pot.RestoreTo(outputSlot);
            pot.RestoreTo(inputSlot);

            if (leaded)
            {
                LeadGlaze.CarryTo(outputSlot, true);
                LeadGlaze.CarryTo(inputSlot, true);
            }
        }
    }
}
