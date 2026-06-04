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
            if (stack == null) return;
            if (stack.Attributes == null || !stack.Attributes.HasAttribute(FiberQuality.AttrKey)) return;

            int q = FiberQuality.Get(stack);
            dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(q)));
        }
    }
}
