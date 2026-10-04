using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.Utils
{
    /// <summary>
    /// Makes Immersive Fibercraft's drop spindle consume Rudiments fibers at the same ratio as its
    /// spinning wheel. Both read <c>spinningProps.inputQuantity</c>, but the wheel takes it once per
    /// twine while the spindle (<c>ItemDropSpindle.ProcessSpin</c>) takes it on every spin, and a
    /// twine takes <c>spinsPerCompletion</c> (2) spins — so 2 rolags -> 1 twine on the wheel cost 4
    /// on the spindle. Lowering inputQuantity would just break the wheel instead, so the amount
    /// handed to the spindle's TakeOut call is rewritten here to split inputQuantity across the
    /// spins. Only items in the Rudiments domain are touched; Immersive Fibercraft's own fibers
    /// keep whatever ratio it gives them.
    /// </summary>
    public class DropSpindleRatioFix : ModSystem
    {
        const string HarmonyId = "rudiments.dropspindleratio";

        Harmony harmony;
        static bool foundTakeOut;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            if (!api.ModLoader.IsModEnabled("spinningwheel")) return;

            MethodInfo processSpin = AccessTools.Method("SpinningWheel.Items.ItemDropSpindle:ProcessSpin");
            if (processSpin == null)
            {
                api.Logger.Warning("[rudiments] Drop spindle ratio fix: ItemDropSpindle.ProcessSpin not found — Immersive Fibercraft changed; rolags will spin at its own per-spin ratio.");
                return;
            }

            foundTakeOut = false;
            harmony = new Harmony(HarmonyId);
            harmony.Patch(processSpin, transpiler: new HarmonyMethod(typeof(DropSpindleRatioFix), nameof(Transpiler)));

            if (!foundTakeOut)
            {
                api.Logger.Warning("[rudiments] Drop spindle ratio fix: no TakeOut call in ItemDropSpindle.ProcessSpin — Immersive Fibercraft changed; rolags will spin at its own per-spin ratio.");
                return;
            }
            api.Logger.Notification("[{0}] Drop spindle compat: Rudiments fibers now cost the same on the drop spindle as on the spinning wheel.", Mod.Info.Name);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(HarmonyId);
            base.Dispose();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo takeOut = AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.TakeOut));
            MethodInfo perSpin = AccessTools.Method(typeof(DropSpindleRatioFix), nameof(QuantityThisSpin));
            foreach (CodeInstruction ins in instructions)
            {
                // Stack is [fiberSlot, inputQuantity]; push the slots so QuantityThisSpin can
                // replace inputQuantity with this spin's share before TakeOut sees it.
                if (!foundTakeOut && ins.Calls(takeOut))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);   // spindleSlot
                    yield return new CodeInstruction(OpCodes.Ldarg_2);   // fiberSlot
                    yield return new CodeInstruction(OpCodes.Call, perSpin);
                    foundTakeOut = true;
                }
                yield return ins;
            }
        }

        /// <summary>
        /// This spin's share of <paramref name="inputQuantity"/>, so the shares over one twine sum to
        /// exactly inputQuantity (2 -> 1+1, 3 -> 1+2, 4 -> 2+2). ProcessSpin has already stored the
        /// incremented spin count when it calls TakeOut, so <c>spins</c> is this spin's 1-based index.
        /// </summary>
        public static int QuantityThisSpin(int inputQuantity, ItemSlot spindleSlot, ItemSlot fiberSlot)
        {
            if (fiberSlot?.Itemstack?.Collectible?.Code?.Domain != "rudiments") return inputQuantity;

            ItemStack spindle = spindleSlot?.Itemstack;
            if (spindle == null) return inputQuantity;

            int spinsPerTwine = spindle.Collectible.Attributes?["spinsPerCompletion"].AsInt(2) ?? 2;
            if (spinsPerTwine <= 1) return inputQuantity;

            int spin = GameMath.Clamp(spindle.Attributes.GetInt("spins", 1), 1, spinsPerTwine);
            return spin * inputQuantity / spinsPerTwine - (spin - 1) * inputQuantity / spinsPerTwine;
        }
    }
}
