using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// The fine-textile bonuses, which all live in vanilla code (docs/fine-fibre-uses-plan.md,
    /// options 1–5): mending strength and sewing-kit uses, clothing warmth and wear, armour
    /// durability, the dye barrel carrying the mark, healing, trader prices and fishing line.
    ///
    /// <para>Loads on both sides, because tooltips and the trader's total are computed on the
    /// client too. Harmony patches are process-wide and single player runs client and server in one
    /// process, so the patches are applied once per process. Every target is optional: a missing
    /// one logs a warning and that bonus is simply off. Only applied with the Fibre group on.
    /// Settings come from <see cref="FineTextileSettings"/>, never from the live config.</para>
    /// </summary>
    public class FineTextilePatches : ModSystem
    {
        private const string HarmonyId = "rudiments.finetextiles";

        private static readonly object PatchLock = new();
        private static Harmony harmony;
        private static int applied, wanted;

        public override bool ShouldLoad(EnumAppSide side) => true;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            if (!RudimentsModules.Fibre) return;

            lock (PatchLock)
            {
                if (harmony != null)
                {
                    api.Logger.Notification("[{0}] Fine textiles: {1}/{2} hooks already applied in this process.", Mod.Info.Name, applied, wanted);
                    return;
                }
                harmony = new Harmony(HarmonyId);
                applied = wanted = 0;

                // Option 1: mending and sewing kits.
                TryPatch(api, "mending strength", AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.TryMergeStacks)), prefix: nameof(MendPrefix));
                TryPatch(api, "sewing kit returned by recipes", AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.MatchesForCrafting)), prefix: nameof(KitGuardPrefix));

                // Option 2: linen, cloth and clothing.
                TryPatch(api, "dye barrel", AccessTools.Method(typeof(BarrelRecipe), nameof(BarrelRecipe.TryCraftNow)), prefix: nameof(DyePrefix), postfix: nameof(DyePostfix));
                TryPatch(api, "clothing warmth", AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth)), postfix: nameof(WarmthPostfix));
                TryPatch(api, "clothing wear", AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.ChangeCondition)), prefix: nameof(WearPrefix));
                TryPatch(api, "clothing crafting", AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.OnCreatedByCrafting)), postfix: nameof(WearableCraftPostfix));
                TryPatch(api, "clothing tooltip", AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetHeldItemInfo)), postfix: nameof(WearableInfoPostfix));
                TryPatch(api, "clothing name", AccessTools.Method(typeof(CollectibleObject), nameof(CollectibleObject.GetHeldItemName), new[] { typeof(ItemStack) }), postfix: nameof(NamePostfix));

                // Option 3: bandages and poultices.
                TryPatch(api, "healing", AccessTools.Method(typeof(CollectibleBehaviorHealingItem), nameof(CollectibleBehaviorHealingItem.OnHeldInteractStop)), transpiler: nameof(HealStopTranspiler));
                TryPatch(api, "healing tooltip", AccessTools.Method(typeof(CollectibleBehaviorHealingItem), nameof(CollectibleBehaviorHealingItem.GetHeldItemInfo)), transpiler: nameof(HealInfoTranspiler));

                // Option 4: trader prices.
                TryPatch(api, "trader price", AccessTools.Method(typeof(InventoryTrader), nameof(InventoryTrader.GetTotalGain)), postfix: nameof(GainPostfix));

                // Option 5: fishing line. (ItemFishingPole.GetHeldItemInfo calls base, so the
                // behaviour's tooltip shows without a patch.)
                TryPatch(api, "fishing line strength", AccessTools.Method(typeof(ItemFishingPole), "createRope"), postfix: nameof(RopePostfix));
                TryPatch(api, "fishing line flag", AccessTools.Method(typeof(ItemFishingPole), "throwBobber"), postfix: nameof(ThrowPostfix));
                TryPatch(api, "fishing pole crafting", AccessTools.Method(typeof(ItemFishingPole), nameof(ItemFishingPole.OnCreatedByCrafting)), postfix: nameof(PoleCraftPostfix));
                TryPatch(api, "fishing bite and reel", AccessTools.Method(typeof(EntityBobber), "onServertick"), transpiler: nameof(BobberTranspiler));

                api.Logger.Notification("[{0}] Fine textiles: {1}/{2} hooks applied.", Mod.Info.Name, applied, wanted);
            }
        }

        public override void Dispose()
        {
            lock (PatchLock)
            {
                harmony?.UnpatchAll(HarmonyId);
                harmony = null;
            }
            base.Dispose();
        }

        /// <summary>Patch one target, or log why not. A transpiler that throws (its IL match
        /// failed) counts as not applied.</summary>
        private void TryPatch(ICoreAPI api, string what, MethodBase target, string prefix = null, string postfix = null, string transpiler = null)
        {
            wanted++;
            if (target == null)
            {
                api.Logger.Warning("[{0}] Fine textiles: {1} hook target not found — the game changed; that bonus is off.", Mod.Info.Name, what);
                return;
            }
            try
            {
                harmony.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(FineTextilePatches), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(FineTextilePatches), postfix),
                    transpiler: transpiler == null ? null : new HarmonyMethod(typeof(FineTextilePatches), transpiler));
                applied++;
            }
            catch (System.Exception e)
            {
                api.Logger.Warning("[{0}] Fine textiles: {1} hook failed ({2}) — that bonus is off.", Mod.Info.Name, what, e.Message);
            }
        }

        // ── Option 1: mending and sewing kits ────────────────────────────────────────────

        /// <summary>Vanilla CollectibleBehaviorWearable.TryMergeStacks(ItemStackMergeOperation op, ref EnumHandling handling).
        /// Vanilla sets MovedQuantity = 1 and takes the repair item out itself; ItemSlot.TryPutInto and
        /// the other merge callers do not take MovedQuantity off the source again, so we do the same.</summary>
        private static bool MendPrefix(CollectibleBehaviorWearable __instance, ItemStackMergeOperation op, ref EnumHandling handling)
        {
            if (op == null || op.CurrentPriority != EnumMergePriority.DirectMerge) return true;
            ItemSlot source = op.SourceSlot;
            ItemStack src = source?.Itemstack;
            ItemStack sink = op.SinkSlot?.Itemstack;
            if (src?.Collectible == null || sink?.Attributes == null) return true;

            float strength = src.ItemAttributes?["clothingRepairStrength"].AsFloat() ?? 0f;
            if (strength <= 0f || sink.Attributes.GetFloat("condition") >= 1f) return true;

            bool kit = FineTextile.IsSewingKit(src);
            bool fine = FineTextile.IsFine(src);
            float uses = 1f;
            if (kit)
            {
                uses = FineTextile.UsesFor(src);
                if (fine) strength = FineTextileSettings.KitRepair;
                else if (System.Math.Abs(uses - 1f) <= 0.001f) return true;
            }
            else if (fine && !FineTextile.IsFineCord(src)
                && FineTextile.RoleOf(src.Collectible) == FineRole.Fabric && !FineTextile.IsWearable(src.Collectible))
            {
                strength = FineTextileSettings.ClothRepair;
            }
            else
            {
                return true;   // fine cord is handled by its type-level strength; everything else is vanilla
            }

            if (!kit)
            {
                __instance.ChangeCondition(op.SinkSlot, strength);
                source.TakeOut(1);
            }
            else
            {
                // Thread, counted in kits. A repair takes 1/uses of a kit; WearKey is how much of the
                // top kit is used. When the last kit in the slot runs short, the repair is partial, so
                // a kit restores exactly strength × uses however the kits are split across slots.
                float need = 1f / uses;
                float wear = src.Attributes.GetFloat(FineTextile.WearKey);
                float used = System.Math.Min(need, source.StackSize - wear);
                __instance.ChangeCondition(op.SinkSlot, strength * used / need);

                wear += used;
                int spent = System.Math.Min(source.StackSize, (int)System.Math.Floor(wear + 0.0001f));
                if (spent > 0) source.TakeOut(spent);
                wear -= spent;
                if (source.Empty || wear <= 0.0001f) src.Attributes.RemoveAttribute(FineTextile.WearKey);
                else src.Attributes.SetFloat(FineTextile.WearKey, wear);
            }
            op.MovedQuantity = 1;

            source.MarkDirty();
            handling = EnumHandling.PreventDefault;
            return false;
        }

        /// <summary>Vanilla CollectibleObject.MatchesForCrafting(ItemStack inputStack, IRecipeBase recipe, IRecipeIngredient ingredient).
        /// A recipe that returns a (fresh, plain) sewing kit must not take a fine or part-used one.</summary>
        private static bool KitGuardPrefix(ItemStack inputStack, IRecipeIngredient ingredient, ref bool __result)
        {
            if (!FineTextile.IsSewingKit(inputStack) || ingredient?.ReturnedStack == null) return true;
            if (FineTextile.IsFine(inputStack) || inputStack.Attributes?.GetFloat(FineTextile.WearKey) > 0f)
            {
                __result = false;
                return false;
            }
            return true;
        }

        // ── Option 2: linen, cloth and clothing ──────────────────────────────────────────

        /// <summary>Vanilla BarrelRecipe.TryCraftNow(ICoreAPI api, double nowSealedHours, ItemSlot[] inputSlots).</summary>
        private static void DyePrefix(ItemSlot[] inputSlots, out bool __state)
        {
            __state = false;
            if (inputSlots == null) return;
            foreach (ItemSlot slot in inputSlots)
            {
                ItemStack s = slot?.Itemstack;
                if (s?.Collectible != null && FineTextile.IsFine(s) && FineTextile.RoleOf(s.Collectible) == FineRole.Fabric)
                {
                    __state = true;
                    return;
                }
            }
        }

        private static void DyePostfix(bool __result, ItemSlot[] inputSlots, bool __state)
        {
            if (!__result || !__state || inputSlots == null) return;
            for (int i = 0; i < 2 && i < inputSlots.Length; i++)
            {
                ItemStack s = inputSlots[i]?.Itemstack;
                if (s?.Collectible != null && FineTextile.RoleOf(s.Collectible) == FineRole.Fabric) FineTextile.SetFine(s);
            }
        }

        /// <summary>Vanilla CollectibleBehaviorWearable.GetMaxWarmth(ItemSlot inslot).</summary>
        private static void WarmthPostfix(ItemSlot inslot, ref float __result)
        {
            if (__result > 0f && FineTextile.IsFine(inslot?.Itemstack)) __result *= FineTextileSettings.WarmthMul;
        }

        /// <summary>Vanilla CollectibleBehaviorWearable.ChangeCondition(ItemSlot slot, float changeVal).</summary>
        private static void WearPrefix(ItemSlot slot, ref float changeVal)
        {
            if (changeVal < 0f && FineTextile.IsFine(slot?.Itemstack)) changeVal *= FineTextileSettings.WearMul;
        }

        /// <summary>Vanilla CollectibleBehaviorWearable.OnCreatedByCrafting(ItemSlot[] inSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling).</summary>
        private static void WearableCraftPostfix(CollectibleBehaviorWearable __instance, ItemSlot[] inSlots, ItemSlot outputSlot, IRecipeBase byRecipe)
        {
            ItemStack output = outputSlot?.Itemstack;
            if (output?.Collectible == null || outputSlot is DummySlot) return;

            // Fine armour's extra durability is computed live from the mark (FineTextile.ArmorBonus).
            FineTextile.ApplyCraftMark(inSlots, outputSlot);
        }

        /// <summary>Vanilla CollectibleBehaviorWearable.GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo).</summary>
        private static void WearableInfoPostfix(ItemSlot inSlot, StringBuilder dsc)
        {
            FineTextile.AppendInfo(inSlot, dsc);
        }

        /// <summary>Vanilla CollectibleObject.GetHeldItemName(ItemStack itemStack). Called a lot, so the cheap test is first.</summary>
        private static void NamePostfix(CollectibleObject __instance, ItemStack itemStack, ref string __result)
        {
            if (itemStack == null || !FineTextile.IsFine(itemStack) || FineTextile.IsFineCord(itemStack)) return;
            if (!FineTextile.IsWearable(__instance)) return;
            foreach (CollectibleBehavior b in __instance.CollectibleBehaviors) if (b is IFineTextile) return;
            __result = Lang.Get("rudiments:fine-name", __result);
        }

        // ── Option 3: bandages and poultices ─────────────────────────────────────────────

        /// <summary>Called in place of <c>get_Health</c>.</summary>
        public static float HealthFor(CollectibleBehaviorHealingItem bh, ItemSlot slot)
        {
            float health = bh.Health;
            return FineTextile.IsFine(slot?.Itemstack) ? health * FineTextileSettings.HealMul : health;
        }

        private static IEnumerable<CodeInstruction> HealStopTranspiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceHealth(instructions, OpCodes.Ldarg_2);

        private static IEnumerable<CodeInstruction> HealInfoTranspiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceHealth(instructions, OpCodes.Ldarg_1);

        private static List<CodeInstruction> ReplaceHealth(IEnumerable<CodeInstruction> instructions, OpCode slotArg)
        {
            MethodInfo getHealth = AccessTools.PropertyGetter(typeof(CollectibleBehaviorHealingItem), nameof(CollectibleBehaviorHealingItem.Health));
            MethodInfo healthFor = AccessTools.Method(typeof(FineTextilePatches), nameof(HealthFor));
            var list = new List<CodeInstruction>(instructions);
            int count = 0;
            foreach (CodeInstruction ins in list) if (getHealth != null && ins.Calls(getHealth)) count++;
            if (count != 1) throw new System.InvalidOperationException($"expected exactly 1 get_Health call, found {count}");

            var result = new List<CodeInstruction>(list.Count + 1);
            foreach (CodeInstruction ins in list)
            {
                if (ins.Calls(getHealth))
                {
                    // Reuse the original instruction as the ldarg so any labels on it stay valid.
                    ins.opcode = slotArg;
                    ins.operand = null;
                    result.Add(ins);
                    result.Add(new CodeInstruction(OpCodes.Call, healthFor));
                }
                else result.Add(ins);
            }
            return result;
        }

        // ── Option 4: trader prices ──────────────────────────────────────────────────────

        /// <summary>Vanilla InventoryTrader.GetTotalGain().</summary>
        private static void GainPostfix(InventoryTrader __instance, ref int __result)
        {
            float extra = FineTextileSettings.TradeMul - 1f;
            if (extra == 0f) return;
            for (int i = 0; i < 4; i++)
            {
                ItemStack stack = __instance.GetSellingCartSlot(i)?.Itemstack;
                if (stack == null || !FineTextile.IsFine(stack)) continue;
                if (stack.Attributes.GetFloat(FineTextile.WearKey) > 0f) continue;   // a part-used kit sells as plain
                ResolvedTradeItem t = __instance.GetBuyingConditionsSlot(stack)?.TradeItem;
                if (t?.Stack == null || t.Stack.StackSize <= 0) continue;
                int units = stack.StackSize / t.Stack.StackSize;
                __result += (int)System.Math.Round(t.Price * units * extra);
            }
        }

        // ── Option 5: fishing line ───────────────────────────────────────────────────────

        public const string FineLineKey = "rudimentsFineLine";

        /// <summary>Vanilla private ItemFishingPole.createRope(ItemSlot slot, EntityAgent byEntity, EntityBobber ebobber, Vec3d targetPos).</summary>
        private static void RopePostfix(ItemSlot slot, ClothSystem __result)
        {
            if (__result == null || !FineTextile.IsFine(slot?.Itemstack)) return;
            __result.StretchRip *= FineTextileSettings.LineStrengthMul;
            __result.StretchWarn *= FineTextileSettings.LineStrengthMul;
        }

        /// <summary>Vanilla private ItemFishingPole.throwBobber(ItemSlot slot, EntityAgent byEntity).</summary>
        private static void ThrowPostfix(ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity?.World == null || byEntity.World.Side != EnumAppSide.Server) return;
            ItemStack stack = slot?.Itemstack;
            if (stack?.Attributes == null || !FineTextile.IsFine(stack)) return;
            long id = stack.Attributes.GetLong("bobberEntityId", 0L);
            if (id == 0L) return;
            if (byEntity.World.GetEntityById(id) is EntityBobber bobber) bobber.WatchedAttributes.SetBool(FineLineKey, true);
        }

        /// <summary>Vanilla ItemFishingPole.OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe), which does not call base.</summary>
        private static void PoleCraftPostfix(ItemSlot[] allInputSlots, ItemSlot outputSlot)
        {
            FineTextile.ApplyCraftMark(allInputSlots, outputSlot);
        }

        /// <summary>Called in place of the constant 5.0 in EntityBobber.onServertick.</summary>
        public static double BiteDelay(EntityBobber bobber)
        {
            return bobber?.WatchedAttributes?.GetBool(FineLineKey) == true ? 5.0 * FineTextileSettings.BiteDelayMul : 5.0;
        }

        /// <summary>Called in place of the constant 0.7f in EntityBobber.onServertick.</summary>
        public static float CatchWindow(EntityBobber bobber)
        {
            return bobber?.WatchedAttributes?.GetBool(FineLineKey) == true ? 0.7f + FineTextileSettings.CatchWindowBonusSec : 0.7f;
        }

        private static IEnumerable<CodeInstruction> BobberTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            int delays = 0, windows = 0;
            foreach (CodeInstruction ins in list)
            {
                if (IsDouble(ins, 5.0)) delays++;
                else if (IsFloat(ins, 0.7f)) windows++;
            }
            if (delays != 1 || windows != 1)
                throw new System.InvalidOperationException($"expected exactly one 5.0 and one 0.7f in onServertick, found {delays} and {windows}");

            MethodInfo biteDelay = AccessTools.Method(typeof(FineTextilePatches), nameof(BiteDelay));
            MethodInfo catchWindow = AccessTools.Method(typeof(FineTextilePatches), nameof(CatchWindow));
            var result = new List<CodeInstruction>(list.Count + 2);
            foreach (CodeInstruction ins in list)
            {
                if (IsDouble(ins, 5.0) || IsFloat(ins, 0.7f))
                {
                    bool isDouble = ins.opcode == OpCodes.Ldc_R8;
                    // Reuse the original instruction as ldarg.0 so any labels on it stay valid.
                    ins.opcode = OpCodes.Ldarg_0;
                    ins.operand = null;
                    result.Add(ins);
                    result.Add(new CodeInstruction(OpCodes.Call, isDouble ? biteDelay : catchWindow));
                }
                else result.Add(ins);
            }
            return result;
        }

        private static bool IsDouble(CodeInstruction ins, double v) => ins.opcode == OpCodes.Ldc_R8 && ins.operand is double d && d == v;
        private static bool IsFloat(CodeInstruction ins, float v) => ins.opcode == OpCodes.Ldc_R4 && ins.operand is float f && f == v;
    }
}
