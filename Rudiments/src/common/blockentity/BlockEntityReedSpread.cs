using Rudiments.SRC.Common.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Calendar-driven rhizome spread for normal (non-harvested) reedpapyrus variants
    /// (coopersreed, papyrus, tule, brownsedge). Attempts a spread every
    /// RudimentsConfig.ReedSpreadIntervalDays in-game days via the block's BlockBehaviorRhizomeSpread,
    /// bounded by the outward radius cap (ReedSpreadMaxRadius) just like nettle.
    ///
    /// Replaces the old BlockReedsWithSpread approach, which hung off real-time random block ticks
    /// that reeds don't actually receive — so it responds to time speed and is deterministic.
    /// Harvested variants keep their vanilla "Transient" regrow block entity.
    /// </summary>
    public class BlockEntityReedSpread : BlockEntity, IPatchOrigin
    {
        private double lastSpreadHours = -1;
        private int originX = int.MinValue;
        private int originZ = int.MinValue;
        private long listenerId;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            if (lastSpreadHours < 0) lastSpreadHours = api.World.Calendar.TotalHours;
            if (originX == int.MinValue) { originX = Pos.X; originZ = Pos.Z; }
            listenerId = RegisterGameTickListener(OnGameTick, 5000);
        }

        public void SetPatchOrigin(int x, int z)
        {
            originX = x;
            originZ = z;
            MarkDirty();
        }

        private void OnGameTick(float dt)
        {
            var cfg = RudimentsModSystem.Config;
            if (!cfg.ReedSpreadEnabled) return;

            double now = Api.World.Calendar.TotalHours;
            if (now - lastSpreadHours < cfg.ReedSpreadIntervalDays * Api.World.Calendar.HoursPerDay) return;
            lastSpreadHours = now;

            Block self = Api.World.BlockAccessor.GetBlock(Pos);
            var behavior = self.GetBehavior<BlockBehaviorRhizomeSpread>();
            if (behavior == null) return;

            BlockPos spreadAt = behavior.TrySpreadTick(Api.World, Pos, originX, originZ, cfg.ReedSpreadMaxRadius);
            if (spreadAt != null && Api.World.BlockAccessor.GetBlockEntity(spreadAt) is IPatchOrigin po)
                po.SetPatchOrigin(originX, originZ);
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
            originX = tree.GetInt("patchOriginX", int.MinValue);
            originZ = tree.GetInt("patchOriginZ", int.MinValue);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastSpreadHours", lastSpreadHours);
            tree.SetInt("patchOriginX", originX);
            tree.SetInt("patchOriginZ", originZ);
        }
    }
}
