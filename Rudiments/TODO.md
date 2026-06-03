# Rudiments — TODO

Delivered work lives in [CHANGELOG.md](CHANGELOG.md). This file tracks only pending,
in-progress, and future ideas.

---

## Pending — v2.3.x (before next public release)

_Nothing currently blocking release. The oil press shape (previously listed here) is
already a dedicated vertical-press model in `shapes/block/tool/oilpress/oilpress.json` —
uprights, crossbar, central screw, pressing plate, handle — wired up via the blocktype's
`shape.base`. Only an in-game visual confirm remains; see the deferred playtest list._

---

## Done — shipped in v2.4.0 (needs in-game verification, see deferred list)

### Lime (chemical) retting — DONE
Implemented as a quicklime modifier on the existing vat (no new block). Quicklime in a
second slot → 2.5× rate, `FineChance` forced to 0, `StandardHold` halved (tighter rot
window). 1 unit consumed per batch. Files: `BlockEntityRettingBase.cs`,
`BlockEntityRettingVat.cs`, `rettingvat` lang/blockdesc.
(Note: the old TODO claimed a design existed in `docs/rettrefactor.md` — it never did;
implemented from the brief.)

### Nettle soup / tea — DONE
`nettleleaves` given `nutritionPropsWhenInMeal` (Vegetable, no health field) and patched
into the vanilla `soup` + `vegetablestew` cooking recipes (`patches/cooking-nettle.json`).
Cooking removes the raw -0.5 HP penalty; raw eating still carries it. No new textures
(reuses vanilla meal-bowl rendering). Tea not added — soup/stew covers the goal.

### Coarse fibre restriction enforcement — DONE
- ✅ Red-text warning added to the `coarsefibers` handbook entry (`lang/en.json`,
  `item-handbooktext-coarsefibers`).
- ✅ Wildcard audit cleared: `coarsefibers` defines no `tags`, and no mod recipe
  references `game:flaxfibers`. A vanilla `game:flaxfibers` ingredient cannot match
  `rudiments:coarsefibers` (different domain + code, no shared tag), so there is no
  accidental-acceptance exposure. Only `coarsefibers-rope.json` consumes it.

### "Breathability" armour bonus — RESOLVED (perk dropped, no mechanical change)
Researched: VS has **no overheating/cooling mechanic** — `warmth` (°C, via
`EntityBehaviorBodyTemperature`) only ever helps in the cold; there is nothing for a
"breathability" perk to mitigate. Full findings in `docs/breathability-research.md`.
A positive warmth would make linen a *better cold* armor (backwards); a negative warmth
would just penalize cold survival for no hot-climate gain. So **no warmth modifier was
shipped** — linen stays thermally neutral, matching actual game behavior. Only fix: the
`finecord` handbook text was rewritten to drop the over-promise. No further work unless a
body-temperature overhaul mod is targeted.

### Spinningwheel compat addon (Option C) — DONE (separate mod)
Built as a standalone content mod at `RudimentsSpinningCompat`
(modid `aofspinningcompat`). Patches `rudiments:nettlefiber` with `spinningProps`
(Immersive Fibercraft / modid `spinningwheel` format) → `game:flaxtwine` 2:1. Guarded by
`dependsOn: [ageoffibers, spinningwheel]` so it silently no-ops unless both are installed.
Known limit: applies to any nettle-fibre quality tier (spinningProps is type-level, quality
is per-stack) — quality gating would need a C# hook. Ship as its own zip.

# Deferred Indefinately

### In-game client playtest of v2.3.0 rettrefactor
The headless load-test passed but no client-side playthrough has been done yet.
Walk the complete chain manually and verify:

- [ ] Stook **cure mode**: place fresh unprocessed bundles, wait, confirm →cured in arid
- [ ] Stook **rain stall**: cure should pause during rain, no quality loss
- [ ] Stook **dry mode**: place retted bundles, confirm →dried; quality preserved in arid
- [ ] Stook **rain quality drop**: rain should reset dryProgress and drop quality tier every `rainTierHours`; below Coarse → rot
- [ ] Field retting **arid stall**: drought should hold progress at 0%, no quality loss
- [ ] Field retting **rain advance**: rain/moisture should advance retting normally
- [ ] `flaxbundle-cured` falls through to GroundStorable (NOT stook/fieldretting — must be rippled first)
- [ ] `nettlebundle-cured` right-click bare ground → fieldretting (not stook)
- [ ] Full 7-step flax chain end-to-end
- [ ] Full 6-step nettle chain end-to-end
- [ ] Handbook text: step numbering correct, hrefs work (`fieldretting-north`, not `rettingbed-north`)
- [ ] Mechscutcher: renders correctly, processes dried bundles when axle-driven, block below no longer disappears

### In-game verification of v2.4.0 features (built + headless-clean, no client playthrough yet)

- [ ] Lime retting: right-click vat with quicklime loads it (info text shows count); a batch consumes 1 unit; retting is ~2.5× faster, never reaches fine, and rots sooner after Standard
- [ ] Lime: no-lime vat behaviour unchanged; lime slot survives save/load (1-slot → 2-slot inventory migration); lime can be retrieved before a batch starts
- [ ] Nettle cooking: nettleleaves appears as a valid ingredient in the pot for soup & vegetable stew (confirms the `survival:recipes/cooking/*` patch path resolves)
- [ ] Nettle cooking: cooked soup/stew tooltip shows Vegetable satiety with **no** -0.5 HP line; eating a raw leaf still deals -0.5 HP
- [ ] Spinningwheel compat (separate mod, both mods loaded): nettle fibre spins to flax twine 2:1; with spinningwheel absent the addon loads with no errors and no effect

### Placeholder art — needs real textures (see `docs/texture-pipeline.md`)

| Asset | Current state | Priority |
|---|---|---|
| Crop stages `normal1–9.png` | Pillow placeholders | **Highest** — world-visible every time nettle grows |
| `item/resource/flax/cured.png` | Copy of `unprocessed.png` | High — seen every flax run |
| `item/resource/nettle/cured.png` | Copy of `unprocessed.png` | High |
| `block/tool/stook/stook.json` shape | Simple box geometry | Medium — new block, no real silhouette |
| `block/tool/fieldretting/fieldretting.json` shape | Flat pile placeholder | Medium |
| Modicon `modicon.png` | Pillow diagonal-split | Low — ModDB first impression |

Run the ComfyUI pipeline (`/home/dwhite/comfyui/generate_aof_sprites.py`) for crop stages
and item icons. Stook/fieldretting shapes need JSON geometry work, not AI generation.
See `docs/texture-pipeline.md` for the full workflow.
