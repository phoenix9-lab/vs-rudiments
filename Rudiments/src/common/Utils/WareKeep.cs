using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Carries a bowl's ware tier and glaze across vanilla's serve, which throws them away.
    ///
    /// <c>BlockCookedContainerBase.ServeIntoStack</c> does not fill the bowl you are holding. Unless
    /// that bowl already holds the identical meal it builds a <b>brand new stack</b> from the bowl
    /// blocktype's <c>mealBlockCode</c> and assigns it over the top — so a stoneware bowl comes back
    /// as an untiered one the first time you eat soup out of it, and a lead-glazed bowl comes back
    /// clean. <c>BlockMeal</c>'s <c>eatenBlock</c> does the same thing in reverse when the last
    /// serving goes. Neither is reachable by overriding: <c>ServeIntoStack</c> is not virtual.
    ///
    /// So the fix is at the callers, which are: snapshot the bowl slot, let vanilla do whatever it
    /// does, then put the two attributes back on whatever meal is now sitting there. The tier half of
    /// this is a plain bug fix that predates lead; the glaze half is what makes a lead-glazed bowl
    /// reach the person eating from it at all.
    ///
    /// <b>Known gap.</b> Right-clicking a ground-stored pot while holding a <i>stack</i> of bowls
    /// sends the served meal off to <c>TryGiveItemstack</c> rather than leaving it in hand, and it is
    /// not worth guessing which inventory slot it landed in. One bowl at a time keeps its ware.
    /// </summary>
    internal readonly struct WareKeep
    {
        private readonly ItemSlot slot;
        private readonly string tier;
        private readonly string glaze;

        private WareKeep(ItemSlot slot, string tier, string glaze)
        {
            this.slot = slot;
            this.tier = tier;
            this.glaze = glaze;
        }

        /// <summary>Snapshots a slot. Cheap and safe on anything, including null and empty slots.</summary>
        public static WareKeep Of(ItemSlot slot)
        {
            ItemStack stack = slot?.Itemstack;
            if (stack?.Attributes == null) return default;

            return new WareKeep(slot,
                stack.Attributes.GetString(WareTier.AttrKey),
                stack.Attributes.GetString(WareTier.GlazeAttrKey));
        }

        /// <summary>Snapshots the ground-storage slot under the cursor, which is where greenware and
        /// bowls actually live. Returns an empty keep when the player is not pointing at one.</summary>
        public static WareKeep OfGroundStorage(IWorldAccessor world, BlockSelection blockSel)
        {
            if (blockSel == null) return default;
            if (world?.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityGroundStorage begs) return default;

            return Of(begs.GetSlotAt(blockSel));
        }

        /// <summary>
        /// Puts the snapshot back, but only onto a meal — if the slot still holds the bowl we started
        /// with, or something unrelated, vanilla did not do the swap and there is nothing to restore.
        /// </summary>
        public void Restore()
        {
            ItemStack stack = slot?.Itemstack;
            if (stack?.Block is not IBlockMealContainer) return;

            if (ApplyTo(stack)) slot.MarkDirty();
        }

        /// <summary>Writes the snapshot onto any stack. Returns false when there was nothing to write.</summary>
        public bool ApplyTo(ItemStack stack)
        {
            if (stack?.Attributes == null || (tier == null && glaze == null)) return false;

            if (tier != null) stack.Attributes.SetString(WareTier.AttrKey, tier);
            if (glaze != null) stack.Attributes.SetString(WareTier.GlazeAttrKey, glaze);
            return true;
        }
    }
}
