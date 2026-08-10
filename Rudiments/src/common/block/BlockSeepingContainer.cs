using Rudiments.SRC.Common.Items;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A liquid-holding clay vessel that settles its seepage before anything reads its contents.
    ///
    /// <see cref="CollectibleBehaviorSeepage"/> owns all the arithmetic; this class exists only
    /// because <c>BlockLiquidContainerBase.OnHeldInteractStart</c> overrides without calling base
    /// on the fill / pour / spill path, so a collectible behavior attached to the same block never
    /// sees it. Pouring a jug that had been standing full for twelve hours would otherwise transfer
    /// its full un-drained contents into a barrel and sidestep seepage entirely.
    ///
    /// Patched in via <c>classByType</c> on the fired bowl and jug — the only two vanilla clay
    /// blocktypes that are genuinely liquid containers. The clay pot is a
    /// <c>BlockCookingContainer</c> and holds no litres.
    /// </summary>
    internal class BlockSeepingContainer : BlockLiquidContainerTopOpened
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            CollectibleBehaviorSeepage.Settle(byEntity?.World, itemslot?.Itemstack);
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }
    }
}
