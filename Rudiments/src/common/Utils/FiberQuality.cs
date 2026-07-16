using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Rudiments.Utils
{
    /// <summary>
    /// Fiber quality is carried on a bundle/fiber itemstack as the integer attribute "fiberquality".
    /// 0 = coarse, 1 = standard, 2 = fine. Quality is now driven by retting TIMING for both water
    /// (vat) and field (bed) retting: under-retted bundles yield Coarse fiber; hitting the RNG-gated
    /// Fine window rewards attentive players; letting it run past that settles to Standard; leaving it
    /// far too long rots the bundle entirely. It is read at the hatcheling/scutching stage to scale
    /// the final fiber yield.
    /// </summary>
    public static class FiberQuality
    {
        public const string AttrKey = "fiberquality";

        /// <summary>
        /// Harvest-time quality ceiling carried on a pre-retted flax bundle as the integer
        /// attribute "fiberpotential". Stamped by BlockCropFlax when FlaxBloomHarvest is enabled:
        /// Fine = cut in bloom (stage 8) — converts at Standard, the Fine window can open, never
        /// yields seeds. Standard = fully mature (stage 9) — converts at Coarse, capped Standard,
        /// ripples into seeds and grain. Unset (attribute absent — legacy bundles, or the feature
        /// disabled) rets over the full pre-0.11 coarse-to-fine range. Nettle ignores this and is
        /// always capped at Standard. Consumed (dropped) when retting assigns the final quality.
        /// </summary>
        public const string PotentialAttrKey = "fiberpotential";

        public const int Unset = -1;
        public const int Coarse = 0;
        public const int Standard = 1;
        public const int Fine = 2;

        public static int Get(ItemStack stack)
        {
            if (stack?.Attributes == null) return Standard;
            return stack.Attributes.GetInt(AttrKey, Standard);
        }

        public static void Set(ItemStack stack, int quality)
        {
            if (stack?.Attributes == null) return;
            if (quality == Standard)
                stack.Attributes.RemoveAttribute(AttrKey);
            else
                stack.Attributes.SetInt(AttrKey, GameMath_Clamp(quality, Coarse, Fine));
        }

        /// <summary>Copy quality from a source stack onto a freshly produced stack.</summary>
        public static void Carry(ItemStack from, ItemStack to)
        {
            Set(to, Get(from));
        }

        public static int GetPotential(ItemStack stack)
        {
            if (stack?.Attributes == null) return Unset;
            return stack.Attributes.GetInt(PotentialAttrKey, Unset);
        }

        public static void SetPotential(ItemStack stack, int potential)
        {
            if (stack?.Attributes == null) return;
            if (potential < Standard)
                stack.Attributes.RemoveAttribute(PotentialAttrKey);
            else
                stack.Attributes.SetInt(PotentialAttrKey, GameMath_Clamp(potential, Standard, Fine));
        }

        /// <summary>Copy the harvest potential from a source bundle onto a freshly produced bundle.</summary>
        public static void CarryPotential(ItemStack from, ItemStack to)
        {
            SetPotential(to, GetPotential(from));
        }

        /// <summary>The quality a bundle converts at when retting completes, given its potential.</summary>
        public static int MinQualityFor(int potential)
        {
            return potential >= Fine ? Standard : Coarse;
        }

        /// <summary>Yield multiplier applied to fibers based on quality.</summary>
        public static float YieldMultiplier(int quality)
        {
            switch (quality)
            {
                case Coarse: return 0.8f;
                case Fine: return 1.35f;
                default: return 1f;
            }
        }

        public static string Name(int quality)
        {
            return Lang.Get("rudiments:fiberquality-" + GameMath_Clamp(quality, Coarse, Fine));
        }

        private static int GameMath_Clamp(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }

    /// <summary>
    /// Attached to fiber bundle / fiber items via JSON behaviors. Surfaces the carried fiber quality
    /// in the held-item tooltip so the player can see the effect of their retting and handling choices.
    /// </summary>
    public class FiberQualityBehavior : CollectibleBehavior
    {
        public FiberQualityBehavior(CollectibleObject collObj) : base(collObj) { }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            ItemStack stack = inSlot?.Itemstack;
            if (stack == null || stack.Attributes == null) return;

            if (stack.Attributes.HasAttribute(FiberQuality.AttrKey))
            {
                int q = FiberQuality.Get(stack);
                dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(q)));
            }

            int potential = FiberQuality.GetPotential(stack);
            if (potential == FiberQuality.Fine)
            {
                dsc.AppendLine(Lang.Get("rudiments:fiberpotential-bloom"));
            }
            else if (potential == FiberQuality.Standard)
            {
                dsc.AppendLine(Lang.Get("rudiments:fiberpotential-mature"));
            }
        }
    }
}
