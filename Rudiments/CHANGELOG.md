# Changelog

All notable changes to **Rudiments** are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/). Versioning: [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`).

## Versioning policy

| Bump | When |
|---|---|
| **MAJOR** (`x.0.0`) | Removes/renames existing items or blocks in a way that orphans them in existing saves, or otherwise breaks backward compatibility. Comes with migration notes. |
| **MINOR** (`2.x.0`) | New blocks / items / mechanics, or a reworked mechanic that stays backward-compatible. Removing *recipes* (not the items they produce) counts here — note it under **Removed**. |
| **PATCH** (`2.1.x`) | Bug fixes, balance/tuning tweaks, text and art fixes. No new content or removed content. |

JSON-only tuning of existing `attributes` (e.g. retting timings) is a PATCH. A new block like the retting vat is a MINOR.

## Release checklist (run on every version bump)

1. Decide the bump (table above) and set `version` in `modinfo.json`.
2. Add a dated section to this file (newest on top) under Added / Changed / Removed / Fixed.
3. Build: `~/.dotnet/dotnet build -c Release` — must be **0 errors**.
4. Package `rudiments_X.Y.Z.zip` with `modinfo.json`, `modicon.png`, `Rudiments.dll`, and `assets/` at the zip root (use Python `zipfile`; `zip` is not installed).
5. Headless load-test the new zip (see workspace build-env memory): Windows `VintagestoryServer.exe` via WSL, port 42000, confirm `Dedicated Server now running` with 0 mod errors/warnings.
6. Leave the `modinfo.json` game dependency at `"1.22.0-rc.9"` unless raising the baseline (see note).

> **Game-dependency note:** `"game": "1.22.0-rc.9"` is intentional. A bare `"1.22.0"` fails semver pre-release comparison against RC builds; the `-rc.9` form still satisfies 1.22.x stable (load-verified on 1.22.3).

---

## [0.10.7] — 2026-07-07 — Hand cards first-person pitch mirrored upward

### Changed
- First-person hand cards were correctly facing forward (v0.10.5) but the paddle face pointed down toward the floor, pushing the visible geometry low in the view. Rolled the item 180° about its own forward axis (`fpHandTransform` rotation z: 0 → 180) — the board direction is unchanged (still forward, not backward), but the pad now faces up, lifting the cards higher and more visibly into frame. Confirmed as a low-priority polish tweak, not a functional issue.

## [0.10.6] — 2026-07-07 — Mod description refresh

### Changed
- `modinfo.json` description updated to mention the Wool & More, Immersive Fibercraft, Clayworks, and String Sense integrations alongside Toolsmith — it had only named Toolsmith since v0.7.0, underselling everything added since. README audited against the current `RudimentsConfig` and found already fully in sync (all 26 settings documented, no stale entries).

## [0.10.5] — 2026-07-07 — Hand cards first-person facing flip

### Fixed
- **Hand cards pointed backwards in first person too** — same inverted reference as the v0.10.4 third-person fix: handle pointed up into the view with the boards toward the camera. Applied the identical 180° pad-normal pre-rotation to `fpHandTransform` (solves to a clean `y: 180 → 0`); boards now extend away into the view with the handle at the hand.

## [0.10.4] — 2026-07-07 — Hand cards third-person facing flip

### Fixed
- **Hand cards pointed backwards in third person** — grip was correctly in the fist (v0.10.3), but the boards extended back toward the body with the handle sticking out. The tp rotation now includes a 180° pre-rotation about the item's pad-normal axis (solved numerically, grip and pad-up preserved), so the boards extend forward out of the fist.

## [0.10.3] — 2026-07-07 — Hand cards held transforms rebuilt from renderer math

### Fixed
- **Hand cards held transforms derived analytically instead of hand-guessed.** Playtest screenshots showed the pair hanging below the forearm in third person and rendering below the viewport in first person. Root cause found in `EntityShapeRenderer.RenderItem`: held items compose as `origin + scale·(R·(v−origin) + translation)` — the translation is *inside* the scale factor, and the shape's grip point was landing voxels away from the palm (v0.10.1 had the fist gripping the far board corner). Both hand transforms now set the transform origin at the lower handle's grip point with `translation = −origin/scale`, pinning the grip exactly to the hand attachment origin, and rotations were solved numerically against the vanilla knife's known-good bone-space frame (boards point where a knife blade points; pad face up). First person got the same treatment against the fp default frame.

## [0.10.2] — 2026-07-07 — Silence String Sense flax patch load error

### Fixed
- **Load error with String Sense installed**: String Sense's `cropdrops.json` replaces the vanilla flax stage-9 flaxfibers drop (array index 2) with flax strands, but Rudiments' crop-flax patch already replaced `dropsByType` with a two-entry stage-9 array, so their path missed and logged an `[Error]` every boot (harmless — strands are disabled by our compat anyway, so the end state was already correct). The patch loader deserializes all patch files before applying any, so this can't be fixed with a json patch; a new `StringSensePatchGuard` mod system (ExecuteOrder 0.04, just before the patch loader) rewrites their patch asset in memory, adding an inverted `rudiments` dependson so it skips as a clean unmet condition.

## [0.10.1] — 2026-07-07 — Hand cards third-person visual fixes

### Fixed
- **Hand cards no longer clip through the head in third person** — `tpHandTransform` scale reduced 0.75 → 0.45 (the pair rendered ~0.63 m long, double a real hand card), with the grip translation recomputed so the lower handle stays in the palm.
- **Carding animation reworked so the left hand reads as gripping the upper handle.** The item is rigidly attached to the right hand, so the old right-arm stroke swung the entire pair — including the lower card and fleece web that should sit still — away from the parked left hand. The right arm is now pinned as the anchor and the left arm strokes along the upper card's shape-alternate path (same 19-frame / 1.6 strokes-per-second cycle). Hand-to-handle registration may still need in-game fine-tuning.

## [0.10.0] — 2026-07-07 — String Sense compatibility

### Added
- **String Sense compat** (`stringsense-compat.json`, gated on `stringsense`). String Sense ships AgeOfFlax compat keyed to modid `ageofflax`, which never fires for Rudiments — leaving its flax strands orphaned (unobtainable once Rudiments replaces the flax crop drops) with dead recipes in the handbook, including strand→fibre shortcuts that would bypass retting if strands were obtainable. Mirroring its own AgeOfFlax pattern: the `flaxstrands` itemtype is disabled (which drops every recipe referencing it regardless of String Sense's ConfigLib toggles), its strand recipes are disabled for log hygiene, and **crude flax cord twists directly from 3 rippled bundles** — bridging Rudiments' chain into String Sense's crude cordage tier. The full chain remains the only route to twine and fine cord. The two mods are otherwise verified compatible: reed/rope/bow/wool touch points reviewed, no overlapping patch paths of consequence.

### Notes
- String Sense has no `cord-nettle` material; a nettle route into the crude cord tier would be an upstream PR to String Sense rather than a mislabeled recipe here.

## [0.9.3] — 2026-07-07 — Close the instant-retting bypass on third-party barrels

### Fixed
- **Clayworks' clay barrel no longer retts instantly.** The clay barrel reuses the vanilla barrel block/entity classes, so it consumed our retting barrel recipes but missed the `RettingBath` interceptor (patched only onto `game:blocktypes/wood/barrel`) — sealing completed the raw fallback recipe after 1 game hour with no countdown, quality window, or rot risk. New `clayworks-retting.json` compat patch (gated on `clayworks`) attaches the same `RettingBath`/`RettingBathInfo` behaviors to the clay barrel, making it a first-class retting bath. Note the clay barrel blocktype uses `behaviorsByType`, which overrides a plain `behaviors` property, so the block behavior merges into its `*` entry.
- **Defense-in-depth for unpatched barrel mods:** the fallback recipe `sealHours` raised from 1 → 36 (water) and 1 → 14 (lime). On patched barrels the interceptor takes over within a second so nothing changes; on any barrel mod we haven't patched, retting now at least costs a semi-realistic sealed duration instead of being a near-instant exploit (still without quality tiers — patching the behavior on is the real fix per mod).

## [0.9.2] — 2026-07-07 — Carding sound fix; custom two-hands carding animation

### Fixed
- **Carding sound no longer rings after the animation stops.** The scrape sample is ~5 s long and was fired per stroke (~3×/s) as fire-and-forget instances, so several tails kept playing for seconds after the ~2 s interaction. Now a single client-side `ILoadedSound` per player starts with the interaction and is stopped/disposed on stop or cancel (the same management the IF drop spindle uses).

### Added
- **Custom two-hands carding animation** (carding visuals phase 2, replacing the borrowed vanilla `squeezehoneycomb`): `patches/player-carding-anim.json` registers `rudimentscarding` (+ `-fp` twin for immersive first person) on the player entity and adds the keyframes to the seraph shape, following Immersive Fibercraft's `holdbothhandsspindle` pattern. Both hands come together at chest height; the right hand strokes on a 19-frame cycle that matches the item's 1.6 strokes/s shape-alternate cycling. Gated on the wool mod like all carding content.

## [0.9.1] — 2026-07-07 — Two-card carding visuals

### Changed
- **Hand cards now show both cards**, matching the real process (fleece brushed between a pair of carders). The idle item is the pair nested face-to-face, pins protected. During carding, the stroke poses show the lower card held steady with a web of fleece charged on its pins while the upper card sweeps across it; the final return pose shows a small rolag forming at the near edge. The bare idle shape is no longer shown mid-stroke. All baked into the held item's `renderVariant` shape alternates — no mechanics changes.

### Notes
- **Planned (carding animation, phase 2):** the item-shape trick reads well in first person, but in third person the whole two-card assembly rides on the right hand. The fix is a custom two-hands-together seraph animation — patch `game:entities/humanoid/player` the way Immersive Fibercraft adds `holdbothhandsspindle` — then reference it as `heldTpUseAnimation` in `handcards.json`, replacing the borrowed vanilla `squeezehoneycomb`. See the TODO(carding-anim) marker in `ItemHandCards.cs`.

## [0.9.0] — 2026-07-07 — Interactive carding; carding is now mandatory before spinning

### Changed
- **Carding is now an interactive held-item action** (like the Immersive Fibercraft drop spindle) instead of a crafting-grid recipe: hold washed wool fibers in the **off hand**, the hand cards in the active hand, and hold right mouse. ~2 s of brushing (animated card strokes, wool fluff particles, scrape sound) consumes 1 fiber, costs 1 cards durability, and yields 1 rolag. New `ItemHandCards` item class; fiber→rolag mapping is derived from item codes (`wool:fibers-X` → `rudiments:rolag-X`), so all 10 variants work without per-variant recipes.
- **Washed wool fibers can no longer be spun directly.** Immersive Fibercraft (`spinningwheel`) patches `spinningProps` onto `wool:fibers-*`, which let washed fibers bypass carding on the drop spindle and spinning wheel. Rudiments now strips that attribute in `AssetsFinalize` (runs after all JSON patching, so it is immune to mod load order) and adds `spinningProps` to rolags instead — the spindle/wheel spin 2 rolags → 1 wool twine, mirroring Immersive Fibercraft's raw-fiber ratio. Grid twine recipes already take 4 rolags (unchanged from 0.8.0).
- **Rolags have their own shape** — a fuzzy cylinder tinted per fleece color — instead of reusing Wool & More's fleece cloud shape, which made rolags and fleece indistinguishable.
- **Hand cards art pass** — replaced the flat placeholder texture with a 3D single carding brush (wooden paddle + handle, pinned leather pad) plus three stroke-pose alternate shapes used by the brushing animation.

### Removed
- Grid carding recipes (`recipes/grid/carding.json`) and their enable patches — superseded by the interactive action. The hand cards crafting recipe itself is unchanged.

## [0.8.0] — 2026-07-06 — Hand carding (Wool & More compatibility)

### Added
- **Hand cards** (`rudiments:handcards`) — a wire-toothed carding tool (2 planks + 1 leather + any metal nails & strips, 128 durability). Used as a grid tool: card 1 washed wool fiber into 1 **rolag** (1 durability per fiber). All 10 fiber type/color variants map to matching rolags (Mohair, Qiviut, 8 generic colors).
- **Wool rolags** (`rudiments:rolag-*`) — carded wool, groundstorable/shelvable, mirrors Wool & More's fiber variants.
- Compat patch `wool-carding.json` — everything ships disabled and is only enabled when the `wool` mod is loaded (same gating pattern as spinning compat); Wool & More's twine recipes are patched to take rolags instead of raw washed fibers, inserting carding between washing and twining. Zero footprint without the wool mod.

### Notes
- Planned next tier: a drum carder bench block for throughput (mirroring scutchboard → mechanical scutcher).
- Hand cards texture is a placeholder pending an art pass.

## [0.7.1] — 2026-07-06 — Ripple yield rebalance

### Changed
- **Ripple grain/seed yields drastically reduced** to sit below vanilla's mature-flax harvest (vanilla stage-9 flax: avg 3 grain + 1.2 seeds per plant). Rippling a cured bundle previously averaged 6/8/12 grain per bundle by tier — ~30 bundles yielded 3–4 stacks of grain, an effectively infinite food source. New per-bundle averages: primitive 1.5 grain + 0.6 seeds, simple 1.75 + 0.7, advanced 2.0 + 0.8 (tiers now differentiate mainly on throughput via `rippleAmount`, not yield).
- Grain is now rolled per bundle instead of one roll multiplied by batch size, so multi-bundle tiers no longer amplify a single lucky roll; zero-quantity drops are skipped.

### Added
- Config settings `RippleGrainYieldMultiplier` and `RippleSeedYieldMultiplier` (default 1.0) in `ModConfig/rudiments.json` — global multipliers on the per-tier JSON base yields; set to 0 to disable grain or seeds from rippling. Live-reloadable via `/rudimentsreload`.

## [0.7.0] — 2026-06-07 — Toolsmith compatibility replaces homegrown tool binding

### Removed
- **Tool Binding Methods** — the homegrown alternative tool-binding system (friction-fit /
  glue / nail / glue+nail, with durability multipliers, curing, and friction-fit failure
  mechanics; shipped in v0.5.0 and patched through v0.6.11) has been removed entirely. It
  conflicted both conceptually and technically with [Toolsmith](https://mods.vintagestory.at/toolsmith):
  both mods inject `CollectibleBehavior`s onto the same vanilla tools and override
  `GetMaxDurability`/`OnDamageItem`, which would collide if both were loaded. Existing
  bound tools simply lose the custom behaviour (durability multiplier, lashing texture)
  on load and continue functioning as plain tools — no save corruption.

### Added
- **Toolsmith binding interop** — `rudiments:finecord` is now registered as a premium
  binding material (`baseHPfactor` 1.6, between `leather` and `sturdy`) in Toolsmith's
  data-driven tool-tinkering system when that mod is present. Pure JSON data, no code, no
  hard dependency — a complete no-op without Toolsmith. Nettle-spun twine needs no
  separate registration since it produces vanilla `flaxtwine`, already supported natively.

---

## [0.3.0] — 2026-06-02 — Unique tool graphics, build pipeline

### Added
- **GitHub Actions workflow** packages `rudiments_X.Y.Z.zip` and `rudimentsspinningcompat_X.Y.Z.zip` on every push to main and on `v*` tags.
- **Pre-commit hook** (`.githooks/pre-commit`) compiles `Rudiments.dll` via `dotnet build` and stages it automatically before each commit. Activate once with `git config core.hooksPath .githooks`.

### Fixed
- **Ripple** now has custom Pillow-generated textures (dark oak tones, distinct per tier) rather than borrowed vanilla wood/metal block textures shared with the hatchel.
- **Scutch board** has a new purpose-built shape (flat vertical board on log base) and custom pine-toned textures — no longer shares the hatchel model.
- **`flaxbundle-cured`** and **`nettlebundle-cured`** textures were identical to their `-unprocessed` counterparts. Cured now shows golden/dried stalk colouring.
- `BlockEntityDryingRack.inventory` field shadowing base class member; added `new` keyword to silence CS0108.

### Changed
- Stale `ageoffibers` / `aofspinningcompat` modid references removed from README files and changelog.
- `Rudiments.csproj` now uses the standard `$(VINTAGE_STORY)` env var for DLL hint paths (was a hardcoded Windows path).

---

## [2.4.0] — 2026-06-01 — Lime retting, nettle cooking, fibre QoL

### Added
- **Lime retting.** Right-click the retting vat with `game:quicklime` to load a lime modifier (second slot). One unit is consumed when a batch starts. Lime makes retting **2.5× faster** but caps quality at Standard (fine fibre is never produced) and tightens the rot window (`StandardHold` halved). No-lime behaviour is unchanged.
- **Nettle leaves are now cookable.** `nettleleaves` gained `nutritionPropsWhenInMeal` and was added to the vanilla `soup` and `vegetablestew` cooking recipes via patch. Cooking removes the raw **-0.5 HP** sting penalty and yields a wholesome Vegetable meal. Eating leaves raw still carries the penalty.

### Changed
- **Coarse fibres** handbook entry now carries a red-text warning that they can only be twisted into rope (cannot substitute for standard/fine fibre in twine, cloth, gambeson, or bowstring recipes). Audit confirmed no accidental wildcard/tag acceptance in vanilla flax recipes.
- **Fine gambeson handbook** — the previously promised "breathability" perk has been retired with no mechanical change. VS has no overheating/cooling mechanic (`warmth` only helps in cold), so neither a positive nor negative warmth value would represent breathability; linen stays thermally neutral like other armor. Handbook text now claims only the durability bonus. See `docs/breathability-research.md`.

### Notes
- The optional **Spinning Wheel compat** is shipped as a *separate* mod (`RudimentsSpinningCompat`, modid `rudimentsspinningcompat`), not part of this zip. It patches `rudiments:nettlefiber` to be spinnable into `game:flaxtwine` on the Immersive Fibercraft spinning wheel, guarded by `dependsOn` so it no-ops unless both mods are present.

## [2.3.0] — 2026-05-31 — Field retting rework + stook drying

Field/dew retting is no longer a crafted trough — bundles are laid on the ground and weathered, and the flax/nettle chains gain the two real drying moments (curing before retting, drying after) modelled through one **ambient-moisture** mechanic: wet advances retting and resets drying, arid advances drying and stalls retting.

### Added
- **`cured` bundle stage** for both flax and nettle. Freshly-harvested green bundles (`*-unprocessed`) must now be **stook-cured** before rippling (flax) or retting (nettle). New item variants, textures (placeholder), lang and handbook entries. Flax is now a 7-step chain, nettle a 6-step chain.
- **Stook** (`BlockStook` / `BlockEntityStook`) — a ground-placed block (no crafting) with two modes auto-detected from the bundle: **cure mode** (green `*-unprocessed` → `*-cured` in arid weather; rain harmlessly stalls) and **dry mode** (`*-retted` → `*-dried` in arid weather, quality preserved). Drying outdoors is **risky**: rain resets drying progress and accumulates exposure — every `rainTierHours` of rain drops fibre quality one tier (Fine→Standard→Coarse), and below Coarse the bundle rots. Tunables in blocktype JSON (`cureHours`, `dryHours`, `rainTierHours`, `dryStallRainfall`).
- **`FieldWeather`** shared static helper (`IsExposedRaining`, `DryFactor`) — de-duplicates the climate/exposure math previously copied between field retting and the drying rack.

### Changed
- **Field retting**: the crafted `rettingbed` trough is replaced by `fieldretting` — bundles laid flat on grass (new placeholder shape, no `wood`/`water` trough look). Right-clicking bare ground routes by bundle state: green → stook (cure), `flaxbundle-rippled`/`nettlebundle-cured` → field-ret, `*-retted` → stook (dry). Classes renamed `BlockRettingBed`/`BlockEntityRettingBed` → `BlockFieldRetting`/`BlockEntityFieldRetting`.
- **Arid weather now stalls field retting** (rainfall below `dryStallRainfall`, default 0.05 → zero progress) instead of creeping forward via the old moisture floor — a drought "just dries without retting", no quality loss.
- **Nettle ret input** is now `nettlebundle-cured` (was `nettlebundle-unprocessed`); **ripple input** is now `flaxbundle-cured` (was `flaxbundle-unprocessed`).
- The sheltered **drying rack** is unchanged — it remains the safe, quality-preserving post-ret dryer (refactored onto `FieldWeather` with identical behaviour).

### Removed
- Crafted retting-bed recipe (`recipes/grid/rettingbed.json`) and the `rettingbed` blocktype/shape. Field retting needs no crafting.

## [2.2.1] — 2026-05-30 — Texture overhaul (Pillow pixel art)

### Fixed
- **All nine nettle crop stage sprites** redrawn. Stages 7–9 were rendering as pine-tree artifacts from a bad AI downsample. All stages now show a proper nettle plant (stem + opposite leaf pairs, seed/flower clusters on stages 7–9) at the correct height for each growth tier.
- **Nettle bundle textures** (unprocessed/retted/dried/broken) replaced — previous versions were identical grey-green blobs with no visual distinction between stages. Now show distinct stalk-stripe material textures: deep green (unprocessed), olive-brown (retted), pale khaki (dried), frayed straw (broken).
- **Linseed oil**: was a dark green plant shape — now a ceramic vase with amber oil fill.
- **Linseed cake**: was a tree/mushroom — now a flat pressed disc with cross-hatch top.
- **Fine cord**: was a grey smudge — now a diagonal twisted cord.
- **Fine fibres**: was a cluttered brown block — now neat parallel strands with a centre binding.
- **Coarse fibres**: improved from blob to recognisable tangled fibre clump.
- **Nettle leaves**: was scattered noise pixels — now a serrated leaf cluster.
- **Nettle rhizome**: was nearly invisible — now a pale knobbly root with node bumps.
- **Nettlestub**: was nearly invisible — now a soil-tile block texture with green stem stubs.

All item/block/crop textures redrawn via `scripts/fix_af_textures.py` (Pillow). No code changes; no asset migration needed.
- **Modicon**: reverted from broken AI-generated scatter to the clean Pillow diagonal-split design (`modicon_preview.png` → `modicon.png`).

## [2.2.0] — 2026-05-30 — Scutching step + field retting overhaul

### Added
- **Scutch board** (`scutchboard-primitive/simple/advanced`) — new craftable processing block that converts broken bundles into scutched bundles by scraping off woody shives. Completes the historically accurate manual chain: break → scutch → hatchel. Primitive is stick + flint + axe; simple/advanced use planks + copper/iron nails.
- `scutched` bundle variant for both `flaxbundle` and `nettlebundle`.

### Changed
- **Full processing chains now:**
  - Flax (6 steps): ripple → ret → dry → **break → scutch → hatchel** → fiber
  - Nettle (5 steps): ret → dry → **break → scutch → hatchel** → fiber
- **Field/dew retting no longer requires crafting a retting bed.** Right-click bare ground with rippled flax or fresh nettle bundles and a retting bed is auto-placed at that spot. All quality/weather tracking is unchanged.
- **Hatchel** now accepts `scutched` bundles (was `broken`). Existing `broken` bundles from previous saves should be scutched on a scutch board first.
- **Mechanical Scutch Mill** now outputs `scutched` bundles (was `broken`) — correctly represents the combined break + scutch mechanical operation.

### Removed
- Crafting recipe for the retting bed (block still exists and is auto-placed by the field-retting mechanic).

## [2.1.1] — 2026-05-30 — AI art pass

### Changed
- Replaced Pillow-generated placeholder sprites with SDXL + pixel-art LoRA textures for all new item and crop assets. Flax textures (OppoOtis originals) unchanged.

## [2.1.0] — 2026-05-30 — Quality retting rework

### Added
- **Retting vat** — new craftable water-retting block (`rettingvat`, vanilla `block/wood/barrel/closed` shape). Fast, steady retting; replaces the old static barrel recipes. Crafted from 7 planks.
- **`BlockEntityRettingBase`** — abstract base implementing a shared 4-stage quality state machine: under-retted → **Coarse** → **Fine** (brief RNG window) → **Standard** → **Rot**. A single RNG roll at conversion decides if/when the Fine window opens; the rest is deterministic and persisted. All thresholds + RNG chances are exposed as tunable blocktype JSON `attributes`.
- Stage-aware block-info text on both retting blocks so players can catch the Fine window.

### Changed
- **Fibre quality is now time/attention-driven for both retting methods**, replacing the old method-driven model (water = standard, field = fine). Catch bundles at the Fine window for the best fibre; leave them too long and they rot.
- **Retting bed** reworked onto the shared state machine (keeps its weather-driven progress rate; slower than the vat). Tuned defaults: minRet 72h, fine 0.7 @ +24–72h for 36h, standard +96h, rot +168h.
- Retting vat tuned defaults: minRet 18h, fine 0.6 @ +6–18h for 12h, standard +24h, rot +36h.
- Handbook entries, item descriptions, and craft-info text rewritten to describe the timing-based quality model.

### Removed
- Static barrel retting recipes `rettedflax.json` and `rettednettle.json` (superseded by the retting vat). Existing retted bundles in saves are unaffected.

## [2.0.0] — 2026-05-30 — Initial release

Full from-scratch expansion of OppoOtis's AgeOfFlax into a two-fibre (flax + stinging nettle) production system:
- Flax chain: ripple → ret → dry → break → hatchel, with a `fiberquality` stack attribute (coarse/standard/fine).
- Stinging nettle: fast-growing crop with wild worldgen spawn, stage-based drops (edible leaves vs fibre bundles), careful slow-harvest, bare-hand/tool stinging, rhizome digging/planting and wild spreading.
- Weather-aware drying rack, linseed oil press, and an axle-driven mechanical scutch mill.
- Fine-fibre payoff: fine cord giving durability bonuses on bows and linen gambeson armour.
