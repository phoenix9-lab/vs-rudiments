using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A crock that does not launder the bowl it serves into. See <see cref="BlockWarePot"/> — same
    /// three routes, same snapshot-and-restore, different base class.
    /// </summary>
    internal class BlockWareCrock : BlockCrock
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            WareKeep keep = WareKeep.OfGroundStorage(byEntity?.World, blockSel);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            keep.Restore();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            WareKeep keep = WareKeep.Of(byPlayer?.InventoryManager?.ActiveHotbarSlot);
            bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);
            keep.Restore();
            return handled;
        }

        public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            WareKeep keep = WareKeep.Of(byPlayer?.InventoryManager?.ActiveHotbarSlot);
            bool handled = base.OnContainedInteractStart(be, slot, byPlayer, blockSel);
            keep.Restore();
            return handled;
        }
    }
}
