using Rudiments.Utils;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Abstract base for all retting block entities. Implements the shared 4-stage quality
    /// state machine: input converts to retted (Coarse) at MinRetHours, then a RNG roll
    /// decides whether a Fine window opens; after that quality transitions deterministically
    /// Standard → Rot. Subclasses supply the inventory key, language prefix, progress rate,
    /// and tunable threshold defaults.
    /// </summary>
    public abstract class BlockEntityRettingBase : BlockEntity
    {
        protected InventoryGeneric inventory;
        public ItemSlot FiberSlot => inventory[0];

        // --- state machine fields (all persisted) ---
        private double retProgressHours;
        private double lastTotalHours = -1;
        private bool converted;
        private bool rolled;
        private bool willBeFine;
        private double tFineStart;
        private double tFineEnd;
        private double tStandard;
        private double tRot;
        private bool rotted;

        // --- tunable thresholds (loaded by subclass then optionally overridden from JSON) ---
        protected double MinRetHours;
        protected double FineChance;
        protected double FineDelayMin;
        protected double FineDelayMax;
        protected double FineWindow;
        protected double StandardDelay;
        protected double StandardHold;

        // --- lime retting modifier (active for this session, persisted) ---
        protected bool limeActive;

        // Lime modifier constants: applied when quicklime is present at retting start.
        // Faster but quality-capped: no fine window, and rot arrives sooner.
        protected const double LimeRateMultiplier  = 2.5;   // retting completes ~2.5× faster
        protected const double LimeStandardHoldMul = 0.5;   // StandardHold halved — tighter rot window

        protected AssetLocation interactSound;

        // --- subclass contract ---
        protected abstract string InvKey { get; }
        protected abstract string LangPrefix { get; }
        protected virtual double GetProgressRate() => 1.0;
        protected abstract void LoadDefaults();
        /// <summary>Number of inventory slots. Base = 1 (fiber only). Override to add modifier slots.</summary>
        protected virtual int InventorySlots => 1;
        /// <summary>Called when fibres are first inserted into an empty fiber slot. Override to apply modifiers (e.g. lime).</summary>
        protected virtual void OnFibersInserted() { }

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null)
            {
                inventory = new InventoryGeneric(InventorySlots, InvKey + "-" + Pos, api);
            }
            base.Initialize(api);
            inventory.LateInitialize(InvKey + "-" + Pos, api);

            interactSound = new AssetLocation("game", "sounds/block/water");
            LoadTunables();
            RegisterGameTickListener(OnGameTick, 1000);
        }

        private void LoadTunables()
        {
            LoadDefaults();
            var attrs = Block?.Attributes;
            if (attrs == null) return;
            MinRetHours    = attrs["minRetHours"].AsDouble(MinRetHours);
            FineChance     = attrs["fineChance"].AsDouble(FineChance);
            FineDelayMin   = attrs["fineDelayMinHours"].AsDouble(FineDelayMin);
            FineDelayMax   = attrs["fineDelayMaxHours"].AsDouble(FineDelayMax);
            FineWindow     = attrs["fineWindowHours"].AsDouble(FineWindow);
            StandardDelay  = attrs["standardDelayHours"].AsDouble(StandardDelay);
            StandardHold   = attrs["standardHoldHours"].AsDouble(StandardHold);
        }

        /// <summary>Returns the retted output AssetLocation for a rettable input stack, or null if not rettable.</summary>
        public static AssetLocation GetRettedOutput(ItemStack stack)
        {
            if (stack?.Collectible?.Code == null) return null;
            string path = stack.Collectible.Code.Path;
            if (path == "flaxbundle-rippled")   return stack.Collectible.Code.CopyWithPath("flaxbundle-retted");
            if (path == "nettlebundle-cured")   return stack.Collectible.Code.CopyWithPath("nettlebundle-retted");
            return null;
        }

        public virtual bool OnInteract(IPlayer player)
        {
            var heldSlot = player.InventoryManager.ActiveHotbarSlot;
            var heldItem = heldSlot?.Itemstack;
            var slot = FiberSlot;

            // Retrieve: empty hand + something in slot → give back to player
            if (heldItem == null && !slot.Empty && player.InventoryManager.TryGiveItemstack(slot.Itemstack.Clone()))
            {
                slot.Itemstack = null;
                slot.MarkDirty();
                ResetState(false);
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            if (GetRettedOutput(heldItem) == null) return false;

            // Insert into empty slot
            if (slot.Empty)
            {
                slot.Itemstack = heldItem.Clone();
                heldSlot.TakeOutWhole();
                heldSlot.MarkDirty();
                slot.MarkDirty();
                ResetState(true);
                OnFibersInserted();
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            // Stack-merge if same item
            if (slot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code))
            {
                int transferable = Math.Min(heldItem.StackSize, slot.Itemstack.Collectible.MaxStackSize - slot.Itemstack.StackSize);
                if (transferable <= 0) return true;
                slot.Itemstack.StackSize += transferable;
                heldSlot.TakeOut(transferable);
                heldSlot.MarkDirty();
                slot.MarkDirty();
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            return false;
        }

        private void ResetState(bool isInsert)
        {
            retProgressHours = 0;
            lastTotalHours = isInsert ? Api.World.Calendar.TotalHours : -1;
            converted = false;
            rolled = false;
            willBeFine = false;
            rotted = false;
            tFineStart = 0;
            tFineEnd = 0;
            tStandard = 0;
            tRot = 0;
            limeActive = false;
        }

        private void OnGameTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            double now = Api.World.Calendar.TotalHours;

            if (FiberSlot.Empty || rotted)
            {
                lastTotalHours = now;
                return;
            }

            bool isInput = GetRettedOutput(FiberSlot.Itemstack) != null;
            if (!isInput && !converted)
            {
                lastTotalHours = now;
                return;
            }

            if (lastTotalHours < 0) { lastTotalHours = now; return; }
            double elapsed = now - lastTotalHours;
            lastTotalHours = now;
            if (elapsed <= 0) return;

            double rate = GetProgressRate();
            if (limeActive) rate *= LimeRateMultiplier;
            retProgressHours += elapsed * rate;

            if (!converted && retProgressHours >= MinRetHours)
            {
                DoConvert();
            }

            if (converted && !rotted)
            {
                ApplyStage();
            }

            MarkDirty();
        }

        private void DoConvert()
        {
            ItemStack input = FiberSlot.Itemstack;
            AssetLocation outCode = GetRettedOutput(input);
            if (outCode == null) return;

            Item rettedItem = Api.World.GetItem(outCode);
            if (rettedItem == null) return;

            ItemStack s = new ItemStack(rettedItem, input.StackSize);
            FiberQuality.Set(s, FiberQuality.Coarse);
            FiberSlot.Itemstack = s;

            // Lime forces FineChance to 0 — the alkaline environment breaks fine fibre structure.
            double effectiveFineChance = limeActive ? 0.0 : FineChance;
            double effectiveStandardHold = limeActive ? StandardHold * LimeStandardHoldMul : StandardHold;

            // RNG roll: decide if/when a Fine window opens
            willBeFine = Api.World.Rand.NextDouble() < effectiveFineChance;
            if (willBeFine)
            {
                double delay = FineDelayMin + Api.World.Rand.NextDouble() * (FineDelayMax - FineDelayMin);
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

            tRot     = tStandard + effectiveStandardHold;
            rolled   = true;
            converted = true;

            FiberSlot.MarkDirty();
            MarkDirty(true);
        }

        private void ApplyStage()
        {
            double p = retProgressHours;

            if (p >= tRot)
            {
                DoRot();
                return;
            }

            int target;
            if (willBeFine && p >= tFineStart && p < tFineEnd)
                target = FiberQuality.Fine;
            else if (p >= tStandard)
                target = FiberQuality.Standard;
            else
                target = FiberQuality.Coarse;

            if (FiberQuality.Get(FiberSlot.Itemstack) != target)
            {
                FiberQuality.Set(FiberSlot.Itemstack, target);
                FiberSlot.MarkDirty();
            }
        }

        private void DoRot()
        {
            int stackSize = FiberSlot.Itemstack.StackSize;
            var rot = Api.World.GetItem(new AssetLocation("game", "rot"));
            if (rot != null)
            {
                FiberSlot.Itemstack = new ItemStack(rot, stackSize);
            }
            rotted = true;
            MarkDirty(true);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
                inventory = new InventoryGeneric(InventorySlots, InvKey + "-" + Pos, Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            retProgressHours = tree.GetDouble("retProgressHours");
            lastTotalHours   = tree.GetDouble("lastTotalHours", -1);
            converted        = tree.GetBool("converted");
            rolled           = tree.GetBool("rolled");
            willBeFine       = tree.GetBool("willBeFine");
            tFineStart       = tree.GetDouble("tFineStart");
            tFineEnd         = tree.GetDouble("tFineEnd");
            tStandard        = tree.GetDouble("tStandard");
            tRot             = tree.GetDouble("tRot");
            rotted           = tree.GetBool("rotted");
            limeActive       = tree.GetBool("limeActive");

            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetDouble("retProgressHours", retProgressHours);
            tree.SetDouble("lastTotalHours",   lastTotalHours);
            tree.SetBool("converted",          converted);
            tree.SetBool("rolled",             rolled);
            tree.SetBool("willBeFine",         willBeFine);
            tree.SetDouble("tFineStart",       tFineStart);
            tree.SetDouble("tFineEnd",         tFineEnd);
            tree.SetDouble("tStandard",        tStandard);
            tree.SetDouble("tRot",             tRot);
            tree.SetBool("rotted",             rotted);
            tree.SetBool("limeActive",         limeActive);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (FiberSlot.Empty)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-empty"));
                return;
            }

            if (limeActive)
            {
                dsc.AppendLine(Lang.Get("rudiments:rettingvat-lime-active"));
            }

            int count = FiberSlot.Itemstack.StackSize;

            if (rotted)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-rotted", count));
                return;
            }

            if (!converted)
            {
                int progress = (int)GameMath.Clamp(retProgressHours / MinRetHours * 100, 0, 100);
                dsc.AppendLine(Lang.Get(LangPrefix + "-progress", count, progress));
                return;
            }

            // Converted — show stage
            int q = FiberQuality.Get(FiberSlot.Itemstack);
            string stageKey;
            switch (q)
            {
                case FiberQuality.Fine:     stageKey = LangPrefix + "-stage-fine";     break;
                case FiberQuality.Standard: stageKey = LangPrefix + "-stage-standard"; break;
                default:                   stageKey = LangPrefix + "-stage-coarse";   break;
            }
            dsc.AppendLine(Lang.Get(stageKey, count));
            dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(q)));
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                for (int i = 0; i < inventory.Count; i++)
                {
                    if (!inventory[i].Empty)
                        Api.World.SpawnItemEntity(inventory[i].Itemstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
                }
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
