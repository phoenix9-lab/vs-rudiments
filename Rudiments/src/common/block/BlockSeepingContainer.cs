using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// A liquid-holding clay vessel that settles its seepage before anything reads its contents, and
    /// that puts its lead into the liquid rather than into you directly.
    ///
    /// <see cref="CollectibleBehaviorSeepage"/> owns all the seepage arithmetic; this class exists
    /// only because <c>BlockLiquidContainerBase.OnHeldInteractStart</c> overrides without calling base
    /// on the fill / pour / spill path, so a collectible behavior attached to the same block never
    /// sees it. Pouring a jug that had been standing full for twelve hours would otherwise transfer
    /// its full un-drained contents into a barrel and sidestep seepage entirely.
    ///
    /// <b>Lead marks the liquid, not the cup.</b> That is what makes it travel: <c>TryTakeContent</c>
    /// clones the content stack out of the container, so the mark rides the portion into whatever
    /// receives it — another vessel, a bucket, a barrel — and comes back out again when it is drawn
    /// off. Nothing has to be hooked for that; the only two things that do are the moment a leaded
    /// vessel first touches a liquid, and the drink itself.
    ///
    /// Historically this is the honest emphasis. Leaded drinking vessels and lead-lined cisterns did
    /// far more harm than leaded dinnerware ever did, and they did it because the lead ended up in
    /// the water rather than staying politely on the pottery.
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
            Contaminate(itemslot?.Itemstack);

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        /// <summary>
        /// Liquid arriving in this vessel. The one hook the whole liquid chain needs: contamination
        /// starts here if the vessel is leaded, and continues here if what is being poured in already
        /// was.
        /// </summary>
        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            int moved = base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
            if (moved <= 0) return moved;

            if (LeadGlaze.IsToxic(containerStack) || LeadGlaze.IsMarked(liquidStack))
            {
                LeadGlaze.Mark(GetContent(containerStack));
            }

            return moved;
        }

        /// <summary>
        /// One swallow. Vanilla decides how much came out and whether anything came out at all, so
        /// the dose is measured from the litres that actually left rather than from the click — a
        /// released-early drink costs nothing.
        /// </summary>
        protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            ItemStack vessel = slot?.Itemstack;
            Contaminate(vessel);

            bool leaded = LeadGlaze.DrinkIsLeaded(vessel, GetContentSafe(vessel));
            float before = vessel == null ? 0 : GetCurrentLitres(vessel);

            base.tryEatStop(secondsUsed, slot, byEntity);

            if (byEntity?.World == null || byEntity.World.Side != EnumAppSide.Server) return;

            ItemStack after = slot?.Itemstack;
            if (after?.Collectible != this) return;

            if (GetCurrentLitres(after) < before) LeadGlaze.Expose(byEntity, leaded, 1);
        }

        /// <summary>
        /// Marks whatever this vessel is holding, if the vessel is the thing doing the leaching.
        /// Called on the way in to every interaction rather than only on filling, because a container
        /// can also be filled straight from a water block or a barrel without <see cref="TryPutLiquid"/>
        /// ever running.
        /// </summary>
        private void Contaminate(ItemStack containerStack)
        {
            if (!LeadGlaze.IsToxic(containerStack)) return;

            LeadGlaze.Mark(GetContentSafe(containerStack));
        }

        private ItemStack GetContentSafe(ItemStack containerStack)
        {
            return containerStack == null ? null : GetContent(containerStack);
        }
    }
}
