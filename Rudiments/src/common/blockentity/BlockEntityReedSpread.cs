using Rudiments.SRC.Common.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Calendar-driven rhizome spread for normal (non-harvested) reedpapyrus variants
    /// (coopersreed, papyrus, tule, brownsedge). Attempts a spread every
    /// RudimentsConfig.ReedSpreadIntervalDays in-game days via the block's BlockBehaviorRhizomeSpread.
    ///
    /// Replaces the old BlockReedsWithSpread approach, which hung off real-time random block ticks
    /// that reeds don't actually receive — so it responds to time speed and is deterministic.
    /// Harvested variants keep their vanilla "Transient" regrow block entity.
    /// </summary>
    public class BlockEntityReedSpread : BlockEntity
    {
        private double lastSpreadHours = -1;
        private long listenerId;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            if (lastSpreadHours < 0) lastSpreadHours = api.World.Calendar.TotalHours;
            listenerId = RegisterGameTickListener(OnGameTick, 5000);
        }

        private void OnGameTick(float dt)
        {
            var cfg = RudimentsModSystem.Config;
            if (!cfg.ReedSpreadEnabled) return;

            double now = Api.World.Calendar.TotalHours;
            if (now - lastSpreadHours < cfg.ReedSpreadIntervalDays * Api.World.Calendar.HoursPerDay) return;
            lastSpreadHours = now;

            Block self = Api.World.BlockAccessor.GetBlock(Pos);
            self.GetBehavior<BlockBehaviorRhizomeSpread>()?.TrySpreadTick(Api.World, Pos);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (listenerId != 0) UnregisterGameTickListener(listenerId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            lastSpreadHours = tree.GetDouble("lastSpreadHours", -1);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastSpreadHours", lastSpreadHours);
        }
    }
}
