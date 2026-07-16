using Rudiments;
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
    /// A stook placed on the ground — a cone of standing sheaves. Two modes:
    ///   Cure mode  — input is *-unprocessed; arid weather cures it into *-cured.
    ///                Exposed rain stalls curing (no progress, no damage).
    ///   Dry mode   — input is *-retted; arid weather dries it into *-dried, preserving
    ///                quality. Exposed rain resets dryProgress and accumulates
    ///                rainExposureHours; every rainTierHours of rain drops quality one
    ///                tier; below Coarse the bundle rots.
    /// </summary>
    public class BlockEntityStook : BlockEntity
    {
        private InventoryGeneric inventory;
        public ItemSlot FiberSlot => inventory[0];

        // Persisted state
        private double cureProgress;    // game-hours of curing accrued (cure mode)
        private double dryProgress;     // game-hours of drying accrued  (dry mode)
        private double rainExposureHours; // cumulative rain hours in dry mode
        private double lastTotalHours = -1;

        // Tunables (loaded from JSON attributes)
        private double CureHours      = 24.0;
        private double DryHours       = 18.0;
        private double RainTierHours  = 12.0;
        private double DryStallRainfall = 0.05;

        private AssetLocation interactSound;

        // ------------------------------------------------------------------ //
        //  Mode detection                                                      //
        // ------------------------------------------------------------------ //

        private enum StookMode { None, Cure, Dry }

        private StookMode GetMode(ItemStack stack)
        {
            if (stack?.Collectible?.Code == null) return StookMode.None;
            string path = stack.Collectible.Code.Path;
            if (path.EndsWith("-unprocessed")) return StookMode.Cure;
            if (path.EndsWith("-retted"))      return StookMode.Dry;
            return StookMode.None;
        }

        private bool IsAccepted(ItemStack stack) => GetMode(stack) != StookMode.None;

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null)
                inventory = new InventoryGeneric(1, "stook-" + Pos, api);

            base.Initialize(api);
            inventory.LateInitialize("stook-" + Pos, api);

            interactSound = new AssetLocation("game", "sounds/block/water");
            LoadTunables();

            if (api.Side == EnumAppSide.Server)
                RegisterGameTickListener(OnGameTick, 1000);
        }

        private void LoadTunables()
        {
            var attrs = Block?.Attributes;
            if (attrs == null) return;
            CureHours       = attrs["cureHours"].AsDouble(CureHours);
            DryHours        = attrs["dryHours"].AsDouble(DryHours);
            RainTierHours   = attrs["rainTierHours"].AsDouble(RainTierHours);
            DryStallRainfall = attrs["dryStallRainfall"].AsDouble(DryStallRainfall);
        }

        // ------------------------------------------------------------------ //
        //  Interaction                                                         //
        // ------------------------------------------------------------------ //

        public bool OnInteract(IPlayer player)
        {
            var heldSlot = player.InventoryManager.ActiveHotbarSlot;
            var heldItem = heldSlot?.Itemstack;
            var slot     = FiberSlot;

            // Empty hand → retrieve bundles and remove the block
            if (heldItem == null && !slot.Empty)
            {
                if (player.InventoryManager.TryGiveItemstack(slot.Itemstack.Clone()))
                {
                    Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                    Api.World.BlockAccessor.SetBlock(0, Pos);
                    return true;
                }
                return false;
            }

            if (!IsAccepted(heldItem)) return false;

            int cap = RudimentsModSystem.Config?.StookMaxBundles ?? 64;

            // Insert into empty slot
            if (slot.Empty)
            {
                int take = Math.Min(heldItem.StackSize, cap);
                slot.Itemstack = heldItem.Clone();
                slot.Itemstack.StackSize = take;
                heldSlot.TakeOut(take);
                heldSlot.MarkDirty();
                slot.MarkDirty();
                ResetProgress();
                lastTotalHours = Api.World.Calendar.TotalHours;
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            // Stack-merge only identical bundles (same code, quality and harvest potential),
            // so distinct grades never average together.
            if (slot.Itemstack.Collectible.Code.Equals(heldItem.Collectible.Code)
                && FiberQuality.Get(slot.Itemstack) == FiberQuality.Get(heldItem)
                && FiberQuality.GetPotential(slot.Itemstack) == FiberQuality.GetPotential(heldItem))
            {
                int xfer = Math.Min(heldItem.StackSize, cap - slot.Itemstack.StackSize);
                if (xfer <= 0) return true;
                slot.Itemstack.StackSize += xfer;
                heldSlot.TakeOut(xfer);
                heldSlot.MarkDirty();
                slot.MarkDirty();
                MarkDirty(true);
                Api.World.PlaySoundAt(interactSound, player, null, false, 8f, 0.6f);
                return true;
            }

            return false;
        }

        private void ResetProgress()
        {
            cureProgress = 0;
            dryProgress  = 0;
            rainExposureHours = 0;
            lastTotalHours = -1;
        }

        // ------------------------------------------------------------------ //
        //  Tick                                                                //
        // ------------------------------------------------------------------ //

        private void OnGameTick(float dt)
        {
            if (FiberSlot.Empty) { lastTotalHours = Api.World.Calendar.TotalHours; return; }

            StookMode mode = GetMode(FiberSlot.Itemstack);
            if (mode == StookMode.None) { lastTotalHours = Api.World.Calendar.TotalHours; return; }

            double now = Api.World.Calendar.TotalHours;
            if (lastTotalHours < 0) { lastTotalHours = now; return; }
            double elapsed = now - lastTotalHours;
            lastTotalHours = now;
            if (elapsed <= 0) return;

            var climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            bool exposedRain = FieldWeather.IsExposedRaining(Api.World, Pos, DryStallRainfall);

            if (mode == StookMode.Cure)
            {
                TickCure(elapsed, climate, exposedRain);
            }
            else
            {
                TickDry(elapsed, climate, exposedRain);
            }

            MarkDirty();
        }

        private void TickCure(double elapsed, ClimateCondition climate, bool exposedRain)
        {
            // Rain stalls curing; arid air advances it.
            if (exposedRain) return;

            double factor = FieldWeather.DryFactor(climate);
            cureProgress += elapsed * factor;

            if (cureProgress >= CureHours)
                ConvertToCured();
        }

        private void TickDry(double elapsed, ClimateCondition climate, bool exposedRain)
        {
            if (exposedRain)
            {
                // Rain resets drying progress and accumulates rain exposure.
                dryProgress = 0;
                rainExposureHours += elapsed;

                // Drop quality tier every RainTierHours of rain exposure.
                int tierDrops = (int)(rainExposureHours / RainTierHours);
                if (tierDrops > 0)
                {
                    rainExposureHours -= tierDrops * RainTierHours;
                    ItemStack stack = FiberSlot.Itemstack;
                    int quality = FiberQuality.Get(stack);
                    quality -= tierDrops;
                    if (quality < FiberQuality.Coarse)
                    {
                        ConvertToRot();
                        return;
                    }
                    FiberQuality.Set(stack, quality);
                    FiberSlot.MarkDirty();
                }
                return;
            }

            // Arid — advance drying.
            double factor = FieldWeather.DryFactor(climate);
            dryProgress += elapsed * factor;

            if (dryProgress >= DryHours)
                ConvertToDried();
        }

        // ------------------------------------------------------------------ //
        //  Conversions                                                         //
        // ------------------------------------------------------------------ //

        private void ConvertToCured()
        {
            ItemStack input = FiberSlot.Itemstack;
            // *-unprocessed → *-cured
            AssetLocation curedCode = input.Collectible.Code.CopyWithPath(
                input.Collectible.Code.Path.Replace("-unprocessed", "-cured"));
            Item curedItem = Api.World.GetItem(curedCode);
            if (curedItem == null) return;

            ItemStack curedStack = new ItemStack(curedItem, input.StackSize);
            FiberQuality.CarryPotential(input, curedStack);
            FiberSlot.Itemstack = curedStack;
            FiberSlot.MarkDirty();
            ResetProgress();
            MarkDirty(true);
        }

        private void ConvertToDried()
        {
            ItemStack retted = FiberSlot.Itemstack;
            AssetLocation driedCode = retted.Collectible.Code.CopyWithPath(
                retted.Collectible.Code.Path.Replace("-retted", "-dried"));
            Item driedItem = Api.World.GetItem(driedCode);
            if (driedItem == null) return;

            ItemStack driedStack = new ItemStack(driedItem, retted.StackSize);
            FiberQuality.Carry(retted, driedStack);

            FiberSlot.Itemstack = driedStack;
            FiberSlot.MarkDirty();
            ResetProgress();
            MarkDirty(true);
        }

        private void ConvertToRot()
        {
            int stackSize = FiberSlot.Itemstack.StackSize;
            var rot = Api.World.GetItem(new AssetLocation("game", "rot"));
            if (rot != null)
                FiberSlot.Itemstack = new ItemStack(rot, stackSize);
            ResetProgress();
            MarkDirty(true);
        }

        // ------------------------------------------------------------------ //
        //  Block info                                                          //
        // ------------------------------------------------------------------ //

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (FiberSlot.Empty)
            {
                dsc.AppendLine(Lang.Get("rudiments:stook-empty"));
                return;
            }

            StookMode mode = GetMode(FiberSlot.Itemstack);
            int count = FiberSlot.Itemstack.StackSize;

            if (mode == StookMode.Cure)
            {
                int pct = (int)GameMath.Clamp(cureProgress / CureHours * 100, 0, 100);
                dsc.AppendLine(Lang.Get("rudiments:stook-curing-progress", count, pct));
            }
            else if (mode == StookMode.Dry)
            {
                int pct = (int)GameMath.Clamp(dryProgress / DryHours * 100, 0, 100);
                dsc.AppendLine(Lang.Get("rudiments:stook-drying-progress", count, pct));
                dsc.AppendLine(Lang.Get("rudiments:fiberquality-label", FiberQuality.Name(FiberQuality.Get(FiberSlot.Itemstack))));
                if (rainExposureHours > 0)
                    dsc.AppendLine(Lang.Get("rudiments:stook-rain-warning"));
            }
            else
            {
                dsc.AppendLine(Lang.Get("rudiments:stook-empty"));
            }
        }

        // ------------------------------------------------------------------ //
        //  Block broken                                                        //
        // ------------------------------------------------------------------ //

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server && !FiberSlot.Empty)
                Api.World.SpawnItemEntity(FiberSlot.Itemstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
            base.OnBlockBroken(byPlayer);
        }

        // ------------------------------------------------------------------ //
        //  Persistence                                                         //
        // ------------------------------------------------------------------ //

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null)
                inventory = new InventoryGeneric(1, "stook-" + Pos, Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            cureProgress      = tree.GetDouble("cureProgress");
            dryProgress       = tree.GetDouble("dryProgress");
            rainExposureHours = tree.GetDouble("rainExposureHours");
            lastTotalHours    = tree.GetDouble("lastTotalHours", -1);

            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetDouble("cureProgress",      cureProgress);
            tree.SetDouble("dryProgress",        dryProgress);
            tree.SetDouble("rainExposureHours",  rainExposureHours);
            tree.SetDouble("lastTotalHours",     lastTotalHours);
        }
    }
}
