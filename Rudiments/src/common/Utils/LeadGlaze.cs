using Rudiments.SRC.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// The one place that answers "is this leaded, and did it get into the food".
    ///
    /// Lead glaze is not a lesser version of the other two. It is the cheapest glaze in the game, it
    /// works in a pit kiln on day one, and it costs nothing but a galena nugget — so without a price
    /// there is no reason to ever use anything else, and tin and salt are decoration. The price is
    /// that lead leaches into whatever the vessel is holding.
    ///
    /// <b>The lead travels with the food, not with the pot.</b> Cook a stew in a leaded pot, decant it
    /// into a crock, serve it into a spotless porcelain bowl, and the stew is still leaded. That is
    /// carried by a single boolean stack attribute, <c>rudimentslead</c>, stamped on whatever is
    /// holding the food at each hand-off, and it is why a player cannot launder a contaminated meal by
    /// changing plates.
    ///
    /// Two carriers, because food is stored two different ways:
    /// <list type="bullet">
    /// <item><b>Meals</b> mark the vessel stack — a meal has no single "contents" object, it has an
    /// array of ingredients, and marking those would stop them merging.</item>
    /// <item><b>Liquids</b> mark the content portion itself, which is the thing that actually moves
    /// between containers, so it propagates through barrels and buckets on its own.</item>
    /// </list>
    /// Both clear themselves: eat the meal and the bowl reverts to an unmarked empty bowl; drink the
    /// water and the marked portion is gone with it.
    ///
    /// The attribute is registered in <c>GlobalConstants.IgnoredStackAttributes</c> at startup, so it
    /// can never refuse a merge, a liquid top-up or a recipe match. The visible consequence is that
    /// mixing leaded liquid into a larger clean one dilutes it away rather than contaminating it,
    /// which is the forgiving reading and avoids a baffling "these two waters will not combine".
    /// </summary>
    public static class LeadGlaze
    {
        /// <summary>The glaze value that poisons. Salt and tin do not, which is their entire purpose.</summary>
        public const string ToxicGlaze = "lead";

        /// <summary>Boolean stack attribute: what this is holding came out of lead.</summary>
        public const string MarkKey = "rudimentslead";

        public static bool IsToxic(string glaze) => glaze == ToxicGlaze;

        /// <summary>True if the vessel itself is lead-glazed, so it contaminates whatever it holds.</summary>
        public static bool IsToxic(ItemStack stack) => IsToxic(WareTier.GetGlaze(stack));

        public static bool IsMarked(ItemStack stack) => stack?.Attributes?.GetBool(MarkKey) == true;

        public static void Mark(ItemStack stack)
        {
            if (stack?.Attributes == null) return;
            if (!RudimentsModSystem.LeadPoisoningEnabled) return;

            stack.Attributes.SetBool(MarkKey, true);
        }

        /// <summary>Whether a meal in this vessel is leaded — either the vessel leaches into it, or it
        /// arrived that way from somewhere earlier in the chain.</summary>
        public static bool MealIsLeaded(ItemStack vessel) => IsToxic(vessel) || IsMarked(vessel);

        /// <summary>Whether a drink from this vessel is leaded. The mark rides the liquid rather than
        /// the cup, so both have to be asked.</summary>
        public static bool DrinkIsLeaded(ItemStack vessel, ItemStack content) => IsToxic(vessel) || IsMarked(content);

        /// <summary>The liquid portion inside a container stack, or null if it is not one.</summary>
        public static ItemStack ContentOf(ItemStack vessel)
        {
            return vessel?.Block is BlockLiquidContainerBase liquid ? liquid.GetContent(vessel) : null;
        }

        /// <summary>
        /// Passes the contamination on to whatever the food was just decanted into. Only ever marks a
        /// vessel that is actually holding a meal, so a serve that did nothing marks nothing.
        /// </summary>
        public static void CarryTo(ItemSlot destination, bool leaded)
        {
            if (!leaded) return;

            ItemStack stack = destination?.Itemstack;
            if (stack?.Block is not IBlockMealContainer) return;

            Mark(stack);
            destination.MarkDirty();
        }

        /// <summary>
        /// Whether food or drink actually passes through this vessel. A lead-glazed flowerpot is
        /// harmless and should not be labelled as though it were not, so the tooltip warning and the
        /// exposure itself both ask this rather than assuming every glazed thing is a cup.
        /// </summary>
        public static bool IsConsumptionVessel(ItemStack stack)
        {
            Block block = stack?.Block;
            if (block == null) return false;
            if (block is IBlockMealContainer || block is BlockLiquidContainerBase) return true;
            return block.Attributes?.IsTrue("mealContainer") == true;
        }

        /// <summary>True if this stack should carry the "the ware itself is lead" warning.</summary>
        public static bool Warns(ItemStack stack)
        {
            return RudimentsModSystem.LeadPoisoningEnabled
                && IsToxic(stack)
                && IsConsumptionVessel(stack);
        }

        /// <summary>True if the ware is clean but what is inside it is not — the case a player has no
        /// other way of finding out about, and the whole reason the mark is visible at all.</summary>
        public static bool WarnsContents(ItemStack stack)
        {
            if (!RudimentsModSystem.LeadPoisoningEnabled || IsToxic(stack)) return false;

            return IsMarked(stack) || IsMarked(ContentOf(stack));
        }

        /// <summary>
        /// One helping consumed. Silent and free when the helping was clean, so callers do not need to
        /// branch. Server-side only — the burden is authoritative state.
        /// </summary>
        public static void Expose(EntityAgent byEntity, bool leaded, double servings)
        {
            if (!leaded || servings <= 0 || byEntity?.World == null) return;
            if (byEntity.World.Side != EnumAppSide.Server) return;

            if (!RudimentsModSystem.LeadPoisoningEnabled) return;

            RudimentsConfig cfg = RudimentsModSystem.Config;
            if (cfg.LeadPerServing <= 0) return;

            byEntity.GetBehavior<EntityBehaviorLeadBurden>()?.Add(servings * cfg.LeadPerServing);
        }
    }
}
