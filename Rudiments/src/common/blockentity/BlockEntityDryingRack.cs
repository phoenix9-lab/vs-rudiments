using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Drying rack for retted fiber bundles. Drying is handled fully in code (not via the transitionable
    /// system) so it can be weather-aware and preserve fiber quality. Warm, dry, sheltered conditions dry
    /// fastest; rain on an exposed rack effectively pauses drying. Quality carried on the retted bundle is
    /// preserved into the dried bundle ("dry it on a rack to keep fine fiber fine").
    /// </summary>
    public class BlockEntityDryingRack : BlockEntityDisplayCase
    {
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "fiberdryingrack";
        private new InventoryGeneric inventory;

        public ItemSlot flaxSlot => inventory[0];

        // In-game hours of drying required under ideal conditions.
        private const double DryHoursRequired = 18.0;
        private double dryProgressHours;
        private double lastTotalHours = -1;

        static SimpleParticleProperties waterParticleStart;
        static SimpleParticleProperties waterParticleFinish;

        private AssetLocation interactSound;

        static Vec2d[] particlePositions = new Vec2d[8]
        {
            new Vec2d(0.2f,0.9375f),
            new Vec2d(0.2f,0.0625f),
            new Vec2d(0.4f,0.9375f),
            new Vec2d(0.4f,0.0625f),
            new Vec2d(0.6f,0.9375f),
            new Vec2d(0.6f,0.0625f),
            new Vec2d(0.8f,0.9375f),
            new Vec2d(0.8f,0.0625f),
        };
        static Vec2d particleOffset = new Vec2d(0.0625f, 0.0625f);

        static Vec3f flaxRotationPivot = new Vec3f(0.5f, 0.03125f, 0.5f);


        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null)
            {
                inventory = new InventoryGeneric(1, InventoryClassName + "-" + Pos, api);
            }

            base.Initialize(api);

            inventory.LateInitialize(InventoryClassName + "-" + Pos, api);
            container.Reset();

            waterParticleStart = new SimpleParticleProperties(1, 1, ColorUtil.ToRgba(175, 113, 184, 232), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f(), 0.6f, 0.01f, 0.01f, 0.1f, EnumParticleModel.Cube);
            waterParticleStart.WithTerrainCollision = false;
            waterParticleStart.WindAffected = true;
            waterParticleStart.AddPos.Set(0.125f, 0.0625f, 0.125f);
            waterParticleStart.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.1f);

            waterParticleFinish = new SimpleParticleProperties(1, 1, ColorUtil.ToRgba(125, 113, 184, 232), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f(), 2f, 1f, 0.2f, 0.2f, EnumParticleModel.Cube);
            waterParticleFinish.WithTerrainCollision = false;
            waterParticleFinish.ShouldDieInLiquid = true;
            waterParticleFinish.WindAffected = true;

            waterParticleStart.DeathParticles = new IParticlePropertiesProvider[] { waterParticleFinish };

            interactSound = new AssetLocation("game", "sounds/block/leafy-picking");

            RegisterGameTickListener(OnGameTick, 250);
        }

        private bool IsRetted(ItemStack stack)
        {
            return stack?.Collectible?.Code != null && stack.Collectible.Code.Path.EndsWith("-retted");
        }

        public bool OnInteract(IPlayer player)
        {
            var heldSlot = player.InventoryManager.ActiveHotbarSlot;
            var heldItem = heldSlot?.Itemstack;
            var slot = flaxSlot;

            // Empty hand: collect whatever is on the rack
            if (heldItem == null && !slot.Empty && player.InventoryManager.TryGiveItemstack(slot.Itemstack.Clone()))
            {
                slot.Itemstack = null;
                slot.MarkDirty();
                dryProgressHours = 0;
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 1f, 1f);
                return true;
            }

            if (!IsRetted(heldItem)) return false;

            if (slot.Empty)
            {
                slot.Itemstack = heldItem.Clone();
                slot.Itemstack.StackSize = 1;
                heldSlot.TakeOut(1);
                heldSlot.MarkDirty();
                slot.MarkDirty();
                dryProgressHours = 0;
                lastTotalHours = Api.World.Calendar.TotalHours;
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 1f, 1f);
                return true;
            }

            // Only stack identical retted bundles (same code AND same quality), so we don't average quality
            if (slot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code)
                && FiberQuality.Get(slot.Itemstack) == FiberQuality.Get(heldItem))
            {
                int transferable = Math.Min(heldItem.StackSize, slot.Itemstack.Collectible.MaxStackSize - slot.Itemstack.StackSize);
                if (transferable <= 0) return true;

                slot.Itemstack.StackSize += transferable;
                heldSlot.TakeOut(transferable);
                heldSlot.MarkDirty();
                slot.MarkDirty();
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 1f, 1f);
                return true;
            }

            return false;
        }

        private double GetDryFactor()
        {
            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            double factor = FieldWeather.DryFactor(climate);
            // Exposed rain heavily penalises rack drying (sheltered rack is safer but not immune).
            if (FieldWeather.IsExposedRaining(Api.World, Pos, 0.04))
                factor *= 0.15;
            return factor;
        }

        private void OnGameTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server)
            {
                // Client-side: gentle moisture particles while drying
                if (!flaxSlot.Empty && IsRetted(flaxSlot.Itemstack) && Api.World.Rand.Next(0, 4) == 1)
                {
                    int amount = (int)Math.Ceiling(flaxSlot.Itemstack.StackSize / 8f);
                    if (amount > 0) SpawnParticle(Api.World.Rand.Next(0, amount));
                }
                return;
            }

            if (flaxSlot.Empty || !IsRetted(flaxSlot.Itemstack))
            {
                lastTotalHours = Api.World.Calendar.TotalHours;
                return;
            }

            double now = Api.World.Calendar.TotalHours;
            if (lastTotalHours < 0) { lastTotalHours = now; return; }
            double elapsed = now - lastTotalHours;
            lastTotalHours = now;
            if (elapsed <= 0) return;

            dryProgressHours += elapsed * GetDryFactor();

            if (dryProgressHours >= DryHoursRequired)
            {
                ConvertToDried();
            }

            MarkDirty();
        }

        private void ConvertToDried()
        {
            ItemStack retted = flaxSlot.Itemstack;
            if (retted == null) return;

            AssetLocation driedCode = retted.Collectible.Code.CopyWithPath(retted.Collectible.Code.Path.Replace("-retted", "-dried"));
            Item driedItem = Api.World.GetItem(driedCode);
            if (driedItem == null) return;

            ItemStack driedStack = new ItemStack(driedItem, retted.StackSize);
            FiberQuality.Carry(retted, driedStack); // rack drying preserves quality

            flaxSlot.Itemstack = driedStack;
            flaxSlot.MarkDirty();
            dryProgressHours = 0;
            MarkDirty(true);
        }

        private void SpawnParticle(int value)
        {
            Vec3d pos = new Vec3d(Pos.X, Pos.Y, Pos.Z) + RotatePositionsY(new Vec3d(particlePositions[value].X, 0.4f, particlePositions[value].Y), Block.Shape.rotateY);
            waterParticleStart.MinPos.Set(pos.X - particleOffset.X, pos.Y, pos.Z - particleOffset.Y);
            Api.World.SpawnParticles(waterParticleStart);
        }

        public static Vec3d RotatePositionsY(Vec3d position, float angleDeg)
        {
            if (angleDeg == 0) return position;

            float radians = GameMath.DEG2RAD * angleDeg;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            float x = (float)position.X - 0.5f;
            float z = (float)position.Z - 0.5f;

            float xRot = x * cos + z * sin;
            float zRot = -x * sin + z * cos;

            return new Vec3d(xRot + 0.5f, position.Y, zRot + 0.5f);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
            {
                inventory = new InventoryGeneric(1, InventoryClassName + "-" + Pos, Api);
            }

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null)
            {
                inventory.FromTreeAttributes(invTree);
            }
            dryProgressHours = tree.GetDouble("dryProgressHours");
            lastTotalHours = tree.GetDouble("lastTotalHours", -1);
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
            tree.SetDouble("dryProgressHours", dryProgressHours);
            tree.SetDouble("lastTotalHours", lastTotalHours);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!flaxSlot.Empty && IsRetted(flaxSlot.Itemstack))
            {
                int progress = (int)GameMath.Clamp(dryProgressHours / DryHoursRequired * 100, 0, 100);
                dsc.AppendLine(Lang.Get("rudiments:dryingrack-progress", progress));
                dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(FiberQuality.Get(flaxSlot.Itemstack))));
                dsc.AppendLine();
            }
        }

        private MeshData GenItemMesh(ItemStack stack, int count)
        {
            this.nowTesselatingObj = stack.Item;
            MeshData mesh = null;

            if (stack?.Item?.Shape != null)
            {
                try
                {
                    this.capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
                }
                catch { return mesh; }
                if (mesh != null)
                {
                    for (int p = 0; p < mesh.RenderPassesAndExtraBits.Length; p++)
                    {
                        mesh.RenderPassesAndExtraBits[p] = (short)EnumChunkRenderPass.BlendNoCull;
                    }
                }
                if (count == 0)
                {
                    mesh.Rotate(flaxRotationPivot, -110 * GameMath.DEG2RAD, 0, 90 * GameMath.DEG2RAD);
                    mesh.Translate(-0.3f, 0.75f, 0.35f);
                }
                else if (count == 1)
                {
                    mesh.Rotate(flaxRotationPivot, -70 * GameMath.DEG2RAD, 0, 180 * GameMath.DEG2RAD);
                    mesh.Translate(-0.3f, 0.75f, -0.35f);
                }
                else if (count == 2)
                {
                    mesh.Rotate(flaxRotationPivot, -110 * GameMath.DEG2RAD, 0, 0);
                    mesh.Translate(-0.1f, 0.75f, 0.35f);
                }
                else if (count == 3)
                {
                    mesh.Rotate(flaxRotationPivot, -70 * GameMath.DEG2RAD, 0, 270 * GameMath.DEG2RAD);
                    mesh.Translate(-0.1f, 0.75f, -0.35f);
                }
                else if (count == 4)
                {
                    mesh.Rotate(flaxRotationPivot, -110 * GameMath.DEG2RAD, 0, 180 * GameMath.DEG2RAD);
                    mesh.Translate(0.1f, 0.75f, 0.35f);
                }
                else if (count == 5)
                {
                    mesh.Rotate(flaxRotationPivot, -70 * GameMath.DEG2RAD, 0, 0);
                    mesh.Translate(0.1f, 0.75f, -0.35f);
                }
                else if (count == 6)
                {
                    mesh.Rotate(flaxRotationPivot, -110 * GameMath.DEG2RAD, 0, 270 * GameMath.DEG2RAD);
                    mesh.Translate(0.3f, 0.75f, 0.35f);
                }
                else if (count == 7)
                {
                    mesh.Rotate(flaxRotationPivot, -70 * GameMath.DEG2RAD, 0, 90 * GameMath.DEG2RAD);
                    mesh.Translate(0.3f, 0.75f, -0.35f);
                }
                mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.95f, 0.95f, 0.95f);
                var rotate = this.Block.Shape.rotateY;
                mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, rotate * GameMath.DEG2RAD, 0);
            }
            return mesh;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            var shapeBase = "rudiments:shapes/block/tool/dryingrack/dryingrack";
            if (Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Default) is BlockDryingRack block)
            {
                var texture = tesselator.GetTextureSource(block);
                mesh = block.GenMesh(Api as ICoreClientAPI, shapeBase, texture);
                mesher.AddMeshData(mesh);

                if (!flaxSlot.Empty)
                {
                    int amount = (int)Math.Ceiling(flaxSlot.Itemstack.StackSize / 8f);

                    for (int i = 0; i < amount; i++)
                    {
                        var tmpStack = flaxSlot.Itemstack;
                        mesh = this.GenItemMesh(tmpStack, i);
                        if (mesh != null)
                        { mesher.AddMeshData(mesh); }
                    }
                }
            }
            return true;
        }
    }
}
