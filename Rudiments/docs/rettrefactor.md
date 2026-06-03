# AgeOfFibers — Field Retting Rework + Stook Drying

## Context

The mod's field/dew retting is currently a crafted `rettingbed` block — a little
wood-and-water trough (see [rettingbed.json](Rudiments/assets/rudiments/blocktypes/tool/rettingbed.json),
textures `wood`/`water`) auto-placed by [ItemFieldRettableBundle.cs](Rudiments/src/common/item/ItemFieldRettableBundle.cs).
The user finds this conceptually wrong: dew retting is just bundles **laid on the
ground** weathering — not a built structure — and a `recipes/grid/rettingbed.json`
crafting recipe still exists for it. Separately, the real flax process (per the
[wholesomelinen 8-step](https://wholesomelinen.com/blogs/news/105505734-8-step-process-of-turning-flax-plant-into-natural-fiber)
and [Wikipedia](https://en.wikipedia.org/wiki/Flax) references) has **two drying
moments** the mod doesn't model: (1) curing the freshly-pulled green plants in
**stooks** before rippling/retting, and (2) drying again after retting. Only the
post-ret drying rack exists today.

Nettle follows the same process as flax minus rippling.

### Governing design model (from user direction)

Retting and drying are two faces of one **ambient-moisture** mechanic:

| Condition | Effect on a field bundle |
|---|---|
| **Wet** (rain/humidity) | retting **advances**; any drying progress **resets** |
| **Arid** (dry/warm) | drying **advances**; retting **stalls** (no progress, no harm) |

Consequences the design must honor:
- Field retting in a drought "just dries without retting" → retting **stalls**, no quality loss; player waits for moisture or moves to a vat.
- Drying a retted bundle outdoors (stook) is **risky**: rain stops/resets the drying and **restarts retting**, over-retting the fibre and **degrading quality** (Fine→Standard→Coarse→Rot).
- Drying in arid air never hurts quality — it only locks the fibre in.
- The crafted **water vat** (controlled ret) + **drying rack** (sheltered, weather-proof — already implemented) become the safe, quality-preserving path. **Field-ret + stook** are the free, weather-exposed, risky path.

## Target chains

```
Flax:   unprocessed(green) ─stook cure→ cured ─ripple→ rippled ─field-ret OR vat→ retted ─stook OR rack→ dried ─break→ broken ─scutch→ scutched ─hatchel→ fibres
Nettle: unprocessed(green) ─stook cure→ cured ───────────────→ ───field-ret OR vat→ retted ─stook OR rack→ dried ─break→ broken ─scutch→ scutched ─hatchel→ fibres
```

New stage: **`cured`** (green `unprocessed` must be stook-cured before rippling/retting).
"Field-ret" and "stook" are both **placed by right-clicking the ground** with the
bundle — no crafting — mirroring the existing auto-place UX.

Right-click-ground routing (one item class decides by bundle state):
- `*-unprocessed` (green) → place **stook** (cure mode)
- `flaxbundle-rippled` / `nettlebundle-cured` → place **field-retting** (laid bundles)
- `*-retted` → place **stook** (dry mode, risky)
- everything else → fall through to vanilla GroundStorable

## Implementation

### A. Field retting: drop the "bed", lay bundles on the ground
- Rename block code `rettingbed` → `fieldretting`. Re-skin
  [rettingbed.json](Rudiments/assets/rudiments/blocktypes/tool/rettingbed.json)
  → `fieldretting.json`: a **flat pile of bundles on grass** (new shape under
  `shapes/block/tool/fieldretting/`, reuse flax/nettle bundle textures), drop the
  `wood`/`water` textures and the trough look. Keep the 0.25-high collision box.
  Remove from `decorative` creative tab.
- **Delete** `recipes/grid/rettingbed.json` (no longer craftable).
- Keep the working state machine in
  [BlockEntityRettingBed.cs](Rudiments/src/common/blockentity/BlockEntityRettingBed.cs)
  /[BlockEntityRettingBase.cs](Rudiments/src/common/blockentity/BlockEntityRettingBase.cs).
  Rename class/refs to `FieldRetting` and update `InvKey`/`LangPrefix`/registration in
  [RudimentsModSystem.cs](Rudiments/RudimentsModSystem.cs).
- Tweak `GetProgressRate()`: when `climate.Rainfall` is below a dry threshold, return
  ~0 so arid weather **stalls** retting (currently it creeps forward via the `0.35`
  moisture floor and can still rot). Nettle ret input changes `nettlebundle-unprocessed`
  → `nettlebundle-cured` in `GetRettedOutput()`.

### B. Stook drying (new) — `BlockStook` + `BlockEntityStook`
New block placed on the ground (cone of standing sheaves; new shape
`shapes/block/tool/stook/`, reuse bundle textures), no crafting. Register both classes
in `RudimentsModSystem`. Two input modes detected from the held bundle:

- **Cure mode** (`*-unprocessed` green in): arid → `cureProgress` accrues; exposed rain
  → stalls/decays. On completion → `*-cured`. No quality involved.
- **Dry mode** (`*-retted` in): arid → `dryProgress` accrues (reuse the rack's
  `GetDryFactor` logic); on completion → `*-dried`, quality locked via
  [FiberQuality.Carry](Rudiments/src/common/Utils/FiberQuality.cs).
  **Exposed rain** → zero `dryProgress` **and** accumulate `rainExposureHours`; every
  JSON-tunable `rainTierHours` (e.g. 12h) drop quality one tier
  (`FiberQuality.Set` Fine→Standard→Coarse); below Coarse → convert to `game:rot`.
  This captures "rain stops/resets drying, restarts retting, can affect quality and
  rot" without recoupling to the full retting timeline.

The sheltered **rack stays unchanged** as the safe post-ret dryer.

### C. New `cured` bundle stage
- Add `cured` to the `type` variantgroup in
  [flaxbundle.json](Rudiments/assets/rudiments/itemtypes/resource/flaxbundle.json)
  and [nettlebundle.json](Rudiments/assets/rudiments/itemtypes/resource/nettlebundle.json)
  (shape/texture: reuse the `rippled`/unprocessed look). Add handbook + lang entries.
- Ripple input changes `flaxbundle-unprocessed` → `flaxbundle-cured` in
  [BlockRipple](Rudiments/src/common/block/) (and its recipe/handbook text).
- Harvest drops stay `*-unprocessed` (green) — `patches/crop-flax.json` / nettle drops
  unchanged.

### D. Routing + shared weather helper
- Extend [ItemFieldRettableBundle.cs](Rudiments/src/common/item/ItemFieldRettableBundle.cs)
  `OnHeldInteractStart` to route green→stook, rippled/cured→field-ret, retted→stook
  (add `GetCuredOutput`/`GetStookMode` helpers alongside `GetRettedOutput`).
- Factor the duplicated exposure/climate math (currently inline in
  `BlockEntityRettingBed.GetProgressRate` and `BlockEntityDryingRack.GetDryFactor`)
  into a small static helper (e.g. `Utils/FieldWeather.cs`: `IsExposedRaining`,
  `MoistureFactor`, `DryFactor`) reused by field-ret, stook, and rack.

### E. Assets / lang / version
- `lang/en.json`: rename `rettingbed-*` keys → `fieldretting-*`; add `stook-*`
  (empty/curing/drying/rain-warning), `cured` bundle names + handbook.
- Bump `modinfo.json` to 2.3.0; add CHANGELOG entry.

## Critical files
- `src/common/blockentity/BlockEntityRettingBase.cs`, `BlockEntityRettingBed.cs` (→FieldRetting), `BlockEntityDryingRack.cs`
- **New:** `src/common/block/BlockStook.cs`, `src/common/blockentity/BlockEntityStook.cs`, `src/common/Utils/FieldWeather.cs`
- `src/common/item/ItemFieldRettableBundle.cs`, `src/common/block/BlockRipple*.cs`
- `RudimentsModSystem.cs`
- `assets/rudiments/`: `blocktypes/tool/{fieldretting,stook}.json`, `itemtypes/resource/{flax,nettle}bundle.json`, `recipes/grid/rettingbed.json` (delete), `shapes/block/tool/{fieldretting,stook}/`, `lang/en.json`
- `modinfo.json`, `CHANGELOG.md`

## Verification
1. Build: `cd Rudiments && VINTAGE_STORY=/mnt/c/Users/danie/AppData/Roaming/Vintagestory PATH="/home/dwhite/.dotnet:$PATH" dotnet build -c Release` — expect 0 errors.
2. Package zip (Python `zipfile`, flat layout) → `builds/AgeOfFibers-v2.3.0-vs1.22.zip`.
3. Headless load-test (per build-env memory): seed `C:\temp\aof\Mods\`, run `VintagestoryServer.exe --dataPath 'C:\temp\aof'` (port 42000), grep `server-main.log` for errors/`ageoffibers` — confirms blocktypes/recipes/assemblies parse.
4. **In-game client run** (headless won't build atlases): verify field-ret laid-bundle and stook shapes render (no magenta), then walk the full chain — harvest→stook-cure→ripple→field-ret (watch arid stall vs rain progress)→stook-dry in rain (watch quality drop/rot) vs rack (safe)→break→scutch→hatchel.

## Notes / decisions
- **Curing is mandatory** (green `unprocessed` can't be rippled/retted until stook-cured) — faithful to the references and the "both moments" choice. Alternative considered: gate curing via a stack attribute instead of a new variant (less player-visible) — rejected for consistency with the variant-per-stage design.
- Stook rain penalty uses a simple cumulative `rainExposureHours`→tier-drop model rather than reconstructing the retting timeline, keeping `BlockEntityStook` decoupled from the ret state machine.
- Shapes for field-ret/stook can start as simple placeholders reusing bundle meshes; art polish is a follow-up (consistent with the mod's current placeholder-art posture).
