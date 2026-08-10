using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A cooked clay pot that does not launder the bowl it serves into.
    ///
    /// Pure plumbing: every method here snapshots the bowl, calls vanilla, and puts the ware tier and
    /// glaze back. See <see cref="WareKeep"/> for why that is necessary and what it costs. The three
    /// overrides are the three ways a pot can be tipped into a bowl —
    ///
    /// <list type="bullet">
    /// <item>pot in hand, right-click a bowl sitting in ground storage;</item>
    /// <item>pot on the ground, right-click it holding a bowl;</item>
    /// <item>pot in a container or on a shelf, right-click it holding a bowl.</item>
    /// </list>
    ///
    /// <see cref="BlockWareCrock"/> is the same three overrides over a different base class, which is
    /// the only reason it is a separate file.
    /// </summary>
    internal class BlockWarePot : BlockCookedContainer
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
