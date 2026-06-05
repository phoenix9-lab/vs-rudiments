using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Calendar-driven timed block conversion. Counts down a configurable number of in-game days
    /// (read from RudimentsConfig via the "convertConfigKey" block attribute) and then replaces the
    /// block with the target given by the "convertTo" block attribute.
    ///
    /// Used for:
    ///   - the nettle stub regrowing into crop-nettle-1   (convertConfigKey "stub")
    ///   - the hidden buried rhizome surfacing as a nettle (convertConfigKey "creep")
    ///
    /// Because it uses Calendar.TotalHours it responds to time speed (testable at high speed) and is
    /// deterministic, unlike the real-time random block-tick system.
    /// </summary>
    public class BlockEntityNettleConvert : BlockEntity
    {
        private double convertAtTotalHours = -1;
        private long listenerId;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            if (convertAtTotalHours < 0)
            {
                double days = GetConfiguredDays();
                convertAtTotalHours = api.World.Calendar.TotalHours + days * api.World.Calendar.HoursPerDay;
            }

            listenerId = RegisterGameTickListener(OnGameTick, 2000);
        }

        private double GetConfiguredDays()
        {
            string key = Block?.Attributes?["convertConfigKey"].AsString("stub") ?? "stub";
            var cfg = RudimentsModSystem.Config;
            return key == "creep" ? cfg.NettleCreepEmergeDays : cfg.NettleStubRegrowDays;
        }

        private void OnGameTick(float dt)
        {
            if (Api.World.Calendar.TotalHours < convertAtTotalHours) return;

            string toCode = Block?.Attributes?["convertTo"].AsString(null);
            if (toCode == null) return;

            Block target = Api.World.GetBlock(new AssetLocation(toCode));
            if (target == null) return;

            // SetBlock removes this BE and places the target (which gets its own block entity, if any).
            Api.World.BlockAccessor.SetBlock(target.BlockId, Pos);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (listenerId != 0) UnregisterGameTickListener(listenerId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            convertAtTotalHours = tree.GetDouble("convertAtTotalHours", -1);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("convertAtTotalHours", convertAtTotalHours);
        }
    }
}
