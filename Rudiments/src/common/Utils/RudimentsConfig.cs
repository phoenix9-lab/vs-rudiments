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

        /// <summary>Hard outward cap: a patch will not spread further than this many blocks from where it
        /// started. Children inherit the patch origin, so the whole patch is bounded. Set to 0 for
        /// UNLIMITED spread (nettle will grow without bound). Default: 16.</summary>
        public int NettleSpreadMaxRadius { get; set; } = 16;

        /// <summary>Minimum growth stage before a nettle attempts to spread. Default: 6.</summary>
        public int NettleSpreadMatureStage { get; set; } = 6;

        /// <summary>If true, nettle growing ON farmland will not spread (cultivated plots stay put).
        /// Wild nettle can still spread onto farmland. Default: false (nettle is invasive everywhere).</summary>
        public bool NettleFarmlandContainment { get; set; } = false;

        /// <summary>In-game days between spread attempts for a mature wild nettle. Calendar-driven,
        /// so it responds to time speed. Default: 1.</summary>
        public double NettleSpreadIntervalDays { get; set; } = 1.0;

        /// <summary>In-game days a wild nettle takes to advance one growth stage. Calendar-driven.
        /// Default: 3.</summary>
        public double NettleWildGrowthDaysPerStage { get; set; } = 3.0;

        /// <summary>In-game days a cut stub takes to regrow into stage-1 nettle. Calendar-driven.
        /// Default: 3.</summary>
        public double NettleStubRegrowDays { get; set; } = 3.0;

        /// <summary>In-game days a hidden buried rhizome takes to surface as stage-1 nettle.
        /// Calendar-driven. Default: 4.</summary>
        public double NettleCreepEmergeDays { get; set; } = 4.0;

        /// <summary>Leave a stub on every break, any stage, any soil. Default: true.</summary>
        public bool NettleAlwaysLeaveStub { get; set; } = true;

        /// <summary>Whether nettle drains neighbour-farmland nitrogen. Default: true.</summary>
        public bool NettleHeavyFeederEnabled { get; set; } = true;

        /// <summary>Own-soil N consumption applied to CropProps in OnLoaded. Nettle is efficient —
        /// 50% less than an ordinary crop (~30). Default: 15.</summary>
        public int NettleNutrientConsumption { get; set; } = 15;

        /// <summary>N leached from each adjacent (non-nettle) farmland per growth event — ~10% of a
        /// normal crop's use. Nettle never drains its own kind. Default: 3.</summary>
        public float NettleNeighborNitrogenDepletion { get; set; } = 3f;

        // ── Reed spread ──────────────────────────────────────────────────────────────

        /// <summary>Whether reedpapyrus spreads. Default: true.</summary>
        public bool ReedSpreadEnabled { get; set; } = true;

        /// <summary>Reed per-attempt spread chance (relaxed; no tilled bonus). Default: 0.03.</summary>
        public double ReedSpreadChance { get; set; } = 0.03;

        /// <summary>In-game days between spread attempts for a reed. Calendar-driven. Default: 2.</summary>
        public double ReedSpreadIntervalDays { get; set; } = 2.0;

        /// <summary>Reed density cap. Default: 6.</summary>
        public int ReedSpreadMaxDensity { get; set; } = 6;

        /// <summary>Reed density-cap scan radius. Default: 2.</summary>
        public int ReedSpreadDensityRadius { get; set; } = 2;
    }
}
