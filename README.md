# Rudiments

**A Vintage Story mod restoring the depth of pre-industrial craft.**

Rudiments adds realistic, multi-step production chains for the materials that defined life before iron — worked one process at a time, with each step mattering. Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

---

## Current Content — Fibre (v0.4.3)

Flax and stinging nettle processed through authentic production chains. Quality is earned, not given — retting timing determines fibre grade, and fine fibre carries real mechanical bonuses.

### Flax — 7 steps
`Harvest early` → `Stook-cure` → `Ripple` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

### Nettle — 6 steps
`Harvest mature` → `Stook-cure` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

### Retting quality
Bundles pass through **Coarse → Fine → Standard → Rot** as retting progresses. The fine window is brief. Water retting (vat) is fast and steady; field retting is free but weather-dependent. Lime retting (add quicklime to the vat) is faster still but permanently caps quality at Standard — good for bulk, not for craft work.

### Equipment

| Block | Function |
|---|---|
| Stook | Weather-aware curing and drying rack; bundles placed on the ground form the stook |
| Retting vat | Water retting with quality control and optional lime modifier |
| Drying rack | Safe indoor drying, quality preserved |
| Break | Breaks dried bundles |
| Scutch board | Removes woody shives; primitive / simple / advanced tiers |
| Hatchel | Final combing; primitive / simple / advanced tiers |
| Mechanical scutch mill | Axle-driven; handles breaking and scutching automatically |
| Oil press | Presses mature flax seeds into linseed oil |

### Fibre quality bonuses
Fine fibre twisted into **fine cord** gives a significant durability bonus on bows and is used in gambeson armour crafting.

### Nettle — a living weed
Young plants (stages 3–7) yield edible leaves — cook them in a pot to remove the raw sting. Mature plants (stages 7–9) yield fibre bundles.

Nettle no longer drops seeds — it propagates the way real nettle does, by **rhizome**:

- **Root crowns.** Cutting a plant at *any* stage leaves a small root crown (stub) that regrows on its own, or can be dug up with a shovel for a transplantable rhizome.
- **It spreads.** Wild nettle creeps into nearby fertile ground, strongly preferring **tilled farmland** — it thrives on disturbed earth. A built-in density cap stops patches from running away.
- **Networked feeder.** Nettle is one connected organism: it's efficient on its own soil (uses ~50% less nitrogen than an ordinary crop) but **leaches nitrogen from adjacent farmland** as it grows — and never drains its own kind. Keep it clear of your fields, or use it deliberately as a fallow-ground pest.
- **Invasive mode (optional, off by default).** When enabled, nettle spreads by *hidden* underground rhizomes that surface without warning. Tilling the soil clears buried rhizomes before they emerge.

Every part of this is tunable — see [Configuration](#configuration).

---

## Planned

These are directions, not promises. Each will be its own coherent addition when the time is right.

- **Clayworks** — hand-formed pottery with drying and firing stages; quality influenced by clay source and technique
- **Intermediate kilns** — the gap between a pit fire and a beehive kiln is large; filling it with updraft and clamp kilns
- **Mudwork** — wattle and daub, cob, adobe; building with what's underfoot
- **Stone tools** — friction-fitted heads that can disassemble with wear, knapped from specific stone types

---

## Installation

1. Download the latest `.zip` from [Releases](../../releases)
2. Drop it into your `VintagestoryData/Mods/` folder
3. No dependencies required

**Spinning wheel support is built in.** If you also run [Immersive Fibercraft](https://mods.vintagestory.at) (the spinning wheel mod), nettle fibre can be spun on the wheel — the integration activates automatically and is a no-op when that mod is absent. No separate compat download needed.

---

## Configuration

All settings live in `VintagestoryData/ModConfig/rudiments.json` (created on first launch). If [AutoConfigLib](https://mods.vintagestory.at) is installed, the same settings can be edited in-game through its ConfigLib window — no file editing needed.

### Nettle spread & invasiveness

| Setting | Default | Effect |
|---|---|---|
| `NettleSpreadEnabled` | `true` | Wild nettle spreads to neighbouring fertile ground. |
| `NettleSpreadChance` | `0.20` | Spread chance onto plain fertile soil (rolled per growth tick, once mature). |
| `NettleTilledSpreadChance` | `0.45` | Higher spread chance onto tilled farmland. |
| `NettleSpreadMatureStage` | `6` | Minimum growth stage before a plant starts spreading. |
| `NettleSpreadMaxDensity` | `5` | Spread halts once this many nettle blocks are nearby (anti-runaway cap). |
| `NettleSpreadDensityRadius` | `2` | Radius checked for the density cap. |
| `NettleCreepEnabled` | `false` | **Invasive mode.** On: spreads as an *invisible* buried rhizome that emerges later (heavier on performance). Off: spread places visible young nettle. |
| `NettleCreepEmergeChance` | `0.03` | How fast hidden rhizomes surface (lower = stays hidden longer). |

### Nettle as a heavy feeder

| Setting | Default | Effect |
|---|---|---|
| `NettleHeavyFeederEnabled` | `true` | Nettle leaches nitrogen from adjacent farmland as it grows. |
| `NettleNutrientConsumption` | `15` | Nitrogen the crop takes from its own soil. Nettle is efficient — 50% less than an ordinary crop (~30). |
| `NettleNeighborNitrogenDepletion` | `3` | Nitrogen leached from each adjacent farmland per growth (~10% of a normal crop's use). Farmland with nettle on it is exempt — nettle never drains its own kind. |
| `NettleAlwaysLeaveStub` | `true` | Cutting nettle at any stage leaves a regrowable root crown. |

### Reed spread (coopersreed, papyrus, tule, brownsedge)

| Setting | Default | Effect |
|---|---|---|
| `ReedSpreadEnabled` | `true` | Reeds slowly spread along suitable water/soil. |
| `ReedSpreadChance` | `0.03` | Spread chance per tick (relaxed). |
| `ReedSpreadMaxDensity` | `6` | Anti-runaway density cap. |
| `ReedSpreadDensityRadius` | `2` | Radius checked for the density cap. |

### Other

| Setting | Default | Effect |
|---|---|---|
| `StookMaxBundles` | `64` | Maximum bundles a single ground stook can hold. |

---

## Compatibility

| Version | Status |
|---|---|
| VS 1.22.x | Supported |

---

## Credits

Expanded from [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by OppoOtis.  
Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

---

## Licence

MIT — do what you like, credit appreciated.
