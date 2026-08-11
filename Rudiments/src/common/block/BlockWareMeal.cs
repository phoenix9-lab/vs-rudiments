using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A bowl with a meal in it — the last link in the chain, and the only place eating can be
    /// observed.
    ///
    /// Three things happen here that <c>BlockMeal</c> does not do:
    ///
    /// <list type="bullet">
    /// <item><b>The ware survives the meal.</b> <c>eatenBlock</c> hands back a freshly built empty
    /// bowl with none of the original's attributes on it. See <see cref="WareKeep"/> — one of four
    /// places vanilla does this.</item>
    /// <item><b>Lead reaches the eater.</b> Not the bowl's lead: <i>the meal's</i>. A clean bowl
    /// holding a stew that was cooked in a leaded pot poisons you exactly as much as a leaded bowl
    /// does, which is the point of tracking it. Scaled by the servings that actually went down rather
    /// than by the click.</item>
    /// <item><b>Fragility finally fires.</b> <c>BlockMeal.OnHeldInteractStop</c> overrides without
    /// calling base, and base is what forwards to <c>CollectibleBehaviors</c> — so
    /// <see cref="CollectibleBehaviorFragile"/> has never once rolled when a player ate a meal out of
    /// a bowl, in any version of this mod. Calling base here would not fix that, because base <i>is</i>
    /// the override that skips it; the roll is invoked directly instead.</item>
    /// </list>
    ///
    /// Everything is gated on servings having genuinely been consumed, so releasing the button early
    /// costs nothing and neither poisons nor breaks anything.
    /// </summary>
    internal class BlockWareMeal : BlockMeal
    {
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            IWorldAccessor world = byEntity?.World;
            ItemStack before = slot?.Itemstack;

            WareKeep keep = WareKeep.Of(slot);
            bool leaded = LeadGlaze.MealIsLeaded(before);
            float servingsBefore = before == null ? 0 : GetQuantityServings(world, before);

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (world == null || world.Side != EnumAppSide.Server) return;

            // The emptied bowl is a new stack, so the ware goes back on it — but deliberately not the
            // lead mark. The mark says "what is in this is leaded", and there is nothing in it now.
            keep.Restore();

            ItemStack after = slot?.Itemstack;
            float servingsAfter = after?.Collectible == this ? GetQuantityServings(world, after) : 0;

            double eaten = servingsBefore - servingsAfter;
            if (eaten <= 0) return;

            LeadGlaze.Expose(byEntity, leaded, eaten);
            CollectibleBehaviorFragile.TryShatterInHand(world, slot, byEntity);
        }
    }
}
