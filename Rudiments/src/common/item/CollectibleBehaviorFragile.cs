using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Per-use fragility. Every time a player actually uses a fired clay vessel — drinks from it,
    /// fills it, empties it, pours it — there is a small chance it shatters in their hands.
    ///
    /// "Use" is deliberately narrow: it is a player-initiated *held* interaction that ran to
    /// completion, which is what <c>OnHeldInteractStop</c> means. Carrying a vessel, stacking it,
    /// putting it on a shelf and opening a placed container are all free. The last of those is
    /// widened by <c>BreakageIncludesPlacedContainers</c>, handled on the vessel block itself.
    ///
    /// The numbers are low on purpose (0.1% / 0.5% / 1.5%): median uses before a break are ~693,
    /// ~138 and ~46. Wear is texture, not a tax. Seepage is what actually pushes the player up the
    /// ware ladder.
    /// </summary>
    public class CollectibleBehaviorFragile : CollectibleBehavior
    {
        public CollectibleBehaviorFragile(CollectibleObject collObj) : base(collObj) { }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);

            // Leave `handling` at PassThrough — setting it would suppress the vanilla eat/drink
            // completion that this same call is delivering.
            IWorldAccessor world = byEntity?.World;
            if (world == null || world.Side != EnumAppSide.Server) return;

            TryShatterInHand(world, slot, byEntity);
        }

        /// <summary>
        /// Rolls one use against the stack's tier and, on a hit, destroys exactly one item from the
        /// slot. Returns true if something broke.
        /// </summary>
        public static bool TryShatterInHand(IWorldAccessor world, ItemSlot slot, Entity byEntity)
        {
            ItemStack stack = slot?.Itemstack;
            if (stack == null || byEntity == null) return false;

            double chance = WareTier.BreakChance(WareTier.Get(stack), RudimentsModSystem.Config);
            if (chance <= 0 || world.Rand.NextDouble() >= chance) return false;

            ItemStack broken = stack.Clone();
            broken.StackSize = 1;

            Vec3d pos = byEntity.Pos.XYZ.Add(0, byEntity.LocalEyePos.Y * 0.5, 0);
            ClayWare.Shatter(world, pos, broken, 1);

            slot.TakeOut(1);
            slot.MarkDirty();
            return true;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            ItemStack stack = inSlot?.Itemstack;
            if (stack == null) return;

            EnumWareTier tier = WareTier.Get(stack);
            dsc.AppendLine(Lang.Get("rudiments:ware-label", WareTier.Name(tier)));

            string glaze = WareTier.GetGlaze(stack);
            if (glaze != null) dsc.AppendLine(Lang.Get("rudiments:glaze-label", WareTier.GlazeName(glaze)));

            // Right under the line that says it is sealed, because that is the line the player read
            // when they decided to glaze it. A leaded flowerpot never gets this — nothing eats out of
            // a flowerpot — so the warning only appears where it is true.
            if (LeadGlaze.Warns(stack)) dsc.AppendLine(Lang.Get("rudiments:glaze-lead-warning"));

            // And the one a player has no other way of discovering: a spotless bowl holding a stew
            // that was cooked in a leaded pot three vessels ago.
            else if (LeadGlaze.WarnsContents(stack)) dsc.AppendLine(Lang.Get("rudiments:lead-contents-warning"));

            double chance = WareTier.BreakChance(tier, RudimentsModSystem.Config);
            if (chance > 0) dsc.AppendLine(Lang.Get("rudiments:ware-breakchance", chance * 100));

            if (RudimentsModSystem.Config.ThrownClayBreakChance > 0) dsc.AppendLine(Lang.Get("rudiments:ware-dropwarning"));
        }
    }
}
