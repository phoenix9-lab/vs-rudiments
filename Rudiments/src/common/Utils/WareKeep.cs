using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Carries a vessel's ware tier and glaze across the four places vanilla throws them away.
    ///
    /// All four are the same mistake: a stack is not modified, it is <b>replaced</b> by a freshly
    /// built one whose only inheritance is the blocktype code.
    ///
    /// <list type="bullet">
    /// <item><c>ServeIntoStack</c> — unless the bowl already holds the identical meal, it builds a new
    /// stack from the bowl's <c>mealBlockCode</c> and assigns it over the top.</item>
    /// <item><c>SetServingsMaybeEmpty</c> — the pot or crock that just gave up its last serving is
    /// replaced by its <c>emptiedBlockCode</c>.</item>
    /// <item><c>BlockMeal</c>'s <c>eatenBlock</c> — the bowl you just emptied.</item>
    /// <item><c>BlockCookingContainer.DoSmelt</c> — the cooked pot is a new stack of the
    /// <c>type: cooked</c> variant; the raw pot that went into the fire is simply dropped.</item>
    /// </list>
    ///
    /// So a stoneware bowl came back untiered the first time you ate soup out of it, and a lead-glazed
    /// pot came out of the firepit clean. None of the four methods is virtual. What is virtual is
    /// every interaction that reaches them, so the fix is the same everywhere: snapshot the slot,
    /// let vanilla do whatever it does, and if the collectible changed underneath us, stamp the two
    /// attributes back onto whatever is there now.
    ///
    /// <b>Known gap.</b> Right-clicking a ground-stored pot while holding a <i>stack</i> of bowls
    /// sends the served meal off to <c>TryGiveItemstack</c> rather than leaving it in hand, and it is
    /// not worth guessing which inventory slot it landed in. One bowl at a time keeps its ware.
    /// </summary>
    internal readonly struct WareKeep
    {
        private readonly ItemSlot slot;
        private readonly CollectibleObject was;
        private readonly string tier;
        private readonly string glaze;

        private WareKeep(ItemSlot slot, CollectibleObject was, string tier, string glaze)
        {
            this.slot = slot;
            this.was = was;
            this.tier = tier;
            this.glaze = glaze;
        }

        /// <summary>Snapshots a slot. Cheap and safe on anything, including null and empty slots.</summary>
        public static WareKeep Of(ItemSlot slot)
        {
            ItemStack stack = slot?.Itemstack;
            if (stack?.Attributes == null) return default;

            return new WareKeep(slot, stack.Collectible,
                stack.Attributes.GetString(WareTier.AttrKey),
                stack.Attributes.GetString(WareTier.GlazeAttrKey));
        }

        /// <summary>The ground-storage slot under the cursor, which is where bowls and crocks actually
        /// live. Null when the player is not pointing at one.</summary>
        public static ItemSlot GroundStorageSlot(IWorldAccessor world, BlockSelection blockSel)
        {
            if (blockSel == null) return null;
            if (world?.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityGroundStorage begs) return null;

            return begs.GetSlotAt(blockSel);
        }

        public void Restore() => RestoreTo(slot);

        /// <summary>
        /// Puts the snapshot onto <paramref name="target"/>, but only if the stack there has been
        /// swapped for a different collectible — that swap is the whole failure mode. An untouched
        /// slot, an emptied slot, or one holding the leftovers of the same stack is left alone.
        /// </summary>
        public void RestoreTo(ItemSlot target)
        {
            if (was == null || (tier == null && glaze == null)) return;

            ItemStack stack = target?.Itemstack;
            if (stack?.Attributes == null || stack.Collectible == was) return;

            if (tier != null) stack.Attributes.SetString(WareTier.AttrKey, tier);
            if (glaze != null) stack.Attributes.SetString(WareTier.GlazeAttrKey, glaze);
            target.MarkDirty();
        }
    }
}
