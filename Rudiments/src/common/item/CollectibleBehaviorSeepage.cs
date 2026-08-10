using System;
using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Unsealed earthenware weeps. Above about 3% water absorption a fired clay body is genuinely
    /// porous, and an unglazed earthenware jug loses its contents over hours — which is the whole
    /// reason a potter cares about vitrification or a glaze in the first place.
    ///
    /// The loss is a fraction of <b>each vessel's own capacity</b> per hour, so a 1 L bowl and a
    /// 3 L jug both read half at six hours and dry at twelve. A flat litre rate could not serve
    /// both, and percentage-of-remaining is exponential and never actually empties.
    ///
    /// Nothing ticks. Two attributes anchor the calculation — <c>rudimentsseepcheck</c> (the game
    /// hour of the last settle) and <c>rudimentsseeplitres</c> (the litres at that moment) — and the
    /// elapsed loss is applied whenever the vessel is next handled. The second attribute is what
    /// makes the settle idempotent: without it there is no way to tell "seeped away since last
    /// check" from "the player just filled it", and a refill would be drained the instant it
    /// happened.
    /// </summary>
    public class CollectibleBehaviorSeepage : CollectibleBehavior
    {
        /// <summary>Float stack attribute: litres present at the last settle. Companion to <see cref="WareTier.SeepCheckAttrKey"/>.</summary>
        public const string SeepLitresAttrKey = "rudimentsseeplitres";

        private const float Epsilon = 0.0001f;

        public CollectibleBehaviorSeepage(CollectibleObject collObj) : base(collObj) { }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            Settle(byEntity?.World, slot?.Itemstack);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            Settle(byEntity?.World, slot?.Itemstack);
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        /// <summary>Litres this vessel loses every in-game hour. 0 when sealed or seepage is disabled.</summary>
        public static float DrainPerHour(BlockLiquidContainerBase container)
        {
            float emptyHours = RudimentsModSystem.Config.EarthenwareEmptyHours;
            if (container == null || emptyHours <= 0) return 0;
            return container.CapacityLitres / emptyHours;
        }

        /// <summary>
        /// Applies everything that has seeped away since the last settle and re-anchors. Server
        /// side only and safe to call as often as you like — it is idempotent, and a vessel that
        /// was filled or emptied since the last anchor simply re-anchors without draining.
        /// </summary>
        public static void Settle(IWorldAccessor world, ItemStack stack)
        {
            if (world == null || world.Side != EnumAppSide.Server) return;
            if (stack?.Attributes == null) return;
            if (stack.Collectible is not BlockLiquidContainerBase container) return;

            if (WareTier.IsSealed(stack))
            {
                stack.Attributes.RemoveAttribute(WareTier.SeepCheckAttrKey);
                stack.Attributes.RemoveAttribute(SeepLitresAttrKey);
                return;
            }

            double now = world.Calendar.TotalHours;
            float litres = container.GetCurrentLitres(stack);

            bool anchored = stack.Attributes.HasAttribute(WareTier.SeepCheckAttrKey);
            float anchorLitres = stack.Attributes.GetFloat(SeepLitresAttrKey, 0);

            // Never seen before, or fuller than we left it: the player added liquid. Re-anchor.
            if (!anchored || litres > anchorLitres + Epsilon)
            {
                Anchor(stack, now, litres);
                return;
            }

            double elapsed = now - stack.Attributes.GetDouble(WareTier.SeepCheckAttrKey);
            float perHour = DrainPerHour(container);

            if (elapsed <= 0 || perHour <= 0 || litres <= 0)
            {
                Anchor(stack, now, litres);
                return;
            }

            SetLitres(container, stack, Math.Max(0f, litres - (float)(perHour * elapsed)));
            Anchor(stack, now, container.GetCurrentLitres(stack));
        }

        /// <summary>
        /// What the vessel actually holds right now, seepage included, without writing anything.
        /// Used for the tooltip, which runs client side where mutation would be overwritten by the
        /// next sync.
        /// </summary>
        public static float PreviewLitres(IWorldAccessor world, ItemStack stack)
        {
            if (stack?.Collectible is not BlockLiquidContainerBase container) return 0;

            float litres = container.GetCurrentLitres(stack);
            if (WareTier.IsSealed(stack) || litres <= 0) return litres;
            if (stack.Attributes?.HasAttribute(WareTier.SeepCheckAttrKey) != true) return litres;

            float anchorLitres = stack.Attributes.GetFloat(SeepLitresAttrKey, 0);
            if (litres > anchorLitres + Epsilon) return litres;

            double elapsed = world.Calendar.TotalHours - stack.Attributes.GetDouble(WareTier.SeepCheckAttrKey);
            if (elapsed <= 0) return litres;

            return Math.Max(0f, litres - (float)(DrainPerHour(container) * elapsed));
        }

        private static void Anchor(ItemStack stack, double atHours, float litres)
        {
            stack.Attributes.SetDouble(WareTier.SeepCheckAttrKey, atHours);
            stack.Attributes.SetFloat(SeepLitresAttrKey, litres);
        }

        private static void SetLitres(BlockLiquidContainerBase container, ItemStack stack, float litres)
        {
            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(container.GetContent(stack));
            if (props == null) return;

            // SetCurrentLitres truncates to whole content items; below one item there is nothing
            // left to represent, so clear the container rather than leave a zero-size stack in it.
            if ((int)(litres * props.ItemsPerLitre) <= 0) container.SetContent(stack, null);
            else container.SetCurrentLitres(stack, litres);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            ItemStack stack = inSlot?.Itemstack;
            if (stack?.Collectible is not BlockLiquidContainerBase container) return;

            if (WareTier.IsSealed(stack))
            {
                dsc.AppendLine(Lang.Get("rudiments:seepage-sealed"));
                return;
            }

            float perHour = DrainPerHour(container);
            if (perHour <= 0) return;

            dsc.AppendLine(Lang.Get("rudiments:seepage-rate", perHour, RudimentsModSystem.Config.EarthenwareEmptyHours));

            float stored = container.GetCurrentLitres(stack);
            float actual = PreviewLitres(world, stack);
            if (stored - actual > 0.05f)
            {
                dsc.AppendLine(Lang.Get("rudiments:seepage-pending", actual, stored - actual));
            }
        }
    }
}
