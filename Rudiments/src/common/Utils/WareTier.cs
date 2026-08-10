using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Rudiments.Utils
{
    /// <summary>
    /// The three ceramic bodies, in the order a potter unlocks them.
    /// Earthenware is porous and weeps; stoneware is vitrified and does not; porcelain is
    /// vitrified, white and the most brittle of the three under impact.
    /// </summary>
    public enum EnumWareTier
    {
        Earthenware = 0,
        Stoneware = 1,
        Porcelain = 2
    }

    /// <summary>
    /// Ware tier is carried on a fired-clay itemstack as the string attribute "rudimentsware".
    /// Only <c>"stoneware"</c> is ever written — earthenware is the ABSENCE of the attribute, so
    /// every pre-existing and third-party clay item reads as earthenware with no migration.
    /// Porcelain is not an attribute at all: it is a real <c>porcelain</c> block/item variant, which
    /// makes it immune to the place-then-break attribute loss that the stoneware attribute is
    /// exposed to (see <see cref="BlockEntityBehaviorWareTier"/>).
    ///
    /// "Sealed" — stoneware, porcelain, or any glaze — is written in exactly one place,
    /// <see cref="IsSealed"/>, and is what the seepage behavior and the watering-can gate read.
    /// </summary>
    public static class WareTier
    {
        /// <summary>String stack attribute holding the tier. Absent == earthenware.</summary>
        public const string AttrKey = "rudimentsware";

        /// <summary>String stack attribute holding the glaze ("lead", "tin", "salt"). Absent == unglazed.</summary>
        public const string GlazeAttrKey = "rudimentsglaze";

        /// <summary>Double stack attribute: total game hours at the last seepage settle.</summary>
        public const string SeepCheckAttrKey = "rudimentsseepcheck";

        /// <summary>The colour/type variant state that means "this is a porcelain body".</summary>
        public const string PorcelainVariant = "porcelain";

        private const string StonewareValue = "stoneware";

        /// <summary>
        /// True if this collectible is a porcelain-bodied variant. Checks both variant groups that
        /// vanilla uses for clay: <c>color</c> on ware blocktypes, <c>type</c> on the clay item and
        /// the clay work item.
        /// </summary>
        public static bool IsPorcelain(CollectibleObject collectible)
        {
            var variant = collectible?.Variant;
            if (variant == null) return false;
            return variant["color"] == PorcelainVariant || variant["type"] == PorcelainVariant;
        }

        public static EnumWareTier Get(ItemStack stack)
        {
            if (stack == null) return EnumWareTier.Earthenware;
            if (IsPorcelain(stack.Collectible)) return EnumWareTier.Porcelain;
            if (stack.Attributes?.GetString(AttrKey) == StonewareValue) return EnumWareTier.Stoneware;
            return EnumWareTier.Earthenware;
        }

        /// <summary>
        /// Stamps the tier onto a stack. Earthenware REMOVES the attribute rather than writing a
        /// value, so an untiered stack and an explicitly-earthenware stack stay mergeable.
        /// Porcelain is never written — the variant already says it.
        /// </summary>
        public static void Set(ItemStack stack, EnumWareTier tier)
        {
            if (stack?.Attributes == null) return;

            if (tier == EnumWareTier.Stoneware && !IsPorcelain(stack.Collectible))
            {
                stack.Attributes.SetString(AttrKey, StonewareValue);
            }
            else
            {
                stack.Attributes.RemoveAttribute(AttrKey);
            }
        }

        /// <summary>Copy the tier from a source stack onto a freshly produced stack.</summary>
        public static void Carry(ItemStack from, ItemStack to)
        {
            Set(to, Get(from));
        }

        public static string GetGlaze(ItemStack stack)
        {
            string glaze = stack?.Attributes?.GetString(GlazeAttrKey);
            return string.IsNullOrEmpty(glaze) ? null : glaze;
        }

        public static void SetGlaze(ItemStack stack, string glaze)
        {
            if (stack?.Attributes == null) return;
            if (string.IsNullOrEmpty(glaze)) stack.Attributes.RemoveAttribute(GlazeAttrKey);
            else stack.Attributes.SetString(GlazeAttrKey, glaze);
        }

        /// <summary>Copy the glaze from a source stack onto a freshly produced stack.</summary>
        public static void CarryGlaze(ItemStack from, ItemStack to)
        {
            SetGlaze(to, GetGlaze(from));
        }

        /// <summary>
        /// The one place the sealing rule is written. A vessel is sealed if its body is vitrified
        /// (stoneware or porcelain) or if it carries any glaze. Unsealed earthenware is the only
        /// thing that seeps, and the only thing the watering-can gate refuses.
        /// </summary>
        public static bool IsSealed(ItemStack stack)
        {
            if (stack == null) return false;
            if (Get(stack) != EnumWareTier.Earthenware) return true;
            return GetGlaze(stack) != null;
        }

        /// <summary>
        /// Per-use shatter chance for a tier. Porcelain is deliberately the highest: it is
        /// thinner-walled and more brittle under impact than thick porous earthenware, which chips
        /// where porcelain shatters.
        /// </summary>
        public static double BreakChance(EnumWareTier tier, RudimentsConfig cfg)
        {
            if (cfg == null) return 0;
            switch (tier)
            {
                case EnumWareTier.Stoneware: return Math.Max(0, cfg.StonewareBreakChance);
                case EnumWareTier.Porcelain: return Math.Max(0, cfg.PorcelainBreakChance);
                default: return Math.Max(0, cfg.EarthenwareBreakChance);
            }
        }

        public static string Name(EnumWareTier tier)
        {
            switch (tier)
            {
                case EnumWareTier.Stoneware: return Lang.Get("rudiments:ware-stoneware");
                case EnumWareTier.Porcelain: return Lang.Get("rudiments:ware-porcelain");
                default: return Lang.Get("rudiments:ware-earthenware");
            }
        }

        public static string GlazeName(string glaze)
        {
            return Lang.Get("rudiments:glaze-" + glaze);
        }
    }
}
