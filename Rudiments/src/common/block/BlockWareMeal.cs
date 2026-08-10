using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A bowl with a meal in it — the one vessel in the game that food actually passes through on its
    /// way into a player, and therefore the one place eating can be observed.
    ///
    /// Three things happen here that <c>BlockMeal</c> does not do:
    ///
    /// <list type="bullet">
    /// <item><b>The ware survives the meal.</b> <c>eatenBlock</c> hands back a freshly built empty
    /// bowl with none of the original's attributes on it. See <see cref="WareKeep"/> — this is the
    /// return leg of the same loss.</item>
    /// <item><b>Lead reaches the eater.</b> A leaded bowl doses whoever empties it, scaled by how
    /// many servings actually went down rather than by the click.</item>
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
            float servingsBefore = before == null ? 0 : GetQuantityServings(world, before);

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (world == null || world.Side != EnumAppSide.Server) return;

            RestoreWare(slot, keep);

            ItemStack after = slot?.Itemstack;
            float servingsAfter = after?.Collectible == this ? GetQuantityServings(world, after) : 0;

            double eaten = servingsBefore - servingsAfter;
            if (eaten <= 0) return;

            LeadGlaze.Expose(byEntity, before, eaten);
            CollectibleBehaviorFragile.TryShatterInHand(world, slot, byEntity);
        }

        /// <summary>
        /// Re-stamps the bowl once the last serving is gone. <see cref="WareKeep.Restore"/> only
        /// writes onto meals, and this is the opposite case — the meal became an empty bowl — so the
        /// two attributes go back on by hand.
        /// </summary>
        private static void RestoreWare(ItemSlot slot, WareKeep keep)
        {
            ItemStack stack = slot?.Itemstack;
            if (stack?.Block is IBlockMealContainer) return;

            if (keep.ApplyTo(stack)) slot.MarkDirty();
        }
    }
}
