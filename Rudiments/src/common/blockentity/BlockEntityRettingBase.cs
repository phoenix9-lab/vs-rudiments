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
    /// Abstract base for placed retting block entities (field/dew retting, and the legacy retting
    /// vat). The shared 4-stage quality state machine now lives in <see cref="RettingProcess"/>;
    /// this class owns the inventory, loads the tunable thresholds from JSON, supplies the progress
    /// rate, and renders the block info. Subclasses supply the inventory key, language prefix,
    /// progress rate, and threshold defaults.
    /// </summary>
    public abstract class BlockEntityRettingBase : BlockEntity
    {
        protected InventoryGeneric inventory;
        public ItemSlot FiberSlot => inventory[0];

        /// <summary>Shared retting state machine (progress, quality, rot, lime).</summary>
        protected readonly RettingProcess process = new RettingProcess();

        // --- tunable thresholds (loaded by subclass then optionally overridden from JSON) ---
        protected double MinRetHours;
        protected double FineChance;
        protected double FineDelayMin;
        protected double FineDelayMax;
        protected double FineWindow;
        protected double StandardDelay;
        protected double StandardHold;

        /// <summary>Lime retting modifier — forwards to the shared process so subclasses (vat) keep working.</summary>
        protected bool limeActive
        {
            get => process.LimeActive;
            set => process.LimeActive = value;
        }

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
            if (attrs != null)
            {
                MinRetHours    = attrs["minRetHours"].AsDouble(MinRetHours);
                FineChance     = attrs["fineChance"].AsDouble(FineChance);
                FineDelayMin   = attrs["fineDelayMinHours"].AsDouble(FineDelayMin);
                FineDelayMax   = attrs["fineDelayMaxHours"].AsDouble(FineDelayMax);
                FineWindow     = attrs["fineWindowHours"].AsDouble(FineWindow);
                StandardDelay  = attrs["standardDelayHours"].AsDouble(StandardDelay);
                StandardHold   = attrs["standardHoldHours"].AsDouble(StandardHold);
            }

            process.MinRetHours   = MinRetHours;
            process.FineChance    = FineChance;
            process.FineDelayMin  = FineDelayMin;
            process.FineDelayMax  = FineDelayMax;
            process.FineWindow    = FineWindow;
            process.StandardDelay = StandardDelay;
            process.StandardHold  = StandardHold;
        }

        /// <summary>Returns the retted output AssetLocation for a rettable input stack, or null if not rettable.</summary>
        public static AssetLocation GetRettedOutput(ItemStack stack) => RettingProcess.GetRettedOutput(stack);

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
                process.Reset(Api.World.Calendar.TotalHours, false);
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
                process.Reset(Api.World.Calendar.TotalHours, true);
                OnFibersInserted();
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            // Stack-merge only identical bundles (same code, quality and harvest potential),
            // so a batch never mixes grades that would ret differently.
            if (slot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code)
                && FiberQuality.Get(slot.Itemstack) == FiberQuality.Get(heldItem)
                && FiberQuality.GetPotential(slot.Itemstack) == FiberQuality.GetPotential(heldItem))
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

        private void OnGameTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (process.Advance(Api.World, FiberSlot, GetProgressRate()))
            {
                MarkDirty();
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
                inventory = new InventoryGeneric(InventorySlots, InvKey + "-" + Pos, Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            process.FromTree(tree);

            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            process.ToTree(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (FiberSlot.Empty)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-empty"));
                return;
            }

            if (process.LimeActive)
            {
                dsc.AppendLine(Lang.Get("rudiments:rettingvat-lime-active"));
            }

            process.AppendStatus(dsc, LangPrefix, FiberSlot, Api.World.Calendar.HoursPerDay);
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
