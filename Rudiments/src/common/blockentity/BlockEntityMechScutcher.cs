using Rudiments.Utils;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Block entity for the mechanical scutcher. Holds an input slot (dried bundles) and an output slot
    /// (fibre). While the mechanical-power network is turning it, it consumes one dried bundle every
    /// few seconds and ejects the resulting fibre, scaled by the bundle's carried fibre quality.
    /// </summary>
    public class BlockEntityMechScutcher : BlockEntity
    {
        protected InventoryGeneric inventory;
        protected BEBehaviorMPConsumer mpc;

        public ItemSlot InputSlot => inventory[0];
        public ItemSlot OutputSlot => inventory[1];

        // Time in seconds per bundle at network speed 1.0
        private const float ProcessSecondsRequired = 5f;
        private float processProgress;

        private AssetLocation interactSound;

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null) inventory = new InventoryGeneric(2, "mechscutcher-" + Pos, api);
            base.Initialize(api);
            inventory.LateInitialize("mechscutcher-" + Pos, api);

            interactSound = new AssetLocation("game", "sounds/block/planks");
            RegisterGameTickListener(OnTick, 100);
        }

        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            mpc = GetBehavior<BEBehaviorMPConsumer>();
        }

        private static bool IsDried(ItemStack stack) =>
            stack?.Collectible?.Code != null && stack.Collectible.Code.Path.EndsWith("-dried");

        public bool OnInteract(IPlayer player)
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
                    return true;
                }
                if (!InputSlot.Empty && player.InventoryManager.TryGiveItemstack(InputSlot.Itemstack.Clone()))
                {
                    InputSlot.Itemstack = null;
                    processProgress = 0;
                    InputSlot.MarkDirty();
                    MarkDirty(true);
                    return true;
                }
                return false;
            }

            if (!IsDried(heldItem)) return false;

            if (InputSlot.Empty)
            {
                InputSlot.Itemstack = heldItem.Clone();
                heldSlot.TakeOutWhole();
            }
            else if (InputSlot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code)
                     && FiberQuality.Get(InputSlot.Itemstack) == FiberQuality.Get(heldItem))
            {
                int transferable = System.Math.Min(heldItem.StackSize, InputSlot.Itemstack.Collectible.MaxStackSize - InputSlot.Itemstack.StackSize);
                if (transferable <= 0) return true;
                InputSlot.Itemstack.StackSize += transferable;
                heldSlot.TakeOut(transferable);
            }
            else return false;

            heldSlot.MarkDirty();
            InputSlot.MarkDirty();
            MarkDirty(true);
            Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.7f);
            return true;
        }

        private float Speed => (mpc?.Network != null) ? mpc.TrueSpeed : 0f;

        private void OnTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            float speed = Speed;
            if (speed <= 0f || InputSlot.Empty || !IsDried(InputSlot.Itemstack)) return;

            processProgress += dt * speed;
            if (processProgress < ProcessSecondsRequired) return;
            processProgress = 0;

            ProcessOne();
            MarkDirty();
        }

        private void ProcessOne()
        {
            ItemStack dried = InputSlot.Itemstack;
            int quality = FiberQuality.Get(dried);

            // The scutch mill combines breaking and scutching — outputs scutched bundles ready
            // for the hatchel. The player still hatchels by hand to extract the final fibre.
            AssetLocation scutchedCode = dried.Collectible.Code.CopyWithPath(
                dried.Collectible.Code.Path.Replace("-dried", "-scutched"));

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
            FiberQuality.Carry(dried, outStack);

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

            if (ownMesh == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (capi == null) return true;

                string shapePath = "rudiments:shapes/block/tool/mechscutcher/mechscutcher";
                Shape shape = capi.Assets.TryGet(shapePath + ".json")?.ToObject<Shape>();
                if (shape == null) return true;

                ITexPositionSource tex = tesselator.GetTextureSource(Block);
                capi.Tesselator.TesselateShape(shapePath, shape, out ownMesh, tex, new Vec3f(0, 0, 0));
            }

            if (ownMesh != null)
                mesher.AddMeshData(ownMesh);

            return true;
        }

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

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null) inventory = new InventoryGeneric(2, "mechscutcher-" + Pos, Api);
            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);
            processProgress = tree.GetFloat("processProgress");
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
            tree.SetFloat("processProgress", processProgress);
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server && inventory != null)
            {
                foreach (var slot in inventory)
                {
                    if (!slot.Empty) Api.World.SpawnItemEntity(slot.Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
