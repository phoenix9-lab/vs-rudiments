using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A liquid-holding clay vessel that settles its seepage before anything reads its contents, and
    /// that puts its lead in you when you drink out of it.
    ///
    /// <see cref="CollectibleBehaviorSeepage"/> owns all the arithmetic; this class exists only
    /// because <c>BlockLiquidContainerBase.OnHeldInteractStart</c> overrides without calling base
    /// on the fill / pour / spill path, so a collectible behavior attached to the same block never
    /// sees it. Pouring a jug that had been standing full for twelve hours would otherwise transfer
    /// its full un-drained contents into a barrel and sidestep seepage entirely.
    ///
    /// Drinking is the other half. <c>tryEatStop</c> is <c>protected override</c> on the same base
    /// class, so the swallow is one override away in a class the mod already owns — no second patch
    /// and no new class. The jug of water is the exposure route that matters most for lead, and
    /// historically it is the honest one: leaded drinking vessels and lead-lined cisterns did far
    /// more harm than leaded dinnerware ever did.
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

        /// <summary>
        /// One swallow. Vanilla decides how much came out and whether anything came out at all, so
        /// the dose is measured from the litres that actually left rather than from the click — a
        /// released-early drink costs nothing.
        /// </summary>
        protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            ItemStack vessel = slot?.Itemstack;
            float before = vessel == null ? 0 : GetCurrentLitres(vessel);

            base.tryEatStop(secondsUsed, slot, byEntity);

            if (byEntity?.World == null || byEntity.World.Side != EnumAppSide.Server) return;

            ItemStack after = slot?.Itemstack;
            if (after?.Collectible != this) return;

            if (GetCurrentLitres(after) < before) LeadGlaze.Expose(byEntity, vessel, 1);
        }
    }
}
