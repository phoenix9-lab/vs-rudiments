# Rudiments

**A Vintage Story mod restoring the depth of pre-industrial craft.**

Rudiments adds realistic, multi-step production chains for the materials and tools that defined life before iron — worked one process at a time, with each step mattering. Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

Pre-release — work in progress. New systems are added incrementally; existing ones are tested and balanced before the next is opened.

---

## Fibre Production

Flax and stinging nettle processed through authentic multi-step chains. Quality is earned, not given — retting timing determines fibre grade, and fine fibre carries real mechanical bonuses downstream.

### Flax — 7 steps

`Harvest early` → `Stook-cure` → `Ripple` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Harvest at stages 3–7 for bundles. Waiting to stage 8–9 gives mature seeds for linseed oil — you choose which yield to prioritise.

### Nettle — 6 steps

`Harvest mature` → `Stook-cure` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Nettle skips rippling. Young plants (stages 3–6) yield edible leaves — hold right-mouse for a slow careful harvest to avoid the sting, or wear gloves and break normally. Mature plants (stages 7–9) yield fibre bundles.

### Retting quality

Bundles pass through **Coarse → Fine → Standard → Rot** as retting progresses. The fine window is brief. Letting it pass drops quality back to Standard; leaving bundles indefinitely rots them.

- **Field retting** — free, but weather-dependent. Dry spells stall progress; rain restarts it.
- **Barrel retting** — seal bundles and water in a barrel; it immediately reopens as a retting bath with a visible progress readout. Controlled, predictable, faster.
- **Lime retting** — seal in limewater instead for maximum speed, but quality is permanently capped at Standard. Good for bulk, not craft work.

### Fibre quality bonuses

Fine fibre twisted into **fine cord** gives a significant durability bonus on bows and is used in gambeson armour crafting.

### Equipment

| Block | Function |
|---|---|
| Stook | Weather-aware curing and drying; bundles placed directly on the ground |
| Retting bath (barrel) | Seal bundles + water or limewater in any barrel — it reopens as a timed retting bath |
| Retting vat | Legacy block — existing vats still work but can no longer be crafted |
| Drying rack | Safe indoor drying; quality preserved |
| Break | Breaks dried bundles |
| Scutch board | Removes woody shives; primitive / simple / advanced tiers |
| Hatchel | Final combing; primitive / simple / advanced tiers |
| Mechanical scutch mill | Axle-driven; automates breaking and scutching |
| Oil press | Presses mature flax seeds into linseed oil |

---

## Nettle — an ~~invasive weed~~ resilient crop

Nettle no longer drops seeds. It propagates the way real nettle does: by **rhizome**.

- **Root crowns.** Cutting a plant at any stage leaves a root crown (stub) that regrows on its own, or can be dug up with a shovel for a transplantable rhizome.
- **It spreads.** Wild nettle creeps into nearby fertile ground, strongly preferring tilled farmland. A built-in density cap and outward radius limit stop patches from overrunning a world.
- **Networked feeder.** Nettle is efficient on its own soil (uses roughly half the nitrogen of an ordinary crop) but leaches nitrogen from adjacent farmland as it grows — and never drains its own kind. Keep it clear of your fields, or use it deliberately to exhaust ground you want to fallow.
- **Invasive mode (off by default).** When enabled, nettle spreads as invisible buried rhizomes that surface without warning. Tilling the soil clears them before they emerge.

All of this is tunable — see [Configuration](#configuration).

---

## Planned

These are directions, not promises. Each will be its own coherent addition when the time is right.

- **Pottery** — hand-formed pottery with drying and firing stages; quality influenced by clay source and technique
- **Intermediate kilns** — the gap between a pit fire and a beehive kiln is large; filling it with updraft and clamp kilns
- **Mudwork** — wattle and daub, cob, adobe; building with what's underfoot

---

## Configuration

All settings live in `VintagestoryData/ModConfig/rudiments.json` (created on first launch). If [AutoConfigLib](https://mods.vintagestory.at) is installed, settings can be edited in-game — no file editing needed.

Apply changes without restarting: `/rudimentsreload` (requires `controlserver` privilege).

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

### Other

| Setting | Default | Effect |
|---|---|---|
| `StookMaxBundles` | `64` | Maximum bundles a single ground stook can hold |
| `RippleGrainYieldMultiplier` | `1.0` | Global multiplier on grain yields from rippling; `0` disables grain drops |
| `RippleSeedYieldMultiplier` | `1.0` | Global multiplier on seed yields from rippling; `0` disables seed drops |

---

## Compatibility

| Version | Status |
|---|---|
| VS 1.22.x | Supported |

**Wool & More.** If [Wool & More](https://mods.vintagestory.at) is installed, Rudiments inserts a **hand carding** step into the wool chain: washed fleece must be carded into **rolags** before it can be twisted or spun into twine. Craft a pair of **hand cards** (2 planks + leather + metal nails & strips, 128 durability), hold washed wool fibres in your off hand and the cards in your active hand, then hold right-mouse — a couple of seconds of brushing yields a rolag per fibre. Rolags twist into wool twine in the grid (4 per twine). Everything ships disabled and is enabled by patches gated on the wool mod — zero footprint without it.

**Immersive Fibercraft** (spinning wheel / drop spindle). Nettle fibre and fine fibre gain `spinningProps` and can be spun on the wheel — nettle into flax twine, fine fibre into fine cord. With Wool & More also present, the carding requirement is enforced on the wheel and spindle too: washed wool fibres lose their direct spinnability and **rolags** become the spinnable stage instead (2 rolags → 1 wool twine, the same ratio Immersive Fibercraft uses for raw fibre). All integrations activate automatically and are no-ops if the mod is absent.

**Clayworks.** If [Clayworks](https://mods.vintagestory.at) is installed, its clay barrels work as first-class retting baths — sealing bundles and water pops them open into the same timed, quality-tracked retting process as wooden barrels. (Without this patch the clay barrel would complete the raw fallback recipe and ret near-instantly.)

**Toolsmith.** If [Toolsmith](https://mods.vintagestory.at/toolsmith) is installed, Rudiments' fine cord (`rudiments:finecord`) is registered as a premium binding material in its tool-tinkering system — a step above leather-tier cordage, matching its established "uniform, strong, and resistant to repeated stress" character. Nettle-spun twine needs no separate registration since it produces vanilla flax twine, which Toolsmith already supports natively. The integration is pure data — no code, no hard dependency — and a complete no-op if Toolsmith is absent. (This replaces Rudiments' earlier homegrown tool-binding system, which conflicted with Toolsmith's more comprehensive approach to the same idea.)

**AutoConfigLib / ConfigLib.** Supported for in-game config editing.

---

## Credits

Expanded from [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by OppoOtis.  
Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

---

## Licence

MIT — do what you like, credit appreciated.
