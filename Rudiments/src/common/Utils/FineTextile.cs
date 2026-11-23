using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>How a collectible takes part in carrying the fine mark through crafting.</summary>
    public enum FineRole
    {
        /// <summary>Linen, cloth (and any clothing): carries its mark into what it is made into.</summary>
        Fabric,
        /// <summary>Bandages, poultices, fishing poles: made fine, and carry it on (alcohol, bait).</summary>
        Product,
        /// <summary>Sewing kit: can be fine, but never makes what it sews fine.</summary>
        Tool,
    }

    /// <summary>Implemented by the behaviours that put a vanilla collectible into the fine-textile chain.</summary>
    public interface IFineTextile
    {
        FineRole Role { get; }
    }

    /// <summary>
    /// Fine textiles: vanilla linen, cloth, clothing, sewing kits, bandages, poultices and fishing
    /// poles made from fine cord carry <c>fiberquality = 2</c> (<see cref="FiberQuality.Fine"/>).
    ///
    /// <para>The mark is read with <c>GetAsInt</c> because recipe output attributes load as
    /// <c>LongAttribute</c>, and always written with <c>SetInt</c>; <see cref="ApplyCraftMark"/>
    /// normalises it, or a recipe-made fine stack would not merge with a code-marked one. It must
    /// never be added to <c>GlobalConstants.IgnoredStackAttributes</c>: fine and plain stacks would
    /// then merge and the mark would be lost.</para>
    /// </summary>
    public static class FineTextile
    {
        /// <summary>Sewing kit stack attribute: how much of the top kit's thread is used (0..1).</summary>
        public const string WearKey = "rudimentskitwear";

        public static bool IsFineCord(ItemStack s)
        {
            AssetLocation code = s?.Collectible?.Code;
            return code != null && code.Domain == "rudiments" && code.Path == "finecord";
        }

        public static bool IsSewingKit(ItemStack s)
        {
            AssetLocation code = s?.Collectible?.Code;
            return code != null && code.Domain == "game" && code.Path == "sewingkit";
        }

        /// <summary>Fine cord always is; anything else is when it carries the mark.</summary>
        public static bool IsFine(ItemStack s)
        {
            if (s?.Collectible == null) return false;
            if (IsFineCord(s)) return true;
            return s.Attributes != null && s.Attributes.GetAsInt(FiberQuality.AttrKey, FiberQuality.Standard) == FiberQuality.Fine;
        }

        public static void SetFine(ItemStack s) => s?.Attributes?.SetInt(FiberQuality.AttrKey, FiberQuality.Fine);

        /// <summary>Clothing or armour, including other mods' subclasses of the vanilla behaviour.</summary>
        public static bool IsWearable(CollectibleObject c) => c?.GetCollectibleInterface<CollectibleBehaviorWearable>() != null;

        /// <summary>Our role for a collectible, or null if it is not in the chain. Clothing counts as fabric.</summary>
        public static FineRole? RoleOf(CollectibleObject c)
        {
            if (c == null) return null;
            IFineTextile ft = c.GetCollectibleInterface<IFineTextile>();
            if (ft != null) return ft.Role;
            return IsWearable(c) ? FineRole.Fabric : null;
        }

        /// <summary>Whether a stack can carry the mark at all (the <c>/rudimentsfine mark</c> test aid).
        /// Fibre bundles are excluded on purpose: the same key is their retting quality.</summary>
        public static bool CanMark(ItemStack s) => s?.Collectible != null && !IsFineCord(s) && RoleOf(s.Collectible) != null;

        /// <summary>Fine armour durability, computed live from the mark so a changed setting or a
        /// marked stack is always honoured. Read by <c>DurabilityBonusBehavior</c>, which is on vanilla
        /// armour only, so the tooltip only promises it there.</summary>
        public static int ArmorBonus(CollectibleObject coll, ItemStack stack, int baseDurability)
        {
            if (baseDurability <= 0 || FineTextileSettings.ArmorDurabilityBonus <= 0f || !IsFine(stack)) return 0;
            CollectibleBehaviorWearable wearable = coll?.GetCollectibleInterface<CollectibleBehaviorWearable>();
            if (wearable == null || !wearable.IsArmorType(new DummySlot(stack))) return 0;
            return (int)Math.Round(baseDurability * FineTextileSettings.ArmorDurabilityBonus);
        }

        /// <summary>Inputs whose fineness decides the output's: fine cord, fabric and products.</summary>
        public static bool IsCarrier(ItemStack s)
        {
            if (s?.Collectible == null) return false;
            if (IsFineCord(s)) return true;
            FineRole? role = RoleOf(s.Collectible);
            return role == FineRole.Fabric || role == FineRole.Product;
        }

        /// <summary>
        /// The carry rule. An input of the same collectible as the output (a repair, re-baiting a
        /// pole) passes its own mark through. Otherwise the output is fine when there is at least one
        /// carrier and every carrier is fine. Thread (flax twine), tools, pelts, sticks, dyes and
        /// liquids are not carriers, and neither is other mods' cloth.
        /// </summary>
        public static void ApplyCraftMark(ItemSlot[] inputs, ItemSlot output)
        {
            ItemStack outStack = output?.Itemstack;
            if (outStack?.Collectible == null || inputs == null) return;

            bool any = false, all = true;
            foreach (ItemSlot slot in inputs)
            {
                ItemStack s = slot?.Itemstack;
                if (s?.Collectible == null) continue;

                if (s.Collectible == outStack.Collectible && RoleOf(s.Collectible) != null)
                {
                    if (IsFine(s)) SetFine(outStack);
                    return;
                }
                if (!IsCarrier(s)) continue;
                any = true;
                if (!IsFine(s)) all = false;
            }

            if ((any && all) || IsFine(outStack)) SetFine(outStack);
        }

        /// <summary>Repairs per sewing kit for this stack.</summary>
        public static float UsesFor(ItemStack kit) =>
            Math.Max(0.1f, IsFine(kit) ? FineTextileSettings.FineKitUses : FineTextileSettings.KitUses);

        /// <summary>"Fine linen" for a marked stack. Fine cord has its own name.</summary>
        public static void WrapName(StringBuilder sb, ItemStack s)
        {
            if (sb == null || !IsFine(s) || IsFineCord(s)) return;
            string name = sb.ToString();
            sb.Clear();
            sb.Append(Lang.Get("rudiments:fine-name", name));
        }

        /// <summary>Tooltip lines for any stack in the chain. Shared by our behaviours and the
        /// Harmony postfixes on clothing and the fishing pole.</summary>
        public static void AppendInfo(ItemSlot slot, StringBuilder dsc)
        {
            ItemStack s = slot?.Itemstack;
            if (s?.Collectible == null || dsc == null || IsFineCord(s)) return;
            bool fine = IsFine(s);
            CollectibleObject coll = s.Collectible;

            if (IsSewingKit(s))
            {
                if (fine) dsc.AppendLine(Lang.Get("rudiments:fine-kit", Pct(FineTextileSettings.KitRepair)));
                float uses = UsesFor(s);
                if (Math.Abs(uses - 1f) > 0.001f)
                {
                    int used = (int)Math.Round(s.Attributes.GetFloat(WearKey) * 100);
                    dsc.AppendLine(Lang.Get("rudiments:kit-uses", uses.ToString("0.##"), used));
                }
            }
            else if (fine)
            {
                CollectibleBehaviorWearable wearable = coll.GetCollectibleInterface<CollectibleBehaviorWearable>();
                if (wearable != null)
                {
                    if (wearable.IsArmorType(slot))
                    {
                        if (coll.GetCollectibleInterface<DurabilityBonusBehavior>() != null && FineTextileSettings.ArmorDurabilityBonus > 0f)
                        {
                            dsc.AppendLine(Lang.Get("rudiments:fine-armor", Pct(FineTextileSettings.ArmorDurabilityBonus)));
                        }
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("rudiments:fine-clothing",
                            Pct(FineTextileSettings.WarmthMul - 1), Pct(1 - FineTextileSettings.WearMul)));
                    }
                }
                else if (coll.GetCollectibleInterface<CollectibleBehaviorHealingItem>() != null)
                {
                    dsc.AppendLine(Lang.Get("rudiments:fine-healing", Pct(FineTextileSettings.HealMul - 1)));
                }
                else if (coll is ItemFishingPole)
                {
                    dsc.AppendLine(Lang.Get("rudiments:fine-line",
                        Pct(FineTextileSettings.LineStrengthMul - 1), Pct(1 - FineTextileSettings.BiteDelayMul)));
                }
                else if (RoleOf(coll) == FineRole.Fabric)
                {
                    dsc.AppendLine(Lang.Get("rudiments:fine-fabric", Pct(FineTextileSettings.ClothRepair)));
                }
            }

            if (fine && Math.Abs(FineTextileSettings.TradeMul - 1f) > 0.001f)
            {
                dsc.AppendLine(Lang.Get("rudiments:fine-trade", FineTextileSettings.TradeMul.ToString("0.##")));
            }
        }

        private static int Pct(float fraction) => (int)Math.Round(fraction * 100);
    }

    /// <summary>
    /// The fine-textile numbers, as the server decided them. Loaded from the config at start, then
    /// published to the world config (server) or adopted from it (client), like the module switches,
    /// so client tooltips and trader totals use the server's numbers.
    /// </summary>
    public static class FineTextileSettings
    {
        // Defaults live in RudimentsConfig only; the static constructor loads them from there.
        public static float CordRepair, ClothRepair, KitRepair, KitUses, FineKitUses,
            WarmthMul, WearMul, ArmorDurabilityBonus, HealMul, TradeMul,
            LineStrengthMul, BiteDelayMul, CatchWindowBonusSec;

        static FineTextileSettings() => Load(new RudimentsConfig());

        private const string Prefix = "Rudiments.Fine.";

        private static readonly (string Name, Func<float> Get, Action<float> Set)[] Fields =
        {
            ("CordRepair", () => CordRepair, v => CordRepair = v),
            ("ClothRepair", () => ClothRepair, v => ClothRepair = v),
            ("KitRepair", () => KitRepair, v => KitRepair = v),
            ("KitUses", () => KitUses, v => KitUses = v),
            ("FineKitUses", () => FineKitUses, v => FineKitUses = v),
            ("WarmthMul", () => WarmthMul, v => WarmthMul = v),
            ("WearMul", () => WearMul, v => WearMul = v),
            ("ArmorDurabilityBonus", () => ArmorDurabilityBonus, v => ArmorDurabilityBonus = v),
            ("HealMul", () => HealMul, v => HealMul = v),
            ("TradeMul", () => TradeMul, v => TradeMul = v),
            ("LineStrengthMul", () => LineStrengthMul, v => LineStrengthMul = v),
            ("BiteDelayMul", () => BiteDelayMul, v => BiteDelayMul = v),
            ("CatchWindowBonusSec", () => CatchWindowBonusSec, v => CatchWindowBonusSec = v),
        };

        public static void Load(RudimentsConfig c)
        {
            CordRepair = Math.Max(0f, c.FineCordRepairStrength);
            ClothRepair = Math.Max(0f, c.FineClothRepairStrength);
            KitRepair = Math.Max(0f, c.FineSewingKitRepairStrength);
            KitUses = Math.Max(0.1f, c.SewingKitUses);
            FineKitUses = Math.Max(0.1f, c.FineSewingKitUses);
            // Never 0: vanilla treats clothing with no max warmth as not clothing (no mending, no condition).
            WarmthMul = Math.Max(0.1f, c.FineClothingWarmthMultiplier);
            WearMul = Math.Max(0f, c.FineClothingWearMultiplier);
            ArmorDurabilityBonus = Math.Max(0f, c.FineArmorDurabilityBonus);
            HealMul = Math.Max(0f, c.FineHealingMultiplier);
            TradeMul = Math.Max(0f, c.FineTradeMultiplier);
            LineStrengthMul = Math.Max(0.1f, c.FineLineStrengthMultiplier);
            BiteDelayMul = Math.Max(0.05f, c.FineLineBiteDelayMultiplier);
            CatchWindowBonusSec = Math.Max(0f, c.FineLineCatchWindowBonusSec);
        }

        /// <summary>Server: publish for clients.</summary>
        public static void Publish(ITreeAttribute worldConfig)
        {
            foreach (var (name, get, _) in Fields) worldConfig.SetFloat(Prefix + name, get());
        }

        /// <summary>Client: adopt the server's numbers. Keys that are missing keep the local value.</summary>
        public static void Adopt(ITreeAttribute worldConfig)
        {
            if (worldConfig == null || !worldConfig.HasAttribute(Prefix + "TradeMul")) return;
            foreach (var (name, get, set) in Fields) set(worldConfig.GetFloat(Prefix + name, get()));
        }

        public static string Describe()
        {
            var sb = new StringBuilder();
            foreach (var (name, get, _) in Fields) sb.Append(name).Append('=').Append(get().ToString("0.###")).Append("  ");
            return sb.ToString().TrimEnd();
        }
    }
}
