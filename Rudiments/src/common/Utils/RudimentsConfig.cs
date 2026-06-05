namespace Rudiments
{
    public class RudimentsConfig
    {
        /// <summary>Maximum number of bundles that fit in one stook. Default: 64.</summary>
        public int StookMaxBundles { get; set; } = 64;

        // ── Nettle spread ────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether the invasive hidden rhizome spread is enabled.
        /// When false, spread places visible crop-nettle-1 directly.
        /// Default: false (off by default — potentially heavy persistent invisible ticking blocks).
        /// </summary>
        public bool NettleCreepEnabled { get; set; } = false;

        /// <summary>Whether nettle spreads at all (visible or hidden). Default: true.</summary>
        public bool NettleSpreadEnabled { get; set; } = true;

        /// <summary>Per-tick spread chance onto plain fertile soil. Default: 0.20.</summary>
        public double NettleSpreadChance { get; set; } = 0.20;

        /// <summary>Per-tick spread chance onto tilled farmland. Default: 0.45.</summary>
        public double NettleTilledSpreadChance { get; set; } = 0.45;

        /// <summary>
        /// Density cap: abort spread if this many nettle-family blocks are found in radius.
        /// Set to 0 to disable cap.
        /// Default: 5.
        /// </summary>
        public int NettleSpreadMaxDensity { get; set; } = 5;

        /// <summary>Radius (blocks) for the density-cap scan. Default: 2.</summary>
        public int NettleSpreadDensityRadius { get; set; } = 2;

        /// <summary>Minimum growth stage before a wild nettle attempts to spread. Default: 6.</summary>
        public int NettleSpreadMatureStage { get; set; } = 6;

        /// <summary>Per-tick chance a hidden rhizome emerges as crop-nettle-1. Default: 0.03.</summary>
        public double NettleCreepEmergeChance { get; set; } = 0.03;

        /// <summary>Leave a stub on every break, any stage, any soil. Default: true.</summary>
        public bool NettleAlwaysLeaveStub { get; set; } = true;

        /// <summary>Whether nettle drains neighbour-farmland nitrogen. Default: true.</summary>
        public bool NettleHeavyFeederEnabled { get; set; } = true;

        /// <summary>Own-soil N consumption applied to CropProps in OnLoaded. Default: 45.</summary>
        public int NettleNutrientConsumption { get; set; } = 45;

        /// <summary>N drained from each neighbour farmland per growth event. Default: 4.</summary>
        public float NettleNeighborNitrogenDepletion { get; set; } = 4f;

        // ── Reed spread ──────────────────────────────────────────────────────────────

        /// <summary>Whether reedpapyrus spreads. Default: true.</summary>
        public bool ReedSpreadEnabled { get; set; } = true;

        /// <summary>Reed per-tick spread chance (relaxed; no tilled bonus). Default: 0.03.</summary>
        public double ReedSpreadChance { get; set; } = 0.03;

        /// <summary>Reed density cap. Default: 6.</summary>
        public int ReedSpreadMaxDensity { get; set; } = 6;

        /// <summary>Reed density-cap scan radius. Default: 2.</summary>
        public int ReedSpreadDensityRadius { get; set; } = 2;
    }
}
