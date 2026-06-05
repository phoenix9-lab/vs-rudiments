using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Calendar-driven nettle lifecycle:
    ///   - WILD nettle (on non-farmland soil): advances growth stages over in-game time and drains
    ///     nitrogen from neighbouring farmland each time it grows (heavy feeder).
    ///   - rhizome spread once mature — for BOTH wild and cultivated nettle (it's invasive everywhere).
    ///
    /// Cultivated nettle growth is driven by BlockEntityFarmland and its neighbour depletion by
    /// CropBehaviorHeavyFeeder, so this BE only handles spread there (not growth/depletion).
    ///
    /// Driving this off Calendar.TotalHours (instead of real-time random block ticks) makes the
    /// whole lifecycle deterministic and responsive to time speed.
    /// </summary>
    public class BlockEntityNettle : BlockEntity
    {
        private double lastGrowthHours = -1;
        private double lastSpreadHours = -1;
        private long listenerId;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            double now = api.World.Calendar.TotalHours;
            if (lastGrowthHours < 0) lastGrowthHours = now;
            if (lastSpreadHours < 0) lastSpreadHours = now;

            listenerId = RegisterGameTickListener(OnGameTick, 3000);
        }

        private static int StageOf(Block block)
            => int.TryParse(block?.Variant?["stage"], out int s) ? s : 0;

        private void OnGameTick(float dt)
        {
            var cfg = RudimentsModSystem.Config;
            double now = Api.World.Calendar.TotalHours;
            double hpd = Api.World.Calendar.HoursPerDay;

            Block self = Api.World.BlockAccessor.GetBlock(Pos);
            Block below = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());
            bool onFarmland = below is BlockFarmland;

            // ── Growth (wild only; cultivated growth is driven by BlockEntityFarmland) ──
            int stage = StageOf(self);
            if (!onFarmland && stage > 0 && stage < 9 && below.Fertility > 0 &&
                now - lastGrowthHours >= cfg.NettleWildGrowthDaysPerStage * hpd)
            {
                Block next = Api.World.GetBlock(self.CodeWithVariant("stage", (stage + 1).ToString()));
                if (next != null)
                {
                    // ExchangeBlock keeps this block entity (and its timers) across the stage change.
                    Api.World.BlockAccessor.ExchangeBlock(next.BlockId, Pos);
                    self = next;
                    stage++;

                    if (cfg.NettleHeavyFeederEnabled)
                        NettleFeeder.DepleteNeighborNitrogen(Api.World, Pos.DownCopy(), cfg.NettleNeighborNitrogenDepletion);
                }
                // Advance by one interval (not to "now") so a long unload catches up one stage per tick.
                lastGrowthHours += cfg.NettleWildGrowthDaysPerStage * hpd;
            }

            // ── Spread (wild AND cultivated — invasive; optional farmland containment) ────
            bool contained = onFarmland && cfg.NettleFarmlandContainment;
            if (!contained && stage >= cfg.NettleSpreadMatureStage &&
                now - lastSpreadHours >= cfg.NettleSpreadIntervalDays * hpd)
            {
                self.GetBehavior<BlockBehaviorRhizomeSpread>()?.TrySpreadTick(Api.World, Pos);
                lastSpreadHours = now;
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (listenerId != 0) UnregisterGameTickListener(listenerId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            lastGrowthHours = tree.GetDouble("lastGrowthHours", -1);
            lastSpreadHours = tree.GetDouble("lastSpreadHours", -1);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastGrowthHours", lastGrowthHours);
            tree.SetDouble("lastSpreadHours", lastSpreadHours);
        }
    }
}
