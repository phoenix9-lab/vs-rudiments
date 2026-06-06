using Rudiments.Utils;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Block-entity behavior attached to the vanilla barrel (via a JSON patch). Turns the barrel
    /// into a water-retting "bath": when the player seals a barrel holding cured nettle / rippled
    /// flax submerged in water (or limewater), it immediately unseals and starts an open retting
    /// process running the shared 4-stage quality/rot timeline (<see cref="RettingProcess"/>).
    ///
    /// The seal merely starts the timers — the barrel then sits open as a murky "retting bath"
    /// (the water is converted to rudiments:rettingbathportion so no barrel recipe re-matches) and
    /// the bundles progress through coarse → fine → standard → rot, just like juice turning to rot
    /// in an open barrel. Limewater gives the faster, Standard-capped lime path.
    /// </summary>
    public class BlockEntityBehaviorRettingBath : BlockEntityBehavior
    {
        private readonly RettingProcess process = new RettingProcess();
        private bool active;
        private ICoreAPI api;

        private const string WaterCode = "game:waterportion";
        private const string LimeCode  = "game:slakedlimeportion";
        private const string BathCode  = "rudiments:rettingbathportion";
        private const string LangPrefix = "rudiments:rettingbath";

        public BlockEntityBehaviorRettingBath(BlockEntity blockentity) : base(blockentity) { }

        private BlockEntityBarrel Barrel => Blockentity as BlockEntityBarrel;
        private ItemSlot ItemSlot => Barrel?.Inventory?[0];
        private ItemSlot LiquidSlot => Barrel?.Inventory?[1];

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            this.api = api;

            // Defaults already match the old vat (steady water retting); allow JSON overrides.
            if (properties != null)
            {
                process.MinRetHours   = properties["minRetHours"].AsDouble(process.MinRetHours);
                process.FineChance    = properties["fineChance"].AsDouble(process.FineChance);
                process.FineDelayMin  = properties["fineDelayMinHours"].AsDouble(process.FineDelayMin);
                process.FineDelayMax  = properties["fineDelayMaxHours"].AsDouble(process.FineDelayMax);
                process.FineWindow    = properties["fineWindowHours"].AsDouble(process.FineWindow);
                process.StandardDelay = properties["standardDelayHours"].AsDouble(process.StandardDelay);
                process.StandardHold  = properties["standardHoldHours"].AsDouble(process.StandardHold);
            }

            if (api.Side == EnumAppSide.Server)
            {
                Blockentity.RegisterGameTickListener(OnServerTick, 1000);
            }
        }

        private void OnServerTick(float dt)
        {
            var barrel = Barrel;
            if (barrel == null) return;
            var itemSlot = ItemSlot;
            if (itemSlot == null) return;

            if (!active)
            {
                // Player sealed a barrel of bundles + water/limewater → start an open retting bath.
                if (barrel.Sealed && IsRettingCharge(out bool lime))
                {
                    StartRetting(barrel, lime);
                }
                return;
            }

            // Retting in progress. The seal only starts the timers — keep the barrel open.
            if (barrel.Sealed) barrel.Sealed = false;

            // Bundle removed / emptied → end the batch.
            if (itemSlot.Empty)
            {
                StopRetting();
                return;
            }

            if (process.Advance(api.World, itemSlot, 1.0))
            {
                Blockentity.MarkDirty();
            }
        }

        /// <summary>True if slot 0 holds a rettable bundle and slot 1 holds water (lime=false) or limewater (lime=true).</summary>
        private bool IsRettingCharge(out bool lime)
        {
            lime = false;
            var itemSlot = ItemSlot;
            var liqSlot = LiquidSlot;
            if (itemSlot == null || liqSlot == null) return false;
            if (itemSlot.Empty || liqSlot.Empty) return false;
            if (RettingProcess.GetRettedOutput(itemSlot.Itemstack) == null) return false;

            string liq = liqSlot.Itemstack?.Collectible?.Code?.ToString();
            if (liq == WaterCode) { lime = false; return true; }
            if (liq == LimeCode)  { lime = true;  return true; }
            return false;
        }

        private void StartRetting(BlockEntityBarrel barrel, bool lime)
        {
            api.World.Logger.Notification("[Rudiments] RettingBath at {0}: retting started (lime={1})", Blockentity.Pos, lime);
            barrel.Sealed = false;
            TransformLiquidToBath();

            process.Reset(api.World.Calendar.TotalHours, true);
            process.LimeActive = lime;
            active = true;

            Blockentity.MarkDirty(true);
        }

        private void StopRetting()
        {
            active = false;
            process.Reset(api.World.Calendar.TotalHours, false);
            Blockentity.MarkDirty(true);
        }

        /// <summary>
        /// Replace the clean water / limewater with a murky "retting bath" liquid of the same volume.
        /// This both reads as a bath and stops any barrel recipe from re-matching (so the Seal button
        /// does not reappear while retting). itemsPerLitre matches water (100), so stacksize maps 1:1.
        /// </summary>
        private void TransformLiquidToBath()
        {
            var liqSlot = LiquidSlot;
            if (liqSlot == null || liqSlot.Empty) return;
            Item bath = api.World.GetItem(new AssetLocation(BathCode));
            if (bath == null) return;
            int amount = liqSlot.Itemstack.StackSize;
            liqSlot.Itemstack = new ItemStack(bath, amount);
            liqSlot.MarkDirty();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (!active) return;
            var itemSlot = ItemSlot;
            if (itemSlot == null || itemSlot.Empty) return;

            if (process.LimeActive)
                dsc.AppendLine(Lang.Get("rudiments:rettingvat-lime-active"));

            process.AppendStatus(dsc, LangPrefix, itemSlot, api.World.Calendar.HoursPerDay);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("rb_active", active);
            var sub = new TreeAttribute();
            process.ToTree(sub);
            tree["rb_process"] = sub;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            active = tree.GetBool("rb_active");
            var sub = tree.GetTreeAttribute("rb_process");
            if (sub != null) process.FromTree(sub);
        }
    }
}
