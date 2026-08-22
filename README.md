# Rudiments

**AI Disclosure:** This mod builds on the original source code of [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by @Oppo. Ongoing modifications and maintenance are done primarily with the assistance of Anthropic's Claude models (Opus/Sonnet). If you'd prefer to avoid AI-assisted content, please take this into account.

**A Vintage Story mod restoring the depth of pre-industrial craft.**

Rudiments adds realistic, multi-step production chains for the materials and tools that defined life before iron — worked one process at a time, with each step mattering. Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

Feature-stable as of 1.0 — fibre production and pottery/kilns are both complete, tested chains. New systems are still added incrementally, each balanced against what's already there before the next is opened.

---

## Fibre Production

Flax and stinging nettle processed through authentic multi-step chains. Quality is earned, not given — harvest timing sets the fibre's potential, retting timing determines the grade within it, and fine fibre carries real mechanical bonuses downstream.

### Flax — 7 steps

`Harvest after bloom` → `Stook-cure` → `Ripple` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Flax drops nothing until it has bloomed — then the timing decision is real:

- **Cut in bloom (stage 8):** the best fibre. Bundles ret from *standard* quality with a chance at the brief *fine* window — but they carry **no seed at all**, not from the crop and not at the ripple.
- **Fully mature (stage 9):** the plant drops seeds and grain, and rippling its bundles recovers more of both — but the stems have coarsened. Mature bundles ret from *coarse* and top out at *standard*, like nettle.

Fine fibre comes exclusively from bloom-cut flax; seeds and linseed oil come exclusively from mature flax. You can't have both from one plant.

### Nettle — 6 steps

`Harvest mature` → `Stook-cure` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Nettle skips rippling. Young plants (stages 3–6) yield edible leaves — hold right-mouse for a slow careful harvest to avoid the sting, or wear gloves and break normally. Mature plants (stages 7–9) yield fibre bundles.

### Retting quality

Harvest timing sets the quality range; retting timing decides where you land in it.

- **Bloom-cut flax** rets **Standard → Fine → Standard → Rot**. The fine window is brief — catch it or settle for standard.
- **Mature flax and nettle** ret **Coarse → Standard → Rot**. Patience improves them; fine is out of reach.

- **Field retting** — free, but weather-dependent. Dry spells stall progress; rain restarts it.
- **Barrel retting** — seal bundles and water in a barrel; it immediately reopens as a retting bath with a visible progress readout. Controlled, predictable, faster.
- **Lime retting** — seal in limewater instead for maximum speed, but quality is permanently capped at Standard. Good for bulk, not craft work.

### Scutching — the second quality lever

Retting is not the only decision that matters. Scutching is a hands-on step: load broken bundles onto a **scutch board**, then hold left-mouse with a **scutching sword** to beat the woody boon off them.

- **Only the end facing you is being worked.** Sneak + right-click turns the bundle so you can do the other half. Never turning it caps how clean the batch can get.
- **Listen for the change.** While there is boon left the strike lands dull and throws brown shives. Once that end runs clean it sharpens in pitch and throws pale fibre fluff — that is your cue to turn it, because from then on the blade has nothing left to ride on and starts cutting the long *line* into short *tow*.
- **Both mistakes cost you something different.** Under-scutching leaves boon bound in and grades the batch down; over-scutching keeps the grade but reclassifies more and more of the batch into coarse fibres. Neither is a timer — both fall out of the same curve.
- **Retting stays the ceiling.** Scutching can only lose the quality retting granted, never exceed it.

Board tiers are craftsmanship in wood, not metal: a better-cut notch holds more bundles *and* clears more boon per stroke. Nettle carries far more boon than flax and wants about half again as many strokes per end.

The **mechanical scutch mill** remains the hands-off route, and it is a genuine trade rather than a straight upgrade: it shreds a share of every batch into tow that drops out at the mill. It costs yield, not grade — exactly the complaint the historical millers, who were paid in that tow, kept getting.

### Fibre quality bonuses

Fine fibre twisted into **fine cord** gives a significant durability bonus on bows and is used in gambeson armour crafting.

### Equipment

| Block | Function |
|---|---|
| Stook | Weather-aware curing and drying; bundles placed directly on the ground |
| Ripple | Combs seed heads from cured bundles — mature bundles yield seeds and grain, bloom-cut bundles none |
| Retting bath (barrel) | Seal bundles + water or limewater in any barrel — it reopens as a timed retting bath |
| Retting vat | Legacy block — existing vats still work but can no longer be crafted |
| Drying rack | Safe indoor drying; quality preserved |
| Break | Breaks dried bundles |
| Scutch board | Interactive scutching — load bundles, beat them with a scutching sword, turn them halfway. Split-log / planed / joiner's tiers scale capacity and boon cleared per stroke |
| Scutching sword | The wooden swingle you beat the board with. Deliberately dulled so it scrapes rather than cuts |
| Hatchel | Final combing; primitive / simple / advanced tiers |
| Mechanical scutch mill | Axle-driven; automates breaking and scutching, at the cost of shredding a share of each batch into tow |
| Oil press | Presses mature flax seeds into linseed oil (vanilla `oilportion-flax`) |
| Vanilla fruit press | Also presses flax seeds into linseed oil — a `juiceableProperties` patch, no custom block. Slated to replace the dedicated oil press above once confirmed working in practice |

---

## Nettle — an ~~invasive weed~~ resilient crop

Nettle no longer drops seeds. It propagates the way real nettle does: by **rhizome**.

- **Root crowns.** Cutting a plant at any stage leaves a root crown (stub) that regrows on its own, or can be dug up with a shovel for a transplantable rhizome.
- **It spreads.** Wild nettle creeps into nearby fertile ground, strongly preferring tilled farmland. A built-in density cap and outward radius limit stop patches from overrunning a world.
- **Networked feeder.** Nettle is efficient on its own soil (uses roughly half the nitrogen of an ordinary crop) but leaches nitrogen from adjacent farmland as it grows — and never drains its own kind. Keep it clear of your fields, or use it deliberately to exhaust ground you want to fallow.
- **Invasive mode (off by default).** When enabled, nettle spreads as invisible buried rhizomes that surface without warning. Tilling the soil clears them before they emerge.

All of this is tunable — see [Configuration](#configuration).

---

## Seeds only from mature crops

Vanilla hands seeds back almost no matter what happens to a crop. Rudiments closes those loopholes — for **every** vanilla crop, not just flax:

- **Breaking an immature crop returns nothing.** No more pulling half-grown plants for a guaranteed seed refund.
- **Damaged crops lose their seed exemption.** Vanilla quietly excluded seeds from frost/heat damage multipliers; now seeds take the same hit as the rest of the harvest.
- **A crop eaten by an animal returns nothing.** Vanilla's dead-crop code literally contains a "minor hack to make dead crop always drop seeds" — that hack is gone. If the deer got your turnips, it got your seeds too. Fence your fields.

Nettle is unaffected — it never dropped seeds; it regrows from rhizomes.

Both this and the bloom-stage flax harvest can be disabled in the [config](#configuration) for a gentler game.

---

## Pottery & Kilns

Fired clay comes in three progressively better bodies, and which one you get depends on the kiln you
fire it in and — for the two custom kilns — the fuel you fire it with.

### Ware tiers

| Tier | How you get it | Behaviour |
|---|---|---|
| Earthenware | The default. A pit kiln, or a small brick/updraft kiln fired on wood, peat or lignite | Porous — leaks liquids over time unless glazed; the most fragile in the hand |
| Stoneware | Any beehive firing, or a small brick/updraft kiln fired on charcoal, coke, bituminous or anthracite coal | Vitrified and sealed; sturdier than earthenware |
| Porcelain | Its own clay (`clay-porcelain`, from blue clay via a pulverizer or a firepit+quern route), fired white only in a beehive kiln with every door shut | Vitrified, dense, and does not wear from ordinary use — only from being dropped or thrown |

- **Seepage.** Unsealed earthenware leaks: half empty at 6 in-game hours, dry at 12 — a share of
  *each vessel's own capacity*, so a 1 L bowl and a 3 L jug empty on the same clock.
- **Wear fragility.** Using a vessel — drinking, filling, emptying, pouring — carries a per-use
  shatter chance, highest for earthenware and zero by default for porcelain.
- **Drop breakage.** Any fired clay item shatters if thrown or dropped hard enough, spilling its
  contents rather than voiding them.
- **The watering-can gate.** An unsealed earthenware can refuses to fill from a water source — glaze
  it or move up the ladder.

### The kiln ladder

`Pit kiln` (free, earthenware only) → `Small brick kiln` / `Updraft kiln` → `Beehive kiln` (vanilla, downdraft, even heat)

- **Small brick kiln** — a single block, four ware slots, built from bricks the pit kiln already
  gives you. The cheapest way off earthenware.
- **Updraft kiln** — double the capacity, needs a chimney directly above it, and is the first kiln
  that can fire porcelain — unevenly: expect to lose around 30% of a porcelain load to shards. The
  vanilla beehive kiln's even downdraft heat is the reliable answer to that loss.
- **Fuel decides the tier, not a pass/fail gate.** Charcoal, coke, bituminous and anthracite coal fire
  stoneware, and in the updraft kiln give porcelain its chance at surviving whole. Wood, peat and
  lignite still fire the kiln — just cooler — so the load comes out earthenware instead, and
  porcelain gets no roll at all. Nothing is refused at the mouth.
- **One firing's worth, no more.** Each kiln's fuel slot caps at exactly what one firing needs (4 for
  the small kiln, 8 for the updraft) and burns down to nothing when it lights — no stockpiling a
  64-stack for dozens of firings.

### Glazes

Right-click bone-dry greenware — a pile on the ground, or held in your off hand — to apply. No glaze
item and no second firing: it fires alongside the piece in any kiln, the pit kiln included.

| Glaze | Applied with | Cost |
|---|---|---|
| Lead | A galena nugget, one per vessel | The cheapest seal in the game — but eating or drinking from leaded ware builds a burden that costs max health past a threshold. Decays on its own if you ease off; `/rudimentslead` shows your number |
| Tin | A cassiterite nugget over already-leaded greenware, two nuggets | Same seal, no poisoning — a lead glaze opacified with tin, same as it was historically |
| Salt | A handful thrown into a lit small brick or updraft kiln | Seals the entire load at once — needs a kiln hot enough to fire stoneware |

Lead poisoning is fully configurable, decays daily whether you're online or not, and can be turned off
server-side (`LeadPoisoningEnabled`) — see [Configuration](#configuration).

---

## Planned

These are directions, not promises. Each will be its own coherent addition when the time is right.

- **Mudwork** — wattle and daub, cob, adobe; building with what's underfoot

---

## Configuration

All settings live in `VintagestoryData/ModConfig/rudiments.json` (created on first launch). If [AutoConfigLib](https://mods.vintagestory.at) is installed, settings can be edited in-game — no file editing needed.

Apply changes without restarting: `/rudimentsreload` (requires `controlserver` privilege). Exception: the two harvest-realism flags below gate JSON patches and need a **server restart** to fully apply.

### Harvest realism

| Setting | Default | Effect |
|---|---|---|
| `FlaxBloomHarvest` | `true` | Staged flax harvest: nothing before bloom, fine-capable seed-free bundles at stage 8, seeds/grain plus standard-capped bundles at stage 9. `false` restores the pre-0.11 table (bundles from stage 3, seeds at every stage, all bundles ret coarse-to-fine). Nettle stays capped at standard either way. Restart required. |
| `SeedsOnlyWhenMature` | `true` | Vanilla crops only return seeds once fully grown — immature breaks, damaged crops, and animal-eaten crops return nothing. `false` restores vanilla behavior. Restart required. |

### Scutching

| Setting | Default | Effect |
|---|---|---|
| `ScutchBoonPerStrokeMultiplier` | `1.0` | Global multiplier on boon cleared per stroke. Raise it to shorten the whole minigame |
| `ScutchDamagePerStroke` | `0.10` | Fibre integrity lost per stroke once the worked end is clean. `0` makes over-scutching harmless |
| `ScutchSafeCleanliness` | `0.75` | How clean an end may get before strokes start cutting line into tow — also where the strike changes sound |
| `ScutchCrossSideBleed` | `0.10` | Share of each stroke's cleaning that bleeds onto the end you are *not* working. `0` makes flipping mandatory rather than merely worthwhile |
| `ScutchNettleBoonMultiplier` | `1.5` | How much more boon nettle carries than flax, as a divisor on boon cleared per stroke |
| `ScutchTowFibersPerBundle` | `2` | Coarse fibres handed out per bundle lost to over-scutching or wasted by the mill |
| `ScutchStrokesPerSecond` | `1.456` | Stroke rate. Matched to the sword's swing animation — change one and change the other |
| `ScutchShowMeters` | `true` | Show numeric cleanliness and fibre-intact percentages. `false` reports in qualitative words instead |
| `MechScutcherTowShare` | `0.35` | Share of each batch the mechanical mill shreds into tow. Quality is never affected; `0` disables the waste |

### Nettle spread and invasiveness

| Setting | Default | Effect |
|---|---|---|
| `NettleSpreadEnabled` | `true` | Whether nettle spreads at all |
| `NettleSpreadChance` | `0.20` | Spread chance onto plain fertile soil per attempt |
| `NettleTilledSpreadChance` | `0.45` | Higher spread chance onto tilled farmland |
| `NettleSpreadIntervalDays` | `1` | In-game days between spread attempts for a mature plant |
| `NettleSpreadMatureStage` | `6` | Minimum growth stage before a plant starts spreading |
| `NettleFarmlandContainment` | `false` | If `true`, nettle grown on farmland won't spread outward |
| `NettleSpreadMaxDensity` | `5` | Local density cap — does not limit outward reach |
| `NettleSpreadDensityRadius` | `2` | Radius checked for the density cap |
| `NettleSpreadMaxRadius` | `16` | Hard outward cap in blocks. `0` = unlimited |
| `NettleWildGrowthDaysPerStage` | `3` | Days for a wild nettle to advance one growth stage |
| `NettleStubRegrowDays` | `3` | Days a cut stub takes to regrow to stage 1 |
| `NettleCreepEnabled` | `false` | Invasive mode: spreads as invisible buried rhizomes |
| `NettleCreepEmergeDays` | `4` | Days a buried rhizome takes to surface |

### Nettle as a heavy feeder

| Setting | Default | Effect |
|---|---|---|
| `NettleHeavyFeederEnabled` | `true` | Nettle leaches nitrogen from adjacent farmland |
| `NettleNutrientConsumption` | `15` | Nitrogen taken from its own soil per growth event |
| `NettleNeighborNitrogenDepletion` | `3` | Nitrogen leached from each adjacent farmland block per growth |
| `NettleAlwaysLeaveStub` | `true` | Cutting nettle at any stage leaves a regrowable root crown |

### Reed spread

| Setting | Default | Effect |
|---|---|---|
| `ReedSpreadEnabled` | `true` | Reeds slowly spread along suitable water and soil |
| `ReedSpreadChance` | `0.03` | Spread chance per attempt |
| `ReedSpreadIntervalDays` | `2` | In-game days between spread attempts |
| `ReedSpreadMaxDensity` | `6` | Local density cap |
| `ReedSpreadMaxRadius` | `16` | Hard outward cap in blocks. `0` = unlimited |
| `ReedSpreadDensityRadius` | `2` | Radius checked for the density cap |

### Ware tiers, kilns and glaze

| Setting | Default | Effect |
|---|---|---|
| `EarthenwareBreakChance` | `0.015` | Per-use shatter chance for earthenware. `0` disables wear breakage for it |
| `StonewareBreakChance` | `0.005` | Per-use shatter chance for stoneware |
| `PorcelainBreakChance` | `0` | Per-use shatter chance for porcelain. `0` means it only breaks from drops/throws, never ordinary use |
| `BreakageIncludesPlacedContainers` | `false` | If `true`, opening a placed container counts as a "use" for wear breakage too |
| `ThrownClayBreakChance` | `1.0` | Chance a deliberately dropped fired clay item shatters on landing. `0` disables it (the hard-landing trigger below still applies) |
| `ThrownClayBreakWholeStack` | `true` | Break the whole dropped stack (Ctrl+Q) rather than one item (Q) |
| `ThrownBreakOnDeathDrop` | `false` | Whether pottery scattered on death also shatters |
| `ClayImpactBreakSpeed` | `0.4` | Downward landing speed above which any fired clay shatters, however it got there. `0` disables the hard-landing trigger |
| `EarthenwareEmptyHours` | `12` | In-game hours for a full unsealed earthenware vessel to seep dry. `0` disables seepage |
| `KilnMinFuelTemperature` | `1200` | Minimum burn temperature (paired with burn duration > 30) a fuel needs to fire stoneware instead of earthenware in a small brick or updraft kiln |
| `SmallBrickKilnBurnHours` | `10` | In-game hours a small brick kiln takes to finish a firing |
| `SmallBrickKilnFuelPerFiring` | `4` | Fuel a small brick kiln needs per firing — also the hard cap on its fuel slot |
| `UpdraftKilnBurnHours` | `12` | In-game hours an updraft kiln takes to finish a firing |
| `UpdraftKilnFuelPerFiring` | `8` | Fuel an updraft kiln needs per firing — also the hard cap on its fuel slot |
| `UpdraftKilnPorcelainFailChance` | `0.30` | Per-item chance porcelain comes out of an updraft kiln as shards instead of whole, on a hot-enough firing |
| `SealedWareRequiredForWateringCan` | `true` | Whether an unsealed earthenware watering can refuses to fill |
| `PorcelainClayPerQuartz` | `2` | Blue clay converted per crushed quartz on the pulverizer route to porcelain clay. Requires restart |
| `PorcelainClayPerFlint` | `1` | Blue clay converted per powdered flint + bonemeal on the bone-china route. Requires restart |

### Lead poisoning

| Setting | Default | Effect |
|---|---|---|
| `LeadPoisoningEnabled` | `true` | Whether lead-glazed vessels poison whoever eats or drinks from them. Server-authoritative — a client can't silently disable the warnings while still being poisoned |
| `LeadPerServing` | `1.0` | Burden gained per helping consumed from a lead-glazed vessel |
| `LeadDecayPerDay` | `5.0` | Burden shed per in-game day, online or off — the only cure is time away from leaded ware |
| `LeadOnsetBurden` | `15.0` | Burden below which nothing happens at all — occasional use off leaded ware is free |
| `LeadBurdenPerHealthPoint` | `12.0` | Burden above the onset threshold per point of max health lost |
| `LeadMaxHealthPenalty` | `5.0` | Most max health lead poisoning can ever take, out of 15 |

### Other

| Setting | Default | Effect |
|---|---|---|
| `StookMaxBundles` | `64` | Maximum bundles a single ground stook can hold |
| `RippleGrainYieldMultiplier` | `1.0` | Global multiplier on grain yields from rippling; `0` disables grain drops |
| `RippleSeedYieldMultiplier` | `1.0` | Global multiplier on seed yields from rippling; `0` disables seed drops |
| `LinseedOilLitresPerSeed` | `0.03` | Litres of vanilla `oilportion-flax` extracted per flax seed pressed in the vanilla fruit press. Restart required |

---

## Compatibility

| Version | Status |
|---|---|
| VS 1.22.x | Supported |

**Wool & More.** If [Wool & More](https://mods.vintagestory.at) is installed, Rudiments inserts a **hand carding** step into the wool chain: washed fleece must be carded into **rolags** before it can be twisted or spun into twine. Craft a pair of **hand cards** (2 planks + leather + metal nails & strips, 128 durability), hold washed wool fibres in your off hand and the cards in your active hand, then hold right-mouse — a couple of seconds of brushing yields a rolag per fibre. Rolags twist into wool twine in the grid (4 per twine). Everything ships disabled and is enabled by patches gated on the wool mod — zero footprint without it.

**Immersive Fibercraft** (spinning wheel / drop spindle). Nettle fibre and fine fibre gain `spinningProps` and can be spun on the wheel — nettle into flax twine, fine fibre into fine cord. With Wool & More also present, the carding requirement is enforced on the wheel and spindle too: washed wool fibres lose their direct spinnability and **rolags** become the spinnable stage instead (2 rolags → 1 wool twine, the same ratio Immersive Fibercraft uses for raw fibre). All integrations activate automatically and are no-ops if the mod is absent.

**Clayworks.** If [Clayworks](https://mods.vintagestory.at) is installed, its clay barrels work as first-class retting baths — sealing bundles and water pops them open into the same timed, quality-tracked retting process as wooden barrels. (Without this patch the clay barrel would complete the raw fallback recipe and ret near-instantly.)

**String Sense.** [String Sense](https://mods.vintagestory.at/stringsense) adds a crude cordage tier below twine and re-points primitive recipes at it — a natural partner for Rudiments' harder-earned twine. Rudiments wires its flax chain into that tier the same way String Sense's own AgeOfFlax compat did: its vanilla-flax strand shortcuts (which Rudiments' crop changes orphan) are disabled, and **crude flax cord twists directly from rippled bundles** (3 → 1) — no retting required. The full chain remains the only route to proper twine and fine cord.

**Toolsmith.** If [Toolsmith](https://mods.vintagestory.at/toolsmith) is installed, Rudiments' fine cord (`rudiments:finecord`) is registered as a premium binding material in its tool-tinkering system — a step above leather-tier cordage, matching its established "uniform, strong, and resistant to repeated stress" character. Nettle-spun twine needs no separate registration since it produces vanilla flax twine, which Toolsmith already supports natively. The integration is pure data — no code, no hard dependency — and a complete no-op if Toolsmith is absent. (This replaces Rudiments' earlier homegrown tool-binding system, which conflicted with Toolsmith's more comprehensive approach to the same idea.)

**AutoConfigLib / ConfigLib.** Supported for in-game config editing.

---

## Credits

Expanded from [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by @Oppo.  
Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

---

## Licence

MIT — do what you like, credit appreciated.
