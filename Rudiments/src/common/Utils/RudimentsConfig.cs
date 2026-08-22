namespace Rudiments
{
    public class RudimentsConfig
    {
        // ── Harvest realism ──────────────────────────────────────────────────────────
        // These two flags are copied into the world config at server start so the JSON
        // patches can condition on them — changes need a server restart to fully apply.

        /// <summary>
        /// Staged flax harvest: flax drops nothing until it blooms. Cut in bloom (stage 8)
        /// for seed-free bundles that ret from standard toward fine; let it fully mature
        /// (stage 9) for seeds and grain plus coarser bundles capped at standard (like nettle).
        /// When false, the pre-0.11 flax table is restored: bundles from stage 3 with seeds
        /// at every stage, and every bundle rets coarse-to-fine. Nettle is capped at standard
        /// regardless of this setting. Default: true. Requires restart.
        /// </summary>
        public bool FlaxBloomHarvest { get; set; } = true;

        /// <summary>
        /// Vanilla crops only return seeds once fully grown: immature crops drop nothing when
        /// broken, and damaged or animal-eaten crops lose their guaranteed seed return.
        /// When false, vanilla behavior is restored (immature crops drop ~0.7 seeds, dead
        /// crops always return a seed). Default: true. Requires restart.
        /// </summary>
        public bool SeedsOnlyWhenMature { get; set; } = true;

        /// <summary>Maximum number of bundles that fit in one stook. Default: 64.</summary>
        public int StookMaxBundles { get; set; } = 64;

        // ── Barrel retting ───────────────────────────────────────────────────────────

        /// <summary>Litres of water/limewater required (and consumed) per bundle in the barrel
        /// retting-bath recipes. A 50-litre barrel fits 50/this many bundles, e.g. 1.0 → 50
        /// bundles, 4.0 → 12 bundles. Applied by editing the loaded BarrelRecipe ingredients in
        /// AssetsFinalize (assets/rudiments/recipes/barrel/retting-*.json ship with 1.0).
        /// Default: 1.0. Requires restart.</summary>
        public float BarrelRettingLitresPerBundle { get; set; } = 1.0f;

        // ── Rippling ─────────────────────────────────────────────────────────────────

        /// <summary>Global multiplier on flax grain yielded per bundle at the ripple. Per-tier base
        /// values (avg/var) live in blocktypes/tool/ripple.json. Set to 0 to disable grain from
        /// rippling entirely. Default: 1.0.</summary>
        public float RippleGrainYieldMultiplier { get; set; } = 1.0f;

        /// <summary>Global multiplier on flax seeds yielded per bundle at the ripple. Per-tier base
        /// values live in blocktypes/tool/ripple.json. Set to 0 to disable seeds from rippling
        /// entirely. Default: 1.0.</summary>
        public float RippleSeedYieldMultiplier { get; set; } = 1.0f;

        // ── Linseed oil (fruit press) ───────────────────────────────────────────────
        // Flax seeds are pressed in the vanilla fruit press via a juiceablePropertiesByType
        // patch (assets/rudiments/patches/linseedoil-fruitpress.json), the same generic
        // mechanism apples and olives use — no custom block involved.

        /// <summary>Litres of game:oilportion-flax extracted per flax seed pressed in the vanilla
        /// fruit press. Applied by editing the loaded item's juiceablePropertiesByType attribute in
        /// AssetsFinalize (linseedoil-fruitpress.json ships with 0.03). The pressed byproduct
        /// (rudiments:linseedcake) is produced deterministically per the vanilla fruit press's own
        /// rules — there is no chance/quantity field to tune here. Default: 0.03. Requires restart.</summary>
        public float LinseedOilLitresPerSeed { get; set; } = 0.03f;

        // ── Scutching ────────────────────────────────────────────────────────────────
        // The scutch board is an interactive minigame: load broken bundles, strike them with a
        // scutching sword, flip halfway. Every value here is read live, so /rudimentsreload is
        // enough to retune the whole thing without a rebuild. Per-tier capacity and
        // boon-per-stroke live in blocktypes/tool/scutchboard.json.

        /// <summary>Strokes per second while holding leftmouse with a scutching sword. Kept in sync
        /// with the vanilla "axechop" animation the sword plays: 34 frames at animationSpeed 1.65
        /// (30 fps) is one 0.687 s cycle, so 1.456 strokes/second. Change the animation and this
        /// number must change with it. Default: 1.456.</summary>
        public float ScutchStrokesPerSecond { get; set; } = 1.456f;

        /// <summary>Global multiplier on how much boon each stroke knocks free. Per-tier base values
        /// (<c>boonPerStroke</c>: 0.12 / 0.16 / 0.20) live in blocktypes/tool/scutchboard.json.
        /// Higher values mean fewer strokes to clean a side. Default: 1.0.</summary>
        public float ScutchBoonPerStrokeMultiplier { get; set; } = 1.0f;

        /// <summary>Fibre integrity lost per stroke once the worked side is completely clean and the
        /// blade is biting bare fibre. Damaged bundles are not destroyed — they come off as tow
        /// (rudiments:coarsefibers) instead of scutched line. Set to 0 to make over-scutching
        /// harmless. Default: 0.10.</summary>
        public float ScutchDamagePerStroke { get; set; } = 0.10f;

        /// <summary>How clean the worked side may get (0..1) before strokes start shredding line into
        /// tow. Below this, scutching is free; above it, damage ramps linearly to the full
        /// per-stroke cost. This is also the point at which the strike changes sound and the
        /// particles turn pale — the flip cue. Default: 0.75.</summary>
        public float ScutchSafeCleanliness { get; set; } = 0.75f;

        /// <summary>Fraction of a stroke's cleaning that bleeds onto the side you are *not* working —
        /// you grip the far end while striking the near one. Set to 0 for strictly independent
        /// sides (which makes flipping mandatory rather than merely worthwhile). Default: 0.10.</summary>
        public float ScutchCrossSideBleed { get; set; } = 0.10f;

        /// <summary>Nettle carries substantially more boon than flax, so its boon-per-stroke is
        /// divided by this — 1.5 means roughly half again as many strokes per side. Default: 1.5.</summary>
        public float ScutchNettleBoonMultiplier { get; set; } = 1.5f;

        /// <summary>Coarse fibres (tow) handed out per bundle that was shredded by over-scutching, or
        /// wasted by the mechanical scutch mill. Default: 2.</summary>
        public int ScutchTowFibersPerBundle { get; set; } = 2;

        /// <summary>Show numeric percentages for cleanliness and fibre integrity on the scutch board.
        /// When false, the board reports in qualitative words instead, for players who prefer to work
        /// by feel. Default: true.</summary>
        public bool ScutchShowMeters { get; set; } = true;

        /// <summary>Share of bundles the mechanical scutch mill wastes as tow instead of scutched
        /// bundles — mill scutching was faster than hand work but notoriously wasteful, and the
        /// millers were paid in the tow. Quality is not affected, only yield. Ejected as an item
        /// entity at the mill. Set to 0 to disable the waste. Default: 0.35.</summary>
        public float MechScutcherTowShare { get; set; } = 0.35f;

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

        // ── Ware tiers and kilns ─────────────────────────────────────────────────────
        // Fired clay comes in three bodies: earthenware (the default, and what a pit kiln
        // gives you), stoneware (anything out of a beehive kiln), and porcelain (its own
        // clay, and a real block variant rather than an attribute). Everything here is
        // read live except the two PorcelainClayPer* values, which edit loaded grid
        // recipes and therefore need a restart.

        /// <summary>Per-use shatter chance for untiered fired clay — the body a pit kiln gives you.
        /// Applies when a vessel is actually used: drunk from, filled, emptied, poured. Earthenware
        /// is deliberately the most fragile of the three: it is the most lightly fired and the most
        /// porous, so it wears out fastest in the hand. A bowl survives a median of ~46 uses. Set to
        /// 0 to disable wear breakage for earthenware. Default: 0.015.</summary>
        public double EarthenwareBreakChance { get; set; } = 0.015;

        /// <summary>Per-use shatter chance for stoneware — vitrified and sealed, and sturdier under
        /// ordinary handling than porous earthenware for it. Median ~138 uses. Default: 0.005.</summary>
        public double StonewareBreakChance { get; set; } = 0.005;

        /// <summary>Per-use shatter chance for porcelain. Zero by default: porcelain is fully
        /// vitrified and dense enough that ordinary use does not wear it out at all, only careless
        /// handling does — a dropped or thrown piece still shatters same as any fired clay, via
        /// <see cref="ThrownClayBreakChance"/> / <see cref="ClayImpactBreakSpeed"/> below, which do
        /// not distinguish by tier. Set above 0 to give porcelain a wear chance too.
        /// Default: 0.</summary>
        public double PorcelainBreakChance { get; set; } = 0;

        /// <summary>Widen "use" to include opening a placed container, so a stoneware storage vessel
        /// can fail when you reach into it. Off by default — carrying and opening are free, only
        /// working a vessel wears it. Default: false.</summary>
        public bool BreakageIncludesPlacedContainers { get; set; } = false;

        /// <summary>Chance that a deliberately dropped fired clay item shatters on landing. Set to 0
        /// to disable drop breakage entirely (the hard-landing trigger below still applies).
        /// Default: 1.0.</summary>
        public double ThrownClayBreakChance { get; set; } = 1.0;

        /// <summary>Break the whole dropped stack rather than a single item, so Ctrl+Q costs you all
        /// of it and Q costs you one. Default: true.</summary>
        public bool ThrownClayBreakWholeStack { get; set; } = true;

        /// <summary>Whether pottery scattered when you die also shatters. Death drops carry the same
        /// "dropped by a player" marker as a hand-thrown item, so without this exemption dying with
        /// a shelf of pottery in your bags would smash all of it. Default: false.</summary>
        public bool ThrownBreakOnDeathDrop { get; set; } = false;

        /// <summary>Downward speed at landing above which <em>any</em> fired clay item shatters,
        /// however it got there — pushed off a ledge, spilled from a broken shelf, thrown by an
        /// explosion. Set to 0 to disable the hard-landing trigger and leave only deliberate drops.
        /// Default: 0.4.</summary>
        public double ClayImpactBreakSpeed { get; set; } = 0.4;

        /// <summary>In-game hours for a full unsealed earthenware vessel to leak dry, linear to zero
        /// — half empty at half this. The loss is a share of each vessel's own capacity, so a 1 L
        /// bowl and a 3 L jug empty on the same clock. Sealed ware (stoneware, porcelain, or any
        /// glaze) never seeps. Set to 0 to disable seepage. Default: 12.</summary>
        public float EarthenwareEmptyHours { get; set; } = 12f;

        /// <summary>Blue clay converted per crushed quartz on the pulverizer route to porcelain clay.
        /// Applied by editing the loaded GridRecipe in AssetsFinalize
        /// (assets/rudiments/recipes/grid/porcelainclay-quartz.json ships with 2).
        /// Default: 2. Requires restart.</summary>
        public int PorcelainClayPerQuartz { get; set; } = 2;

        /// <summary>Blue clay converted per powdered flint + bonemeal on the bone-china route.
        /// Deliberately half the quartz route: bone china needs no metal at all, so it pays for that
        /// in clay. Applied the same way (porcelainclay-bonechina.json ships with 1).
        /// Default: 1. Requires restart.</summary>
        public int PorcelainClayPerFlint { get; set; } = 1;

        /// <summary>Minimum burn temperature a fuel must reach to fire ware to stoneware in a
        /// Rudiments kiln, paired with a burn duration above 30. This is the bloomery's own gate,
        /// verbatim: it clears charcoal (1300/40), coke (1340/40), bituminous coal (1200/84) and
        /// anthracite (1200/196). Fuel below this threshold — lignite, peat, every wood — is not
        /// refused any more: a kiln lights on it just fine, only cooler, and turns out earthenware
        /// instead of stoneware. Default: 1200.</summary>
        public int KilnMinFuelTemperature { get; set; } = 1200;

        /// <summary>In-game hours a small brick kiln takes to finish a firing. The bloomery's own
        /// figure. Default: 10.</summary>
        public float SmallBrickKilnBurnHours { get; set; } = 10f;

        /// <summary>Fuel items a small brick kiln needs for one firing. Also the hard cap on its
        /// fuel slot — it holds exactly one firing's worth, never a spare stack, and the whole slot
        /// is spent when it lights. Default: 4.</summary>
        public int SmallBrickKilnFuelPerFiring { get; set; } = 4;

        /// <summary>Whether an unsealed earthenware watering can refuses to fill. Sealed means
        /// stoneware, porcelain, or any glaze. Set to false to restore vanilla behaviour, where a
        /// day-one porous can waters crops indefinitely. Default: true.</summary>
        public bool SealedWareRequiredForWateringCan { get; set; } = true;

        /// <summary>In-game hours an updraft kiln takes to finish a firing. Longer than the small
        /// brick kiln, for double the capacity. Default: 12.</summary>
        public float UpdraftKilnBurnHours { get; set; } = 12f;

        /// <summary>Fuel items an updraft kiln needs for one firing, and the hard cap on its fuel
        /// slot — double the small kiln's, matching its double ware capacity. Default: 8.</summary>
        public int UpdraftKilnFuelPerFiring { get; set; } = 8;

        /// <summary>Per-item chance that porcelain comes out of an updraft kiln as shards. An updraft
        /// kiln is hottest at the firemouths and cools toward the crown, and that unevenness is why
        /// potters used saggars and why losses stayed routine even in the industrial era. It is also
        /// the reason to build a beehive kiln afterwards rather than instead — set this to 0 and the
        /// beehive becomes redundant. Default: 0.30.</summary>
        public double UpdraftKilnPorcelainFailChance { get; set; } = 0.30;

        // ── Lead poisoning ───────────────────────────────────────────────────────────
        // Lead glaze is the cheapest glaze in the game and works in a pit kiln on day one,
        // so without a cost there is no reason to ever use tin or salt. The cost is that it
        // leaches: a burden accrues per leaded helping eaten or drunk, decays whenever you
        // are not doing that, and past a grace threshold it eats into max health.
        // The lead travels with the food rather than with the pot — cook a stew in a leaded
        // pot and it is leaded in whatever bowl you finally eat it from. Everything here is
        // read live, so /rudimentsreload retunes it without a restart.

        /// <summary>Whether lead-glazed vessels poison the people who eat and drink from them. Set to
        /// false and lead glaze becomes a free seal again: no burden accrues, no food is marked, the
        /// tooltip warnings disappear, and any burden already on a character stops costing health
        /// immediately. The burden is kept rather than wiped, so switching back on is not an amnesty —
        /// <c>/rudimentslead clear</c> wipes one deliberately. <b>The server decides this one</b>, not
        /// the client: it is mirrored into the world config so a client cannot silently turn off the
        /// warnings while the server carries on poisoning them. Default: true.</summary>
        public bool LeadPoisoningEnabled { get; set; } = true;

        /// <summary>Burden gained per helping consumed from a lead-glazed vessel. A meal counts the
        /// servings actually eaten; a drink counts as one. Burden is measured in helpings, so this is
        /// really a global multiplier on how fast lead accumulates. Default: 1.0.</summary>
        public double LeadPerServing { get; set; } = 1.0;

        /// <summary>Burden shed per in-game day, always, whether or not the player is online — it is
        /// calendar-driven, like retting and nettle spread. This is the whole recovery mechanism:
        /// there is no antidote and no cure but time away from the stuff. At the default a player can
        /// take five helpings a day off leaded ware and never accumulate anything. Set to 0 for
        /// permanent, irreversible poisoning. Default: 5.0.</summary>
        public double LeadDecayPerDay { get; set; } = 5.0;

        /// <summary>Burden that costs nothing at all. Below this the body clears lead about as fast
        /// as it arrives, so there is no penalty and no message and drinking from a leaded jug once
        /// in a while is genuinely free. Set to 0 to make the very first helping count.
        /// Default: 15.0.</summary>
        public double LeadOnsetBurden { get; set; } = 15.0;

        /// <summary>Burden above the onset threshold per point of max health lost. Lower is harsher.
        /// At the defaults the first whole point goes at 27 burden and the penalty caps out at 75.
        /// Default: 12.0.</summary>
        public double LeadBurdenPerHealthPoint { get; set; } = 12.0;

        /// <summary>Most max health lead can ever take, out of the player's 15. A third is meant to
        /// be a serious, visible handicap that is still survivable and still reversible — lead
        /// poisoning should change how you play, not end the run. Default: 5.0.</summary>
        public double LeadMaxHealthPenalty { get; set; } = 5.0;
    }
}
