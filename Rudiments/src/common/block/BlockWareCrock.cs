using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A crock that neither launders the bowl it serves into nor loses its own identity when it
    /// empties. See <see cref="BlockWarePot"/> — same three routes, same snapshot-restore-carry,
    /// different base class.
    ///
    /// The crock is the storage half of the lead problem: food sealed in a lead-glazed crock is
    /// leaded when it comes back out, however clean the bowl it is served into.
    /// </summary>
    internal class BlockWareCrock : BlockCrock
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            ItemSlot bowlSlot = WareKeep.GroundStorageSlot(byEntity?.World, blockSel);

            WareKeep bowl = WareKeep.Of(bowlSlot);
            WareKeep crock = WareKeep.Of(slot);
            bool leaded = LeadGlaze.MealIsLeaded(slot?.Itemstack);

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);

            bowl.Restore();
            crock.Restore();
            LeadGlaze.CarryTo(bowlSlot, leaded);
        }

        public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot bowlSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;

            WareKeep bowl = WareKeep.Of(bowlSlot);
            WareKeep crock = WareKeep.Of(slot);
            bool leaded = LeadGlaze.MealIsLeaded(slot?.Itemstack);

            bool handled = base.OnContainedInteractStart(be, slot, byPlayer, blockSel);

            bowl.Restore();
            crock.Restore();
            LeadGlaze.CarryTo(bowlSlot, leaded);
            return handled;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            WareKeep bowl = WareKeep.Of(byPlayer?.InventoryManager?.ActiveHotbarSlot);
            bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);
            bowl.Restore();
            return handled;
        }
    }
}
