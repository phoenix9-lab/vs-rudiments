using Rudiments.SRC.Common.Blocks;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Calendar-driven lifecycle for WILD nettle (crop-nettle on non-farmland soil):
    ///   - advances growth stages over in-game time,
    ///   - drains nitrogen from neighbouring farmland each time it grows (heavy feeder),
    ///   - attempts rhizome spread once mature.
    ///
    /// Cultivated nettle (on farmland) is left entirely to BlockEntityFarmland (growth) and
    /// CropBehaviorHeavyFeeder (neighbour depletion); this BE no-ops there.
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

        private bool OnFarmland()
            => Api.World.BlockAccessor.GetBlock(Pos.DownCopy()) is BlockFarmland;

        private static int StageOf(Block block)
            => int.TryParse(block?.Variant?["stage"], out int s) ? s : 0;

        private void OnGameTick(float dt)
        {
            // Cultivated nettle is handled by the farmland system; nothing to do here.
            if (OnFarmland()) return;

            var cfg = RudimentsModSystem.Config;
            double now = Api.World.Calendar.TotalHours;
            double hpd = Api.World.Calendar.HoursPerDay;

            Block self = Api.World.BlockAccessor.GetBlock(Pos);
            Block below = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());

            // ── Growth ────────────────────────────────────────────────────────────────
            int stage = StageOf(self);
            if (stage > 0 && stage < 9 && below.Fertility > 0 &&
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

            // ── Spread ──────────────────────────────────────────────────────────────────
            if (stage >= cfg.NettleSpreadMatureStage &&
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
