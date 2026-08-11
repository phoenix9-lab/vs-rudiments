using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A cooked clay pot that neither launders the bowl it serves into nor loses its own identity when
    /// it empties.
    ///
    /// Every method here snapshots both sides of the hand-off, calls vanilla, restores the ware on
    /// both, and passes any lead contamination on to the vessel that received the food. See
    /// <see cref="WareKeep"/> for what vanilla drops and <see cref="LeadGlaze"/> for why the lead has
    /// to follow the meal rather than sit on the pot.
    ///
    /// The three overrides are the three ways a pot can be tipped into a bowl —
    ///
    /// <list type="bullet">
    /// <item>pot in hand, right-click a bowl sitting in ground storage;</item>
    /// <item>pot in ground storage or on a shelf, right-click it holding a bowl;</item>
    /// <item>pot placed as a block, right-click it holding a bowl. Vanilla pots are
    /// <c>Unplaceable</c>, so this one is only reachable if another mod makes them otherwise — it is
    /// covered because it costs three lines, not because it fires.</item>
    /// </list>
    ///
    /// <see cref="BlockWareCrock"/> is the same three overrides over a different base class, which is
    /// the only reason it is a separate file.
    /// </summary>
    internal class BlockWarePot : BlockCookedContainer
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            ItemSlot bowlSlot = WareKeep.GroundStorageSlot(byEntity?.World, blockSel);

            WareKeep bowl = WareKeep.Of(bowlSlot);
            WareKeep pot = WareKeep.Of(slot);
            bool leaded = LeadGlaze.MealIsLeaded(slot?.Itemstack);

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);

            bowl.Restore();
            pot.Restore();
            LeadGlaze.CarryTo(bowlSlot, leaded);
        }

        public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot bowlSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;

            WareKeep bowl = WareKeep.Of(bowlSlot);
            WareKeep pot = WareKeep.Of(slot);
            bool leaded = LeadGlaze.MealIsLeaded(slot?.Itemstack);

            bool handled = base.OnContainedInteractStart(be, slot, byPlayer, blockSel);

            bowl.Restore();
            pot.Restore();
            LeadGlaze.CarryTo(bowlSlot, leaded);
            return handled;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // No source stack on this route — a placed pot is a block and a block entity, and neither
            // carries the glaze — so the ware is restored but no contamination can be read.
            WareKeep bowl = WareKeep.Of(byPlayer?.InventoryManager?.ActiveHotbarSlot);
            bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);
            bowl.Restore();
            return handled;
        }
    }
}
