using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Greenware that can carry a glaze through the fire.
    ///
    /// The pit kiln discards input NBT outright — <c>BEPitKiln.OnFired</c> replaces each slot with
    /// <c>combustibleProps.SmeltedStack.ResolvedItemstack.Clone()</c> and never looks at what was
    /// there before — so a glaze stamped on the raw stack cannot simply ride through firing. But the
    /// pit kiln reaches that stack via <c>rawStack.Collectible.GetCombustibleProperties(world, stack,
    /// pos)</c>, which is <c>public virtual</c> and stack-aware. Overriding it to return a per-stack
    /// <c>SmeltedStack</c> puts the glaze on the *output* instead, and then the vanilla pit kiln, the
    /// vanilla beehive and both Rudiments kilns all carry it for free with no kiln-side code.
    ///
    /// This is exactly how medieval lead-glazed earthenware was actually made: raw galena dusted
    /// onto the pot and fired with it in a single pass. Raw glazing — apply to bone-dry greenware,
    /// fire once — was standard for millennia and is still used in East Asian traditions. Double
    /// firing is the modern Western habit, and this mod does not do it.
    /// </summary>
    internal class BlockGlazableClayware : Block
    {
        public override CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos)
        {
            CombustibleProperties props = base.GetCombustibleProperties(world, stack, pos);

            string glaze = WareTier.GetGlaze(stack);
            if (glaze == null || props?.SmeltedStack?.ResolvedItemstack == null) return props;

            // Clone() deep-copies SmeltedStack and its ResolvedItemstack, so the block's shared
            // properties are never mutated — this must stay a per-stack answer.
            CombustibleProperties glazed = props.Clone();
            WareTier.SetGlaze(glazed.SmeltedStack.ResolvedItemstack, glaze);
            return glazed;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string glaze = WareTier.GetGlaze(inSlot?.Itemstack);
            if (glaze == null) return;

            dsc.AppendLine(Lang.Get("rudiments:glaze-raw-label", WareTier.GlazeName(glaze)));

            // Unconditional on greenware, unlike the fired warning: this is the moment the player is
            // deciding, and a raw bowl is not yet a vessel the vessel test would recognise.
            if (LeadGlaze.IsToxic(glaze) && RudimentsModSystem.LeadPoisoningEnabled)
            {
                dsc.AppendLine(Lang.Get("rudiments:glaze-lead-warning"));
            }
        }
    }
}
