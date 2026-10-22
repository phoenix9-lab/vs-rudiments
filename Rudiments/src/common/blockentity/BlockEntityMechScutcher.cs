using Rudiments.SRC.Common.Items;
using Rudiments.Utils;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Block entity for the mechanical scutcher. Holds an input slot (dried or broken bundles) and an
    /// output slot (scutched bundles). While the mechanical-power network is turning it, it consumes
    /// one bundle every few seconds and ejects the resulting scutched bundle, carrying the input's
    /// fibre quality. Hoppers and chutes can push bundles into the input and draw from the output, so
    /// a mill can run unattended between a feed hopper and a collecting chest.
    /// </summary>
    public class BlockEntityMechScutcher : BlockEntityContainer
    {
        protected InventoryGeneric inventory;
        protected BEBehaviorMPConsumer mpc;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "mechscutcher";

        public ItemSlot InputSlot => inventory[0];
        public ItemSlot OutputSlot => inventory[1];
        public bool IsEmpty => inventory.Empty;

        // Time in seconds per bundle at network speed 1.0
        private const float ProcessSecondsRequired = 5f;
        private float processProgress;

        private AssetLocation interactSound;

        public BlockEntityMechScutcher()
        {
            inventory = new InventoryGeneric(2, null, null);
            inventory.OnGetAutoPushIntoSlot = (face, fromSlot) => IsMillable(Api?.World, fromSlot?.Itemstack) ? InputSlot : null;
            inventory.OnGetAutoPullFromSlot = face => OutputSlot;
            inventory.SlotModified += OnSlotModified;
        }

        // Hoppers and chutes only mark their own block entity dirty, so sync the mill's load to
        // clients whenever a slot changes underneath it.
        private void OnSlotModified(int slotId)
        {
            if (Api?.Side == EnumAppSide.Server) MarkDirty();
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            interactSound = new AssetLocation("game", "sounds/block/planks");
            RegisterGameTickListener(OnMillTick, 100);
        }

        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            mpc = GetBehavior<BEBehaviorMPConsumer>();
        }

        /// <summary>Any flax or nettle bundle, whatever its stage — the ones the mill answers to,
        /// if only to say it can't take them.</summary>
        public static bool IsFibreBundle(ItemStack stack) => stack?.Collectible is ItemFieldRettableBundle;

        /// <summary>The scutched form of a dried or broken bundle; null for anything else. The mill
        /// breaks as it scutches, so a bundle that skipped the break goes through all the same.</summary>
        private static AssetLocation ScutchedCodeOf(ItemStack stack)
        {
            if (!IsFibreBundle(stack)) return null;
            string type = stack.Collectible.Variant?["type"];
            if (type != "dried" && type != "broken") return null;
            return stack.Collectible.CodeWithVariant("type", "scutched");
        }

        public static bool IsMillable(IWorldAccessor world, ItemStack stack)
        {
            AssetLocation scutchedCode = ScutchedCodeOf(stack);
            return scutchedCode != null && world?.GetItem(scutchedCode) != null;
        }

        public void OnInteract(IPlayer player)
        {
            var heldSlot = player.InventoryManager.ActiveHotbarSlot;
            var heldItem = heldSlot?.Itemstack;

            // Empty hand: take output first, otherwise pull the input back out.
            if (heldItem == null)
            {
                if (!OutputSlot.Empty && player.InventoryManager.TryGiveItemstack(OutputSlot.Itemstack.Clone()))
                {
                    OutputSlot.Itemstack = null;
                    OutputSlot.MarkDirty();
                    MarkDirty(true);
                    return;
                }
                if (!InputSlot.Empty && player.InventoryManager.TryGiveItemstack(InputSlot.Itemstack.Clone()))
                {
                    InputSlot.Itemstack = null;
                    processProgress = 0;
                    InputSlot.MarkDirty();
                    MarkDirty(true);
                }
                return;
            }

            if (!IsMillable(Api.World, heldItem))
            {
                Refuse(player, "scutcher-wronginput", "rudiments:scutcher-wronginput");
                return;
            }

            if (InputSlot.Empty)
            {
                InputSlot.Itemstack = heldItem.Clone();
                heldSlot.TakeOutWhole();
            }
            else if (InputSlot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code)
                     && FiberQuality.Get(InputSlot.Itemstack) == FiberQuality.Get(heldItem))
            {
                int transferable = System.Math.Min(heldItem.StackSize, InputSlot.Itemstack.Collectible.MaxStackSize - InputSlot.Itemstack.StackSize);
                if (transferable <= 0)
                {
                    Refuse(player, "scutcher-full", "rudiments:scutcher-full");
                    return;
                }
                InputSlot.Itemstack.StackSize += transferable;
                heldSlot.TakeOut(transferable);
            }
            else
            {
                Refuse(player, "scutcher-mismatch", "rudiments:scutcher-mismatch");
                return;
            }

            heldSlot.MarkDirty();
            InputSlot.MarkDirty();
            MarkDirty(true);
            Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.7f);
        }

        /// <summary>Loading is validated server-side, so refusals have to be pushed back to the
        /// player's client rather than raised locally.</summary>
        private void Refuse(IPlayer byPlayer, string code, string langKey)
        {
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, Lang.Get(langKey));
            else (Api as ICoreClientAPI)?.TriggerIngameError(this, code, Lang.Get(langKey));
        }

        private float Speed => (mpc?.Network != null) ? mpc.TrueSpeed : 0f;

        private void OnMillTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            float speed = Speed;
            if (speed <= 0f || !IsMillable(Api.World, InputSlot.Itemstack)) return;

            processProgress += dt * speed;
            if (processProgress < ProcessSecondsRequired) return;
            processProgress = 0;

            ProcessOne();
            MarkDirty();
        }

        private void ProcessOne()
        {
            ItemStack input = InputSlot.Itemstack;
            int quality = FiberQuality.Get(input);

            // The scutch mill combines breaking and scutching — outputs scutched bundles ready
            // for the hatchel. The player still hatchels by hand to extract the final fibre.
            AssetLocation scutchedCode = ScutchedCodeOf(input);
            Item scutchedItem = Api.World.GetItem(scutchedCode);
            if (scutchedItem == null) return;

            // Mill scutching wasted more fibre than hand work — the beating arms shred a share of
            // the long line into short tow, and the millers took their pay in exactly that. Quality
            // is untouched; this is a throughput-for-yield trade, not a throughput-for-quality one.
            if (Api.World.Rand.NextDouble() < RudimentsModSystem.Config.MechScutcherTowShare)
            {
                InputSlot.TakeOut(1);
                InputSlot.MarkDirty();
                EjectTow();
                return;
            }

            InputSlot.TakeOut(1);
            InputSlot.MarkDirty();

            ItemStack outStack = new ItemStack(scutchedItem, 1);
            FiberQuality.Carry(input, outStack);

            if (OutputSlot.Empty)
            {
                OutputSlot.Itemstack = outStack;
            }
            else if (OutputSlot.Itemstack.Collectible.Code.Equals(scutchedCode)
                     && FiberQuality.Get(OutputSlot.Itemstack) == quality
                     && OutputSlot.Itemstack.StackSize < OutputSlot.Itemstack.Collectible.MaxStackSize)
            {
                OutputSlot.Itemstack.StackSize += 1;
            }
            else
            {
                Api.World.SpawnItemEntity(outStack, Pos.ToVec3d().Add(0.5, 0.9, 0.5));
            }
            OutputSlot.MarkDirty();
        }

        /// <summary>
        /// Drops the wasted bundle's worth of tow at the mill as an item entity rather than into a
        /// third slot — a slot-count change would break InventoryGeneric.FromTreeAttributes on
        /// existing saves, and tow spilling out of the machine is exactly what happened.
        /// </summary>
        private void EjectTow()
        {
            Item tow = Api.World.GetItem(new AssetLocation("rudiments", "coarsefibers"));
            int perBundle = System.Math.Max(0, RudimentsModSystem.Config.ScutchTowFibersPerBundle);
            if (tow == null || perBundle <= 0) return;

            ItemStack towStack = new ItemStack(tow, perBundle);
            FiberQuality.Set(towStack, FiberQuality.Coarse);
            Api.World.SpawnItemEntity(towStack, Pos.ToVec3d().Add(0.5, 0.9, 0.5));
        }

        // BEBehaviorMPBase.OnTesselation returns true unconditionally, claiming the render
        // slot but adding no mesh. We must call base first (for lighting), then add our own.
        private MeshData ownMesh;

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            base.OnTesselation(mesher, tesselator);

            // Tesselating the block (not the bare shape file) picks up the side variant's rotateY.
            if (ownMesh == null) tesselator.TesselateBlock(Block, out ownMesh);

            if (ownMesh != null)
                mesher.AddMeshData(ownMesh);

            return true;
        }

        // Deliberately skips BlockEntityContainer's food-perish lines, which mean nothing here.
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Speed <= 0f) dsc.AppendLine(Lang.Get("rudiments:scutcher-nopower"));
            else dsc.AppendLine(Lang.Get("rudiments:scutcher-running"));

            if (!InputSlot.Empty)
            {
                dsc.AppendLine(Lang.Get("rudiments:scutcher-input", InputSlot.Itemstack.StackSize, InputSlot.Itemstack.GetName()));
                dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(FiberQuality.Get(InputSlot.Itemstack))));
            }
            if (!OutputSlot.Empty)
                dsc.AppendLine(Lang.Get("rudiments:scutcher-output", OutputSlot.Itemstack.StackSize, OutputSlot.Itemstack.GetName()));

            float towShare = RudimentsModSystem.Config.MechScutcherTowShare;
            if (towShare > 0f)
                dsc.AppendLine(Lang.Get("rudiments:scutcher-towwaste", (int)System.Math.Round(towShare * 100f)));
        }

        // The inventory itself rides on BlockEntityContainer's "inventory" tree, the same key this
        // block entity wrote before it became a container, so mills in existing saves keep their load.
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            processProgress = tree.GetFloat("processProgress");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("processProgress", processProgress);
        }
    }
}
