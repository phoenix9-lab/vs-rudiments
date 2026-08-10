using Rudiments.SRC.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// The one place that answers "is this glaze poisonous, and does this vessel put it in you".
    ///
    /// Lead glaze is not a lesser version of the other two. It is the cheapest glaze in the game, it
    /// works in a pit kiln on day one, and it costs nothing but a galena nugget — so without a price
    /// there is no reason to ever use anything else, and tin and salt are decoration. The price is
    /// that lead leaches, and the vessel you eat and drink from is the one that reaches you.
    ///
    /// <b>Exposure is the vessel in your hand at the moment you consume</b>, not wherever the food
    /// has been. Cooking in a leaded pot and eating out of a clean bowl does not count. That is a
    /// simplification, and a deliberate one: a provenance chain would be invisible, unexplainable and
    /// impossible to act on, where "do not eat off the lead" is a rule a player can follow.
    /// </summary>
    public static class LeadGlaze
    {
        /// <summary>The glaze value that poisons. Salt and tin do not, which is their entire purpose.</summary>
        public const string ToxicGlaze = "lead";

        public static bool IsToxic(string glaze) => glaze == ToxicGlaze;

        public static bool IsToxic(ItemStack stack) => IsToxic(WareTier.GetGlaze(stack));

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

        /// <summary>True if this stack should carry the poisoning warning on its tooltip.</summary>
        public static bool Warns(ItemStack stack)
        {
            return RudimentsModSystem.Config.LeadPoisoningEnabled
                && IsToxic(stack)
                && IsConsumptionVessel(stack);
        }

        /// <summary>
        /// One helping consumed from <paramref name="vessel"/>. Silent and free unless the vessel is
        /// leaded, so callers do not need to test anything first. Server-side only — the burden is
        /// authoritative state.
        /// </summary>
        public static void Expose(EntityAgent byEntity, ItemStack vessel, double servings)
        {
            if (servings <= 0 || byEntity?.World == null) return;
            if (byEntity.World.Side != EnumAppSide.Server) return;

            RudimentsConfig cfg = RudimentsModSystem.Config;
            if (!cfg.LeadPoisoningEnabled || cfg.LeadPerServing <= 0) return;
            if (!IsToxic(vessel)) return;

            byEntity.GetBehavior<EntityBehaviorLeadBurden>()?.Add(servings * cfg.LeadPerServing);
        }
    }
}
