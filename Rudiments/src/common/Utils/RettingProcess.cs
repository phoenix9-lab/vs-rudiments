using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Rudiments.Utils
{
    /// <summary>
    /// Reusable 4-stage retting state machine, shared by every retting method
    /// (field/dew retting and barrel retting). Operates on a single fibre ItemSlot:
    /// the rettable input converts to retted at MinRetHours, after which quality settles
    /// Standard then Rot. The input bundle decides the quality range:
    ///   nettle                      → Coarse floor, capped Standard (always — never fine)
    ///   flax, Fine potential        → Standard floor, Fine window possible (cut in bloom)
    ///   flax, Standard potential    → Coarse floor, capped Standard (fully mature)
    ///   flax, no potential attr     → Coarse floor, Fine possible (legacy / FlaxBloomHarvest off)
    ///
    /// The owning block entity / behavior supplies the thresholds, the per-tick progress
    /// rate (weather for field, steady for barrel), and persists this via <see cref="ToTree"/>
    /// / <see cref="FromTree"/>. Lime retting is faster but quality-capped at Standard with a
    /// tighter rot window.
    /// </summary>
    public class RettingProcess
    {
        // --- tunable thresholds (set by owner; defaults match the vat) ---
        public double MinRetHours = 18;
        public double FineChance = 0.6;
        public double FineDelayMin = 6;
        public double FineDelayMax = 18;
        public double FineWindow = 12;
        public double StandardDelay = 24;
        public double StandardHold = 36;

        // Lime modifier constants: applied when quicklime / limewater is present at retting start.
        public const double LimeRateMultiplier  = 2.5;   // retting completes ~2.5× faster
        public const double LimeStandardHoldMul = 0.5;   // StandardHold halved — tighter rot window

        // --- state (all persisted; tree keys kept identical to the old BlockEntityRettingBase
        //     fields so existing saved field/vat retting batches load seamlessly) ---
        private double retProgressHours;
        private double lastTotalHours = -1;
        private bool converted;
        private bool rolled;
        private bool willBeFine;
        private bool fineObserved;   // the Fine window has been surfaced at least once
        private double tFineStart;
        private double tFineEnd;
        private double tStandard;
        private double tRot;
        private bool rotted;
        private int minQuality = FiberQuality.Coarse;   // conversion floor from harvest potential
        private int maxQuality = FiberQuality.Fine;     // quality ceiling from input type/potential

        /// <summary>Lime retting active for this batch (faster, no Fine, tighter rot).</summary>
        public bool LimeActive;

        public bool Rotted => rotted;
        public bool Converted => converted;

        /// <summary>0..100 progress toward the first (Coarse) conversion. Only meaningful pre-conversion.</summary>
        public int ProgressPercent => (int)GameMath.Clamp(retProgressHours / MinRetHours * 100, 0, 100);

        /// <summary>
        /// Retting progress (in hours) remaining until a Fine window could open. Computed from the
        /// earliest possible onset (MinRetHours + FineDelayMin) so it is identical whether or not the
        /// RNG roll actually granted Fine — it never leaks the outcome. &lt;= 0 once Fine is possible.
        /// </summary>
        public double FineEtaHours => (MinRetHours + FineDelayMin) - retProgressHours;

        /// <summary>Returns the retted output AssetLocation for a rettable input stack, or null if not rettable.</summary>
        public static AssetLocation GetRettedOutput(ItemStack stack)
        {
            if (stack?.Collectible?.Code == null) return null;
            string path = stack.Collectible.Code.Path;
            if (path == "flaxbundle-rippled") return stack.Collectible.Code.CopyWithPath("flaxbundle-retted");
            if (path == "nettlebundle-cured") return stack.Collectible.Code.CopyWithPath("nettlebundle-retted");
            return null;
        }

        /// <summary>Reset all batch state. Pass isInsert=true when fibres were just placed (starts the clock now).</summary>
        public void Reset(double now, bool isInsert)
        {
            retProgressHours = 0;
            lastTotalHours = isInsert ? now : -1;
            converted = false;
            rolled = false;
            willBeFine = false;
            fineObserved = false;
            rotted = false;
            tFineStart = 0;
            tFineEnd = 0;
            tStandard = 0;
            tRot = 0;
            minQuality = FiberQuality.Coarse;
            maxQuality = FiberQuality.Fine;
            LimeActive = false;
        }

        /// <summary>
        /// Advance the state machine. <paramref name="baseRate"/> is the method's progress multiplier
        /// (1.0 = real time; field retting scales by weather). Returns true if anything changed and the
        /// owner should MarkDirty. Server-side only — callers must guard the side.
        /// </summary>
        public bool Advance(IWorldAccessor world, ItemSlot slot, double baseRate)
        {
            double now = world.Calendar.TotalHours;

            if (slot.Empty || rotted)
            {
                lastTotalHours = now;
                return false;
            }

            bool isInput = GetRettedOutput(slot.Itemstack) != null;
            if (!isInput && !converted)
            {
                lastTotalHours = now;
                return false;
            }

            if (lastTotalHours < 0) { lastTotalHours = now; return false; }
            double elapsed = now - lastTotalHours;
            lastTotalHours = now;
            if (elapsed <= 0) return false;

            double rate = baseRate;
            if (LimeActive) rate *= LimeRateMultiplier;

            double prev = retProgressHours;
            retProgressHours = prev + elapsed * rate;

            if (!converted && retProgressHours >= MinRetHours)
            {
                DoConvert(world, slot);
            }

            if (converted && !rotted)
            {
                // Un-skippable Fine window: when a single step would leap from before the Fine
                // window to past its end (e.g. a chunk-unload/reload time jump applying many hours
                // at once), pin progress at the window entry the first time it is crossed. That
                // guarantees at least one tick marks the bundle Fine, so a present/returning player
                // can actually collect it instead of the window being silently skipped. Only the
                // first crossing is pinned (fineObserved) — afterwards it advances to Standard/Rot
                // normally, preserving the "catch it or lose it" design.
                if (willBeFine && !fineObserved && prev < tFineEnd && retProgressHours >= tFineEnd)
                {
                    retProgressHours = tFineStart;
                }
                ApplyStage(world, slot);
            }

            return true;
        }

        private void DoConvert(IWorldAccessor world, ItemSlot slot)
        {
            ItemStack input = slot.Itemstack;
            AssetLocation outCode = GetRettedOutput(input);
            if (outCode == null) return;

            Item rettedItem = world.GetItem(outCode);
            if (rettedItem == null) return;

            // The input decides the quality range (see class doc). Nettle is always capped at
            // Standard; flax follows its harvest potential, with the full legacy coarse-to-fine
            // range when no potential is stamped (old-save bundles, or FlaxBloomHarvest disabled).
            int potential = FiberQuality.GetPotential(input);
            bool isNettle = input.Collectible.Code.Path.StartsWith("nettlebundle");

            minQuality = FiberQuality.MinQualityFor(potential);
            if (isNettle) maxQuality = FiberQuality.Standard;
            else maxQuality = potential == FiberQuality.Unset ? FiberQuality.Fine : potential;

            ItemStack s = new ItemStack(rettedItem, input.StackSize);
            FiberQuality.Set(s, minQuality);
            slot.Itemstack = s;

            // Lime forces FineChance to 0 — the alkaline environment breaks fine fibre structure.
            double effectiveFineChance = (LimeActive || maxQuality < FiberQuality.Fine) ? 0.0 : FineChance;
            double effectiveStandardHold = LimeActive ? StandardHold * LimeStandardHoldMul : StandardHold;

            // RNG roll: decide if/when a Fine window opens.
            willBeFine = world.Rand.NextDouble() < effectiveFineChance;
            if (willBeFine)
            {
                double delay = FineDelayMin + world.Rand.NextDouble() * (FineDelayMax - FineDelayMin);
                tFineStart = MinRetHours + delay;
                tFineEnd   = tFineStart + FineWindow;
                tStandard  = tFineEnd;
            }
            else
            {
                tFineStart = -1;
                tFineEnd   = -1;
                tStandard  = MinRetHours + StandardDelay;
            }

            tRot      = tStandard + effectiveStandardHold;
            rolled    = true;
            converted = true;
            slot.MarkDirty();
        }

        private void ApplyStage(IWorldAccessor world, ItemSlot slot)
        {
            double p = retProgressHours;

            if (p >= tRot)
            {
                DoRot(world, slot);
                return;
            }

            int target;
            if (willBeFine && p >= tFineStart && p < tFineEnd)
                target = FiberQuality.Fine;
            else if (p >= tStandard)
                target = FiberQuality.Standard;
            else
                target = FiberQuality.Coarse;

            if (target < minQuality) target = minQuality;

            if (target == FiberQuality.Fine) fineObserved = true;

            if (FiberQuality.Get(slot.Itemstack) != target)
            {
                FiberQuality.Set(slot.Itemstack, target);
                slot.MarkDirty();
            }
        }

        private void DoRot(IWorldAccessor world, ItemSlot slot)
        {
            int stackSize = slot.Itemstack.StackSize;
            var rot = world.GetItem(new AssetLocation("game", "rot"));
            if (rot != null)
            {
                slot.Itemstack = new ItemStack(rot, stackSize);
            }
            rotted = true;
            slot.MarkDirty();
        }

        /// <summary>
        /// Append the live status line(s) for this batch to a block-info tooltip. <paramref name="langPrefix"/>
        /// selects the message set (e.g. "rudiments:fieldretting"). Expects the prefix to define
        /// -progress, -fine-eta, -stage-coarse/fine/standard and -rotted keys.
        /// </summary>
        public void AppendStatus(StringBuilder dsc, string langPrefix, ItemSlot slot, double hoursPerDay)
        {
            int count = slot.Itemstack.StackSize;

            if (rotted)
            {
                dsc.AppendLine(Lang.Get(langPrefix + "-rotted", count));
                return;
            }

            if (!converted)
            {
                dsc.AppendLine(Lang.Get(langPrefix + "-progress", count, ProgressPercent));
                return;
            }

            // Converted. Until a Fine window is even possible, show a countdown to that point
            // (deliberately vague: when it BECOMES possible, not exactly when/if Fine arrives).
            // Standard-capped batches (mature flax, nettle) never see a Fine window, so skip
            // the countdown for them — the cap is visible on the bundle, nothing to leak.
            if (maxQuality >= FiberQuality.Fine && FineEtaHours > 0)
            {
                dsc.AppendLine(Lang.Get(langPrefix + "-fine-eta", count, HumanizeHours(FineEtaHours, hoursPerDay)));
            }
            else
            {
                int q = FiberQuality.Get(slot.Itemstack);
                string stageKey;
                switch (q)
                {
                    case FiberQuality.Fine:     stageKey = langPrefix + "-stage-fine";     break;
                    case FiberQuality.Standard: stageKey = langPrefix + "-stage-standard"; break;
                    default:                    stageKey = langPrefix + "-stage-coarse";   break;
                }
                dsc.AppendLine(Lang.Get(stageKey, count));
            }

            dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(FiberQuality.Get(slot.Itemstack))));
        }

        /// <summary>Format an approximate retting-progress duration as "~N days" or "~N hours" text.</summary>
        public static string HumanizeHours(double hours, double hoursPerDay)
        {
            if (hours < 0) hours = 0;
            if (hoursPerDay <= 0) hoursPerDay = 24;
            if (hours >= hoursPerDay) return Lang.Get("{0} days", Math.Round(hours / hoursPerDay, 1));
            return Lang.Get("{0} hours", Math.Round(hours));
        }

        public void ToTree(ITreeAttribute tree)
        {
            tree.SetDouble("retProgressHours", retProgressHours);
            tree.SetDouble("lastTotalHours",   lastTotalHours);
            tree.SetBool("converted",          converted);
            tree.SetBool("rolled",             rolled);
            tree.SetBool("willBeFine",         willBeFine);
            tree.SetBool("fineObserved",       fineObserved);
            tree.SetDouble("tFineStart",       tFineStart);
            tree.SetDouble("tFineEnd",         tFineEnd);
            tree.SetDouble("tStandard",        tStandard);
            tree.SetDouble("tRot",             tRot);
            tree.SetBool("rotted",             rotted);
            tree.SetBool("limeActive",         LimeActive);
            tree.SetInt("minQuality",          minQuality);
            tree.SetInt("maxQuality",          maxQuality);
        }

        public void FromTree(ITreeAttribute tree)
        {
            retProgressHours = tree.GetDouble("retProgressHours");
            lastTotalHours   = tree.GetDouble("lastTotalHours", -1);
            converted        = tree.GetBool("converted");
            rolled           = tree.GetBool("rolled");
            willBeFine       = tree.GetBool("willBeFine");
            fineObserved     = tree.GetBool("fineObserved");
            tFineStart       = tree.GetDouble("tFineStart");
            tFineEnd         = tree.GetDouble("tFineEnd");
            tStandard        = tree.GetDouble("tStandard");
            tRot             = tree.GetDouble("tRot");
            rotted           = tree.GetBool("rotted");
            LimeActive       = tree.GetBool("limeActive");
            minQuality       = tree.GetInt("minQuality", FiberQuality.Coarse);
            maxQuality       = tree.GetInt("maxQuality", FiberQuality.Fine);
        }
    }
}
