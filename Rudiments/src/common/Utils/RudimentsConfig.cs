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

        /// <summary>Hard outward cap for reeds: a patch won't spread further than this many blocks from
        /// where it started. Set to 0 for UNLIMITED spread (reeds grow without bound). Default: 16.</summary>
        public int ReedSpreadMaxRadius { get; set; } = 16;

        // ── Tool binding methods ─────────────────────────────────────────────────────

        /// <summary>Durability multiplier for friction-fit (mortise/tenon) tools. Default: 0.35.</summary>
        public float FrictionDurabilityMul { get; set; } = 0.35f;

        /// <summary>Durability multiplier for rope/string-bound tools (baseline). Default: 1.0.</summary>
        public float RopeDurabilityMul { get; set; } = 1.0f;

        /// <summary>Durability multiplier for glue/pitch-bound tools. Default: 1.1.</summary>
        public float GlueDurabilityMul { get; set; } = 1.1f;

        /// <summary>Durability multiplier for nail-bound tools. Default: 1.25.</summary>
        public float NailDurabilityMul { get; set; } = 1.25f;

        /// <summary>Durability multiplier for combined nail+glue binding (top tier). Default: 1.5.</summary>
        public float GlueNailDurabilityMul { get; set; } = 1.5f;

        /// <summary>Per-use chance a friction-fit tool comes apart during use. Default: 0.04.</summary>
        public double FrictionFailChance { get; set; } = 0.04;

        /// <summary>Chance the stone head survives (drops as an item) when a friction-fit tool fails;
        /// otherwise it broke and drops nothing. Default: 0.6.</summary>
        public double HeadSurvivesChance { get; set; } = 0.6;

        /// <summary>In-game hours glue/pitch must cure before a glued tool becomes usable.
        /// Calendar-driven, so it responds to time speed. Default: 12.</summary>
        public double GlueCureHours { get; set; } = 12.0;

        /// <summary>If true, friction-fit recipes are only registered when ToolsRequireRope is present
        /// (friction-fit is only meaningful as a rope bypass). Default: true.</summary>
        public bool FrictionRequiresRopeMod { get; set; } = true;

        /// <summary>Tool-head materials eligible for friction-fit binding (stone-age tools only).</summary>
        public string[] FrictionStoneMaterials { get; set; } =
            { "flint", "chert", "granite", "andesite", "basalt", "obsidian", "peridotite" };

        /// <summary>The pitch-glue liquid consumed from a container when glue-binding (vanilla hot pitch
        /// glue). Like vanilla dough/oil-lamp recipes, it is drawn from a bucket/bowl and the empty
        /// container is returned. Hot glue is the wet, appliable form (it hardens to -cold).</summary>
        public string GlueLiquidContent { get; set; } = "game:glueportion-pitch-hot";

        /// <summary>Container block(s) the pitch glue may be supplied in. One recipe variant is derived
        /// per container, mirroring how vanilla handles liquid ingredients.</summary>
        public string[] GlueContainers { get; set; } = { "game:woodbucket", "game:bowl-*-fired" };

        /// <summary>Litres of pitch glue consumed per tool. Default: 0.25.</summary>
        public float GlueLitres { get; set; } = 0.25f;

        /// <summary>Item code (wildcard) for the nail binding ingredient (vanilla nails &amp; strips).</summary>
        public string NailIngredient { get; set; } = "game:metalnailsandstrips-*";

        /// <summary>Quantity of the nail ingredient consumed per tool. Default: 1.</summary>
        public int NailQuantity { get; set; } = 1;

        /// <summary>Ingredient code path fragments treated as a "rope/cordage" binding to be replaced or
        /// removed when deriving alternative-binding recipes. Matches if the ingredient code path
        /// contains any of these.</summary>
        public string[] RopeBindingCodes { get; set; } =
            { "rope", "flaxtwine", "twine", "cordage" };
    }
}
