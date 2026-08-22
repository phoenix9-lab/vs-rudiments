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

## [2.0.1] — 2026-08-22 — Fixed a fruit-press crash on pressing flax grain

### Fixed
- **Pressing flax grain in the vanilla fruit press crashed the game** with a `NullReferenceException`
  in `BlockEntityFruitPress.InteractMashContainer`. Every vanilla juicing chain (apple, olive) presses
  down into a mash item that itself carries `juiceableProperties` — the fruit press unconditionally
  reads `PressedDryRatio` off whatever lands in the mash slot. `rudiments:linseedcake` had none, so
  that lookup returned null the moment the first grain went in. Fixed by giving `linseedcake` an
  empty `juiceableProperties: {}` — `PressedDryRatio` defaults to `1.0`, and it stays correctly
  non-juiceable (no `litresPerItem`, so a further press attempt on the cake itself is still refused).

## [2.0.0] — 2026-08-22 — Custom oil press retired; fruit press now takes flax grain

### Fixed
- **The fruit press juicing patch targeted the wrong item.** `game:seeds-flax` is the plantable
  seed packet, not the harvested milling grain — pressing it for oil would have let players convert
  their planting stock into oil, and never touched `game:grain-flax` (the item rippling actually
  yields for this purpose). The patch and its config (`LinseedOilLitresPerGrain`, was
  `LinseedOilLitresPerSeed`) now target `game:grain-flax`.

### Removed
- **`rudiments:oilpress` and its class, recipe, and shape are gone.** The vanilla fruit press
  (fixed in 1.1.0 to actually accept the right item) fully replaces it — no reason to keep a
  redundant custom block once the vanilla route works.

> **Migration note:** any `rudiments:oilpress` blocks or items in existing saves (placed presses,
> held/stored press items) become unknown — this is why the bump is MAJOR. Any `rudiments:linseedcake`
> or oil already produced is unaffected; only the press block itself is gone.

## [1.1.0] — 2026-08-22 — Flax seeds are now pressable in the vanilla fruit press

### Added
- **Flax seeds can now be pressed into `game:oilportion-flax` in the vanilla fruit press**, via a
  `juiceablePropertiesByType` patch on `game:itemtypes/resource/seeds` — the same generic mechanism
  vanilla uses for apples and olives. No custom block or C# interaction involved; the pressed
  byproduct is `rudiments:linseedcake`, same as the dedicated oil press produces.
- New config: `LinseedOilLitresPerSeed` (default `0.03`) — litres of oil extracted per seed,
  applied by editing the loaded item's `juiceableProperties` attribute in `AssetsFinalize` (same
  pattern as `BarrelRettingLitresPerBundle`). The pressed-cake byproduct has no chance/quantity
  field to tune — the fruit press always produces it.

The dedicated `rudiments:oilpress` block is unchanged and still works; it's expected to be retired
once the fruit press route is confirmed working in practice (see README's Equipment table).

## [1.0.0] — 2026-08-22 — Linseed oil now uses the vanilla item

### Changed
- **`rudiments:linseedoil` is gone; the seed oil press now dispenses vanilla `game:oilportion-flax`
  instead.** There was no reason to keep a separate item duplicating vanilla's flax oil texture and
  liquid-portion mechanics. The press's yield per stroke is rescaled from ~2 to ~10 units (oilportion
  is 100 units/litre) to land in the same ballpark as vanilla's own linseed cooking recipe (3 flax
  flour + water → 10 units).

### Removed
- `rudiments:linseedoil` and its handbook entry. It was a solid item (edible, combustible as lamp
  fuel); `game:oilportion-flax` is a liquid portion instead, so it is not directly edible or
  burnable as an item — it works through vanilla's cooking and oil-lamp mechanics.

> **Migration note:** any `rudiments:linseedoil` stacks in existing saves (inventories, chests, ground
> items) become unknown/missing items — this is why the bump is MAJOR. `rudiments:linseedcake` is
> unaffected.

## [0.24.1] — 2026-08-21 — Scutching sword's leaned pose fixed

### Fixed
- **The scutching sword's ground-storage pose, added in 0.24.0, didn't work.** It first stood bolt
  upright at the tile center instead of leaning, and an attempt to fix that by reusing the vanilla
  spear's `groundStorageTransform` made it disappear entirely — the spear's shape is a small composite
  fragment on a shared armature with a different axis convention than our sword's plain vertical
  shape, so its rotation values didn't carry over. Replaced with a transform built for this shape's
  own axes: it now actually leans.

## [0.24.0] — 2026-08-21 — Leanable scutching sword, and kiln brick capacity matched to vanilla

### Added
- **The scutching sword can now be placed leaning against a wall**, like other tools — right-click to
  set it down, sneak-right-click to pick it back up.

### Fixed
- **Both kilns capped raw brick loads at far below vanilla.** Ware capacity treated every item as a
  flat 1-piece cost, so a small kiln held at most 4 bricks and an updraft kiln 8 — regardless of what
  the item's own ground-storage stacking allowed. Capacity is now read off each item's real
  `GroundStorable` layout, the same way vanilla's pit kiln does, so a full 24-brick pile fits a small
  kiln and 48 fits an updraft kiln.

## [0.23.0] — 2026-08-20 — Porcelain clayforming texture fix, and a Clayworks wet-stage compat patch

### Fixed
- **Porcelain showed the atlas placeholder ("?") for the entire clayforming stage.** The clay-forming
  voxel view reads its texture from the `clayform` workbench block's own `textures` dict (keyed by the
  work item's short code), not from the item's own texture definitions — vanilla only ever registers
  `clayworkitem-blue/fire/red` there. The icon, the finished raw item and the fired item all resolved
  fine through unrelated texture paths, so only the in-progress forming view was affected.

### Added
- **Clayworks compatibility: porcelain now gets the same wet-then-dry greenware stage as every other
  colour.** Clayworks redirects vanilla clayforming recipes through a `*-wet` block that must sit ~8
  hours before drying into raw ware, but porcelain ships its own clayforming recipes rather than
  reusing the vanilla ones, so it always skipped straight to raw. It now goes through an equivalent
  `rudiments:*-porcelain-wet` stage — only when Clayworks is installed; without it, nothing changes.

## [0.22.3] — 2026-08-19 — Handbook rewrite: plainer pages, and pottery wear stated the right way round

### Fixed
- **The handling page had the ware tiers backwards.** It listed earthenware at 0.1%, stoneware at
  0.5% and porcelain at 1.5% per-use shatter, and told players porcelain was the most fragile of the
  three. The defaults are the other way round — earthenware 1.5% (~46 uses), stoneware 0.5% (~138),
  porcelain 0, no wear from ordinary use at all. Corrected on the handling page, on the ware page's
  closing line, and in the glaze page's "what a glaze does not do" paragraph, which called
  lead-glazed earthenware the toughest tier to handle.
- **The kilns page still said cold-fired porcelain came out earthenware.** It comes out as shards —
  `BlockEntityUpdraftKiln.FireSlot` only grants the survival roll on a hot firing and otherwise falls
  through to the base class. The porcelain page already said so; the two now agree.

### Changed
- **All nine pottery handbook pages rewritten to plain language.** Ware, handling, water, porcelain,
  kilns, glaze, lead poisoning, and the two extra sections (fire clay, salt glazing). Cut the
  chemistry and the history throughout — sodium-alumino-silicate vapour, alumina and silica
  percentages, firing temperatures in °C, Rhenish and Staffordshire potters, Spode's bone-china
  formula, 9th-century Baghdad and Delft, saggars and bottle ovens — and cut the word "gate" and its
  variants from the lang file entirely. What is left is what a player needs to use the thing and what
  it does to them. Total handbook text roughly halved, from ~19,400 to ~11,700 characters.
- **Two mechanics stated that were not before.** Wear applies only to using a vessel — carrying one,
  or opening a container you have placed, is free (`BreakageIncludesPlacedContainers` is off by
  default). And drop breakage does not vary by tier, which is worth saying now that wear does.

### Notes
- Text only: no code, config or asset changes, so no behaviour changes with this version.
- The fibre and nettle handbook entries were left alone — already step-by-step and free of both the
  history and the "gate" language.

## [0.21.0] — 2026-08-10 — Fuel decides the ware, not just the gate

The two custom kilns treated fuel as a binary pass/fail switch and a spare stack rather than a real
cost. Three fixes from the same playtest pass.

### Changed
- **Cold fuel is no longer refused.** Wood, peat and lignite used to be rejected at the kiln's mouth
  outright. They are accepted now, and a kiln lit on them still fires — just cooler.
- **What comes out follows the fuel.** A firing lit on charcoal, coke, bituminous or anthracite coal
  still comes out stoneware, exactly as before. Lit on wood, peat or lignite it comes out earthenware
  instead. The updraft kiln's porcelain roll only ever happens on a hot firing — porcelain fired cold
  cannot vitrify and falls back to shards, the same outcome it always had before that roll existed.
- **A kiln's fuel slot now holds exactly one firing's worth.** Small kiln 4, updraft kiln 8 — double,
  matching its double ware capacity — and the whole slot is spent at ignition. Previously the slot
  took a full stack of up to 64 and a firing burned exactly 1 off the top, so a single load quietly
  lasted dozens of firings unnoticed.

- **Salt no longer glazes a cold firing.** Salt vaporises and reacts with the clay body only at
  stoneware heat, so a load lit on wood, peat or lignite now leaves a loaded salt slot untouched
  rather than stamping a "salt-glazed earthenware" that the game's own lore says can't exist.

### Fixed
- **The updraft kiln, and its chimney, rendered see-through.** Root cause was a texture, not a missing
  one: both reuse a vanilla shape re-skinned with `block/clay/brick/eight/running/red1` as the base
  wall texture, but that PNG is authored as a colour-tint **overlay** with a partially transparent
  alpha channel — every vanilla use of it composites it over an opaque `cream1` base
  (`blocktypes/clay/brickcourse.json`'s own convention) rather than using it bare. Both blocktypes now
  do the same. (`inside` was also missing from both and has been declared too, matching the small
  brick kiln's own shape — real gap, but not what was making the kiln see-through.)
- **Three handbook pages still promised guaranteed vitrification.** Water, porcelain and lead all told
  players a brick/updraft kiln firing vitrifies the body outright; now that cold fuel is accepted,
  that's only true when it's lit on charcoal, coke or coal. Reworded to say so, including that a
  cold-fired porcelain load is a total loss rather than the usual ~30%.

### Notes
- New config: `SmallBrickKilnFuelPerFiring` (4), `UpdraftKilnFuelPerFiring` (8) — also the hard cap on
  each kiln's fuel slot. `KilnMinFuelTemperature`'s doc comment now describes what it actually gates:
  the stoneware/earthenware split, not fuel acceptance.
- Compiles clean; the fuel amounts and the earthenware-from-cold-fuel path are new enough to want a
  playtest before calling the balance settled.

---

## [0.20.0] — 2026-08-10 — Turning lead poisoning off, properly

Every number in the mod was already a config value and read live. Two things about the off switch
were not good enough.

### Added
- **`/rudimentslead`** — reports your burden, the threshold nothing happens below, how much max health
  it is currently costing, and how fast it is draining. There is no GUI for lead and there is not
  going to be one, so this is the only way to see the number behind the messages.
- **`/rudimentslead clear [player]`** — wipes a burden. Requires `controlserver`. The way back for a
  character who accumulated one under a setting the server has since changed its mind about; without
  it, switching the feature off and on again re-applies the old penalty in full.

### Fixed
- **A client could turn the warnings off and still be poisoned.** Config is loaded per side from the
  local `ModConfig/rudiments.json`, which is harmless for every other setting in this mod but not for
  this one: the burden is server-authoritative and the warnings are client-side, so a client with
  `LeadPoisoningEnabled: false` in their own file stopped being warned and carried on accumulating.
  The server now mirrors its answer into the world config, which is synced, and every check reads
  that in preference to the local file. `/rudimentsreload` re-mirrors it, so the switch is live.

### Notes
- Existing configs gain the six `Lead*` keys at their defaults the next time the game writes the file,
  which happens on the first launch after updating. Nothing needs deleting.
- With lead poisoning off, the four vessel classes stay installed. They are also what stops a pot,
  crock or bowl losing its ware tier when it cooks, empties, serves or is eaten from, and that is not
  a lead feature.

---

## [0.19.0] — 2026-08-10 — The lead travels with the food

0.18.0 tied lead exposure to the vessel in your hand at the moment you ate. This replaces that: the
contamination now rides the food itself, so nothing is laundered by changing plates. 0.18.0 was never
played, so nothing here is a migration — it is the same feature with the right model under it.

### Changed
- **Lead follows the food, not the pot.** A lead-glazed vessel contaminates whatever it is holding,
  and the contamination travels with it. Cook a stew in a leaded pot, decant it into a clean crock,
  serve it into a spotless porcelain bowl — it is still leaded and it will still poison you. Same for
  water: fill a leaded jug, pour it into a clean cup, and the cup is leaded. Carried by one boolean
  stack attribute, `rudimentslead`, stamped at each hand-off.
- **Cooking counts, storing counts, serving counts.** Pots, crocks, bowls and jugs, exactly as asked.
  Ware nothing edible passes through — flowerpots, oil lamps, watering cans, storage vessels — is
  still harmless and still shows no warning.
- **A clean vessel holding leaded food says so on its tooltip.** There would otherwise be no way to
  tell, which would make the whole thing feel like a bug rather than a consequence.
- **Two carriers, because food is stored two ways.** Meals mark the vessel stack (a meal has no single
  contents object, and marking the ingredient stacks would stop them merging); liquids mark the
  portion, which is the thing that actually moves, so it propagates through buckets and barrels with
  no code at all.
- `rudimentslead` is registered in `GlobalConstants.IgnoredStackAttributes` at startup so it can never
  refuse a merge, a liquid top-up or a recipe match. The visible consequence is dilution: tipping a
  leaded jug into a full clean barrel loses the lead rather than spoiling the barrel, and the reverse
  keeps it. That is roughly what happens, and much kinder than a silent "these two waters will not
  combine".
- Handbook **Lead poisoning** rewritten around provenance, including the dilution rule.

### Fixed
- **A clay pot lost its ware tier and glaze every time it cooked.** `BlockCookingContainer.DoSmelt`
  builds the cooked pot with `new ItemStack(CodeWithVariant("type", "cooked"), 1)` and never looks at
  the raw pot again, so a stoneware pot cooked itself back down to earthenware. Fourth instance of the
  same vanilla pattern.
- **A pot or crock lost its ware tier and glaze when it gave up its last serving.**
  `SetServingsMaybeEmpty` replaces it with its `emptiedBlockCode`. Third instance.
- The serve wrappers added in 0.18.0 only snapshotted the receiving bowl; they now snapshot both sides,
  and the restore triggers on "the collectible under this slot changed" rather than on a guess about
  what it changed into.

---

## [0.18.0] — 2026-08-10 — Lead poisoning, and tin glaze

The rest of Parcel 4, plus the thing that makes it mean something. Lead glaze was the cheapest seal
in the game with no downside at all, which made tin and salt decoration. Now it has a price, and the
other two have a reason to exist.

### Added
- **Lead poisoning.** Eating or drinking from a lead-glazed vessel builds a body burden. The burden
  decays on its own every in-game day, online or off, and below a grace threshold the two balance and
  nothing happens — five helpings a day off leaded ware is free. Above it you start losing **maximum
  health**, up to a third of it, and losing max health at full health takes current health with it.
  There is no antidote and dying does not clear it: the only cure is time away from the stuff. You are
  told in chat when it starts, worsens, eases and clears. Built on
  `EntityBehaviorHealth.SetMaxHealthModifiers`, so it is keyed, cumulative and reversible — no
  Harmony, no reflection, no invented API.
- **Exposure is the vessel in your hand when you consume**, and nothing else. A lead-glazed bowl you
  eat a meal from, or a bowl or jug you drink from. Cooking in a leaded pot and eating from a clean
  bowl does not count; neither does a leaded flowerpot, oil lamp, watering can or storage vessel.
  A provenance chain would be invisible and unactionable, where "do not eat off the lead" is a rule a
  player can follow.
- **Warned from the first firing, and before it.** The galena nugget says what its glaze costs, raw
  dusted greenware says it again, and fired ware you can consume from says it a third time — right
  under the line that says it is sealed. Ware that cannot poison you never shows the warning.
- **Tin glaze.** A cassiterite nugget over a lead-dusted vessel: two nuggets and a rarer ore against
  lead's one, and it does not poison anyone. It goes *over* lead rather than onto bare clay because
  that is what tin glaze is — a lead glaze opacified with tin oxide, invented in 9th-century Baghdad
  to imitate Chinese porcelain and reinvented at Delft from about 1580. (Real tin-glazed ware still
  leached lead, Delftware included. Treating the tin layer as the barrier it was believed to be is a
  deliberate simplification, stated as one in the handbook: a glaze that is merely somewhat less
  poisonous is not a decision a player can act on.)
- **Handbook: Lead poisoning** (`rudiments-lead`), and the Glaze page rewritten around three glazes
  with three different costs rather than one glaze and an also-ran.
- Config section **Lead poisoning** — `LeadPoisoningEnabled`, `LeadPerServing`, `LeadDecayPerDay`,
  `LeadOnsetBurden`, `LeadBurdenPerHealthPoint`, `LeadMaxHealthPenalty`. All read live.

### Fixed
- **Per-use fragility never once fired when eating a meal from a bowl.** `BlockMeal.OnHeldInteractStop`
  overrides without calling base, and base is what forwards to `CollectibleBehaviors` — so
  `rudiments:Fragile` has been silently skipped on that path since 0.14.0. It rolls now, on a completed
  eat only.
- **A bowl lost its ware tier and glaze the first time you ate out of it.** Vanilla's
  `ServeIntoStack` does not fill the bowl you are holding; unless it already holds the identical meal
  it builds a brand new stack from `mealBlockCode` and assigns it over the top, and `eatenBlock` does
  the same in reverse when the last serving goes. A stoneware bowl therefore came back untiered — and
  a lead-glazed one came back clean, which would have made the whole feature above unreachable through
  meals. `ServeIntoStack` is not virtual; the three interaction entry points that reach it are, so the
  bowl is snapshotted and restored around them. Known gap: serving into a *stack* of bowls sends the
  meal to `TryGiveItemstack` rather than leaving it in hand, and that one is not tracked.

### Notes
- Deviates from the frozen plan on tin in two ways, both because tin's job changed. The plan gave it a
  white texture and a small-brick-kiln gate; it has neither. Rendering white was dropped by request,
  and the temperature argument never supported the kiln gate — tin glaze fires at about 1000 °C, well
  inside a pit kiln. The gate is the ore.

---

## [0.17.0] — 2026-08-10 — Salt glaze

Parcel 4 of the kiln plan, half of it. Tin glaze is deliberately not here — see below.

### Added
- **Salt glaze.** Put salt into a lit small brick or updraft kiln alongside the ware and the fuel and
  the whole load comes out salt-glazed, which seals it exactly as a lead glaze does. One handful does
  a full chamber where lead costs a galena nugget per vessel; in exchange it needs a kiln that can
  actually reach stoneware temperature, which is the gate and needs no code — only the Rudiments kilns
  have a salt slot at all. There is no coating step and no second firing, because the real process has
  neither: at temperature the salt volatilises and the sodium vapour reacts with the silica in the clay
  body itself, so the glaze is made out of the pot. Ware already dusted with lead keeps its lead glaze.
- Salt is recognised by a `rudimentskilnsalt` attribute stamped onto `game:salt`, not by item code, so
  another mod's salt opts in with a one-line patch.
- A handbook section on `game:salt`, and a salt half to the glaze guide page.

### Not added
- **Tin glaze**, which the plan specifies as a white, convincing fake porcelain. Glaze is a stack
  attribute, and an attribute cannot change how a block looks — no texture swap reaches a placed block
  or a ground-storage pile. Without the white it is a strictly worse lead glaze, so it is on hold
  pending a decision about giving lead glaze a health cost, which is the one thing that would make a
  tin alternative matter on its own merits.

---

## [0.16.1] — 2026-08-10 — Kiln fixes from first playtest

Everything here came out of playing the 0.16.0 kilns. No new content.

### Fixed
- **The kilns could not be lit.** Ignition was sneak + right-click on the kiln itself, which the game
  never delivers: a sneaking right-click is routed to block placement first, then to the held item,
  and only reaches the block if neither took it — so with anything at all in your hand the kiln never
  saw the click. Lighting is now the bloomery's, via `IIgnitable`: hold a torch or a firestarter,
  sneak, and hold right-click. Same gesture as every other lit thing in the game.
- **Both kilns were placed facing away from you.** With `rotateY: 0` on the `-north` variant vanilla
  expects the mouth on the *south* face of the shape (see `game:block/clay/bloomery/base`); the small
  brick kiln's shape had it on the north, so it came out 180° round. The mouth moved to the south face
  and the inventory icon now shows the front rather than the back.
- **The updraft kiln was see-through**, having been modelled as an open-topped chamber with no crown.
  It is now the vanilla bloomery shape re-skinned in red brick, which is solid, correctly oriented and
  has the chimney seat already in it. Its two hand-authored shape files are gone.
- **The chimney could not be stacked on the kiln.** Right-clicking the kiln with it in hand was
  swallowed by the loading interaction. It now places on top from that same click, exactly as the
  bloomery's does, is `Unplaceable` anywhere else, and is broken along with the kiln beneath it.
- **The kiln blocks had no names.** Their name and description lang keys were written under the `game:`
  domain, so `rudiments:block-smallbrickkiln-north` and its two siblings resolved to nothing and the
  raw key was displayed in-world.
- Right-clicking a kiln empty-handed emptied the whole thing in one click, which is a bad neighbour to
  a mis-aimed click. It now takes out one slot: ware first, newest first, leftover fuel last.
- `FromTreeAttributes` built the inventory id before calling `base`, so the id was composed from a
  `Pos` that had not been read yet. Base call moved first, and the inventory now takes a live API from
  `worldForResolving` instead of a null one.

### Changed
- A lit kiln emits fire particles at the mouth. There was previously no way to tell one was burning
  except by reading its info panel.
- The kiln info panel names whatever is still missing — fuel, ware, or the chimney — instead of only
  ever offering the ignite prompt.
- The "no fuel" line read *"Charcoal or coal — wood does not burn hot enough"*, which scans as refusing
  charcoal. Reworded, along with the other fuel and ignition strings.

---

## [0.16.0] — 2026-08-10 — The updraft kiln

Porcelain has existed since 0.14.0 and there has been exactly one kiln that could fire it. The
updraft kiln is the cheap, early, unreliable second answer — and the reason to build a beehive
afterwards rather than instead.

### Added
- **`rudiments:updraftkiln`** and **`rudiments:updraftkilnchimney`** — a base and a required chimney
  **directly above it**, validated with the bloomery's own one-line trick rather than a
  `MultiblockStructure`. Flat ground; there is no slope requirement anywhere in this design. Without
  the chimney it refuses to light, with a message saying why.
- **Twice the capacity of the small brick kiln**, expressed the way vanilla expresses ground storage:
  two tiles, so two large `SingleCenter` pieces or eight small `Quadrants` ones, or any honest mix.
  Twelve hours to fire, against the small kiln's ten.
- **It can fire porcelain, badly.** Each porcelain item rolls independently against
  `UpdraftKilnPorcelainFailChance` (0.30). Survivors come out as the raw block's own
  `beehivekiln["0"]` entry — the canonical perfect firing, read off the block so the mapping is never
  duplicated in code — and the rest become shards. A partial loss leaves both in the kiln.
- That ~30% is not a made-up number. An updraft kiln is hottest at the firemouths and cools toward
  the crown, which is why potters packed ware into fireclay saggars and why losses stayed routine
  even in the industrial era. The downdraft beehive exists *because* of that problem. Updraft is a
  draft direction, not a temperature class — the Staffordshire bottle oven is one, and it fired at
  1200–1300 °C. Reaching the temperature was never the difficulty; reaching it evenly was.
- **Two config fields** — `UpdraftKilnBurnHours` and `UpdraftKilnPorcelainFailChance`. Setting the
  second to 0 makes the beehive redundant, which is the point of it not being 0.
- The kilns handbook page now covers all four kilns and why you would want more than one of them.

### Changed
- **`BlockEntityKilnBase`** now holds everything the two kilns share: the fuel gate, loading and
  unloading, the burn timer and greenware conversion, behind an explicit `--- subclass contract ---`
  block in the style of `BlockEntityRettingBase`. `BlockEntitySmallBrickKiln` is now twenty lines
  that say how big it is.
- Capacity is measured in **quarter-tile units** rather than slots, which turns out to be what
  "4 ware slots, matching the pit kiln" actually meant: `BlockEntityPitKiln` extends
  `BlockEntityGroundStorage`, so a pit kiln holds four small pieces *or one large one*, not four of
  anything. The small brick kiln now matches it exactly, and the updraft kiln is cleanly double.
- `BlockSmallBrickKiln` → **`BlockKiln`**, registered under both kilns' class names. Neither kiln has
  any block-level behaviour that differs — the chimney requirement is an ignition condition, and
  ignition conditions belong to the block entity — so two identical classes would have been worse
  code than one honest one.

### Verification
Headless load test, both passes, 0 errors and no mod warnings. Standalone this is **+5 blocks** over
0.15.0: four updraft kiln orientations and the chimney. The 30% loss rate and the chimney check need
a playtest.

---

## [0.15.0] — 2026-08-10 — The small brick kiln, lead glaze and the watering-can gate

0.14.0 gave earthenware a real problem — it leaks — and put the only two answers behind a beehive
kiln. These three ship together because they are the answers, and shipping the gate without them
would have broken day-one farming.

### Added
- **`rudiments:smallbrickkiln`** — a single block, eight fired bricks, four ware slots and one fuel.
  The bricks come from the pit kiln, so the route to it needs nothing new: pit kiln → bricks → brick
  kiln → stoneware. Right-click to load and unload, sneak + right-click to light, status through
  block info. No GUI.
  - **The fuel gate is the bloomery's, verbatim** — burn temperature ≥ 1200 and duration > 30. That
    admits charcoal, coke, bituminous and anthracite coal, and refuses lignite, peat and every wood.
    Cold fuel is refused **at insertion** with a readable message, the way every vanilla fuel gate
    behaves: nothing in vanilla lets you wait ten hours to find out you were wrong.
  - Ware admission is the generic `SmeltingType == Fire` contract rather than a list of codes, so
    Clayworks greenware and any other mod's fires here with **no compat file at all**. The output is
    read off the input block's own combustible properties, never a hardcoded ware code — which is
    why one code path serves every domain.
- **Lead glaze.** Right-click bone-dry greenware with a **galena nugget** — a pile of greenware on
  the ground, or greenware held in your **off hand**. One nugget per vessel, then fire it in **any
  kiln at all, the pit kiln included**. No glaze item, no glaze bucket, no second firing.
  - That is not an abstraction of the process, it *is* the process: raw galena dusted onto a pot and
    fired with it in one pass is how lead-glazed earthenware was made from about 1400 BCE, at
    900–1150 °C — well inside a pit kiln's reach.
  - Glazed ware counts as **sealed**, so it stops seeping and fills a watering can. It does not
    change the body underneath: it is still earthenware, and it gets none of porcelain's advantage
    for stored food. The cheap answer to the water problem, not a shortcut up the ladder.
  - Surviving the fire is the interesting part. The pit kiln discards input NBT outright, so the
    glaze cannot ride the raw stack through firing. `rudiments:BlockGlazableClayware` overrides the
    stack-aware `GetCombustibleProperties` to hand back a per-stack `SmeltedStack` with the glaze
    already on it — after which the vanilla pit kiln, the vanilla beehive and both Rudiments kilns
    carry it for free, with no kiln-side code anywhere.
- **The watering-can gate.** An unsealed earthenware can refuses to fill from a water source, with a
  message. Refuse-to-fill rather than refuse-to-craft, deliberately: you learn the rule at the
  water's edge with the object in hand instead of hitting a silent recipe wall.
- **A `rudiments-glaze` handbook page**, and the kilns and water pages filled in now that the brick
  kiln and the glaze route actually exist.
- **Three config fields** — `KilnMinFuelTemperature`, `SmallBrickKilnBurnHours`,
  `SealedWareRequiredForWateringCan`. All read live.

### Changed
- The order fix from 0.14.0 is generalised (`ClayByTypeOrderFix` → `ByTypeCatchAllOrderFix`) because
  `nugget.json`'s `behaviorsByType` ends in a `"*"` catch-all too, which would have swallowed the
  glaze applicator exactly as Clayworks' catch-all swallowed the porcelain texture. Same one-line
  rule, three more assets.

### Compatibility
- **Clayworks** — its greenware is glazable and its watering can is gated. Without the second of
  those the gate would be trivially bypassable on the default install by simply making a Clayworks
  can instead.

### Verification
Headless load test, both passes, 0 errors and no mod warnings. Standalone this is **+4 blocks** over
0.14.0 — the kiln's four orientations — and no new items, glaze being an attribute rather than an
object. Firing, fuel refusal and the gate itself still need a playtest.

---

## [0.14.0] — 2026-08-10 — Ware tiers: earthenware, stoneware and porcelain

Fired clay was one material with ten paint jobs. It is now three materials. Which one you are
holding decides whether it holds water, how the food inside it keeps, and how easily it breaks —
and the only way up the ladder is a better kiln or a better clay.

**Clayworks is a first-class target, not an afterthought.** Everything below works with it
installed and with it absent; the compat patches simply never apply in the second case.

### Added
- **Three ware tiers.** Earthenware is what unmarked fired clay is and what a pit kiln gives you —
  porous, above 3% water absorption, and it genuinely weeps. Stoneware is anything that comes out of
  a **beehive kiln**: vitrified, sealed, no recipe change and no new clay needed. Porcelain is its
  own body from its own clay. Carried as the `rudimentsware` stack attribute, where *absent* means
  earthenware, so every existing save and every third-party clay item reads correctly with no
  migration.
- **`game:clay-porcelain`** — a fourth state on the vanilla `clay` variantgroup rather than a
  standalone item, so it is a real material with a real code and the vanilla fire clay recipe is
  untouched. Two routes, both from **blue clay only**:
  - **quartz** — 1 crushed quartz + 2 blue clay. Needs a pulverizer, so bronze pounders and
    mechanical power.
  - **bone china** — 1 powdered flint + 1 bonemeal + 1 blue clay. No metal at all: a firepit and a
    quern. Exactly the substitution English potters made from Spode's work in 1789–1793 when they
    could not source petuntse.
- **Porcelain ware** — bowl, crock, jug, cooking pot, storage vessel, planter, flowerpot, oil lamp
  and watering can, with their `-meal`, `-cooked` and dirty-pot companions so a filled porcelain bowl
  actually resolves. Molds and crucibles stay fire clay. Fires white only in a beehive kiln with
  **every door shut**; any door ajar, or a pit kiln, and it comes out as shards.
- **Porcelain's payoff** — food in a porcelain storage vessel keeps markedly longer. Vanilla renders
  the `Stored food perish speed` line for free; no code involved.
- **Seepage.** An unsealed earthenware bowl or jug leaks: half empty at six in-game hours, dry at
  twelve. The rate is a share of *each vessel's own capacity*, so the 3 L jug does not outlast the
  1 L bowl. Nothing ticks — the loss settles from a timestamp the moment you next handle the vessel.
- **Wear fragility.** Using a vessel — drinking, filling, emptying, pouring — can break it. 0.1% /
  0.5% / 1.5% by tier, a median of ~693 / ~138 / ~46 uses. Porcelain is deliberately the most
  fragile: its flexural strength is higher, but it is thin-walled and shatters where thick porous
  ware chips.
- **Drop breakage.** Throw fired pottery on the floor and it shatters, leaving shards and spilling
  its contents rather than voiding them. One entity behavior covers every clay item in the game,
  including other mods', with no per-item patching. Three exemptions: **death drops** (they carry the
  same "dropped by a player" marker as a throw, so this had to be explicit), **greenware** (unfired
  clay deforms, and the test is generic — "still fire-smeltable" — so it covers modded greenware
  too), and **structural ceramics** — bricks, tiles, shingles, chimneys, kiln parts.
- **Five handbook guide pages** — ware tiers, handling, water and porous ware, porcelain, and kilns —
  plus a section on `clay-fire` explaining why fire clay is emphatically not the porcelain body.
- **Fourteen config fields** under `── Ware tiers and kilns ──`. All live-reloadable via
  `/rudimentsreload` except `PorcelainClayPerQuartz` / `PorcelainClayPerFlint`, which retune the
  loaded grid recipes and need a restart.

### Changed
- The **beehive kiln** now yields stoneware. Colours are untouched — the same four door counts give
  the same four results they always did; the tier rides alongside as a stack attribute.
- The fired **bowl**, **jug** and **storage vessel** get thin Rudiments subclasses. The first two
  because `BlockLiquidContainerBase` skips `base` on its fill/pour path, so a pour would otherwise
  transfer un-seeped liquid; the third because `BlockGenericTypedContainer` rebuilds its drop from
  scratch and would drop a stoneware vessel's tier. Behaviour is otherwise unchanged.

### Compatibility
- **Clayworks** — its six exclusive clay colours (azure, crimson, green, orange, pink, snow) get the
  same beehive tiering, fragility and seepage as vanilla ware. Its blue/red/fire chain needs nothing:
  contrary to appearances it does not displace vanilla ware for those three, it adds a wet stage that
  dries back into `game:<ware>-{color}-raw`. Its bricks, tiles, shingles and whole roofing set are
  exempted from drop breakage.
- **CarryOn** — a carried storage vessel keeps its ware tier, because the tier is persisted in the
  block entity tree that CarryOn serialises.
- **vsroofing** — roofs flagged unbreakable-on-drop, behind `dependsOn`.

### Fixed
- `"*"` catch-alls in `*ByType` dictionaries on the clay assets are moved back to last at load.
  `RegistryObjectType.solveByType` takes the **first** matching wildcard, so a catch-all appended by
  any mod silently claims every later, more specific key — including vanilla's own `dirtypot`. This
  restores vanilla's own convention and is what makes porcelain render correctly regardless of mod
  load order.
- The vanilla `rawbrick` grid recipe is narrowed to the three brick clays. It binds `clay-*` to the
  name `type` and emits `rawbrick-{type}`, so a fourth clay state made it try to produce a
  `rawbrick-porcelain` that does not exist. Clayworks ships the identical patch, which is why this
  only surfaced with Clayworks **absent** — a good argument for running both passes.

### Verification
Headless load test, both passes, 0 errors and no mod warnings:
- **Pass 1** — Clayworks 0.6.1, CarryOn 2.0.0-pre.8, Primitive Survival 5.1.0 and vsroofing 1.7.0
  installed.
- **Pass 2** — Rudiments alone. Compared against 0.13.0 this is exactly **+28 blocks and +2 items**:
  9 porcelain greenware, 13 fired forms, 6 lamp orientations, plus `clay-porcelain` and
  `clayworkitem-porcelain`. Every `beehivekiln` and `combustibleProps` target resolved.

In-world behaviour (breakage rolls, seepage over time, kiln outcomes, tooltips, handbook rendering)
still needs a playtest; the load test only proves everything resolves and loads.

---

## [0.13.0] — 2026-08-09 — Interactive scutching: board + scutching sword

Scutching was the most inert step in the fibre chain — a hold-right-click conversion loop identical
to rippling, breaking, hatcheling and oil pressing. It is now a physical, skill-bearing step, and
the second lever the player has on fibre quality (retting was the only one).

### Added
- **`rudiments:scutchsword`** — a wooden swingle with a deliberately dulled edge, the historically
  attested tool (Swedish *skäkta*). Crafted from a plank, a stick and a knife. 150 durability, 1 per
  stroke. Plays the vanilla `axechop` animation; no custom animation patch was needed.
- **`BlockEntityScutchBoard`** — the scutch board now holds a batch of broken bundles and tracks two
  per-side boon meters, fibre integrity, which end is being worked, and the stroke count. The loaded
  bundle's own item shape is draped into the board's notch, so flax and nettle both render with no
  new art.
- **The loop.** Right-click the board with broken bundles to load → hold <kbd>leftmouse</kbd> with the
  sword to strike → <kbd>sneak</kbd> + right-click to turn the bundle → right-click empty-handed to
  collect. `GetPlacedBlockInteractionHelp` now documents all four (no fibre block had it before).
- **The trap, and it is emergent.** Boon decays asymptotically, which gives the documented diminishing
  returns for free. Damage is keyed to the *worked side's* local cleanliness: while there is boon left
  the blade rides on it and costs nothing, and once the side is clean it starts cutting long **line**
  into short **tow**. There is no arbitrary timer anywhere in it.
- **The tell doubles as the flip cue.** Crossing `ScutchSafeCleanliness` changes the strike from a dull
  low-pitched thud throwing brown shives to a sharper high note throwing pale fibre fluff — the same
  signal says "this end is done" and "damage starts now". Block info adds cleanliness, fibre-intact,
  a grade preview, and an explicit prompt to turn the bundle the moment the tell fires with the far
  half still untouched.
- **Nine new config fields** under `── Scutching ──`, all live-reloadable via `/rudimentsreload`:
  `ScutchStrokesPerSecond`, `ScutchBoonPerStrokeMultiplier`, `ScutchDamagePerStroke`,
  `ScutchSafeCleanliness`, `ScutchCrossSideBleed`, `ScutchNettleBoonMultiplier`,
  `ScutchTowFibersPerBundle`, `ScutchShowMeters`, `MechScutcherTowShare`.
  `ScutchShowMeters: false` swaps the percentages for qualitative words.

### Changed
- **Board tiers now scale capacity and boon-per-stroke, not throughput.** `scutchDuration` /
  `scutchAmount` are replaced by `scutchCapacity` (2 / 4 / 8) and `boonPerStroke` (0.12 / 0.16 / 0.20).
  A better-cut notch holds the bundle steadier, so workmanship buys both.
- **The board is wood at every tier, and the shape gained its notch.** Historically the scutch board
  was always an upright wooden plank with a notch sawn near the top and the upper piece split off,
  leaving a guard that stops the knife reaching your holding hand — metal appears only on some hand
  knives and inside industrial mills. Tier display names now describe workmanship
  (split-log / planed / joiner's) rather than material.
- **Outcome at collect time.** Total cleanliness sets the grade (<0.50 coarse, <0.775 standard, else
  fine) and is capped by the input's retted quality, so retting remains the ceiling and scutching can
  only lose what it granted. Fibre integrity decides how much of the batch survives as scutched
  bundles; the remainder is *reclassified*, not deleted, and comes off as `rudiments:coarsefibers`.
- **Nettle needs about half again as many strokes per end** (`ScutchNettleBoonMultiplier`), which is
  the documented difference in how much boon it carries.
- **The mechanical scutch mill now wastes a share of every batch as tow** (`MechScutcherTowShare`,
  default 35%), ejected at the block as coarse fibres. Mill scutching genuinely wasted more fibre than
  hand work, and the millers were paid in the tow. **Quality is untouched** — this is a
  throughput-for-yield trade, not throughput-for-quality. The mill's 2-slot inventory is unchanged, so
  existing saves deserialise; the tow is ejected rather than given a third slot precisely for that.
- Handbook guide, block/item descriptions and the flax and nettle handbook steps rewritten around
  load → strike → **listen for the change** → flip → collect, with the under/over trade-off and the
  mill trade-off stated outright.

### Removed
- **`textures/block/tool/scutchboard/blade-copper.png` and `blade-iron.png`** — the copper and iron
  board faces were inaccurate and are retired. `blade-wood.png` is replaced by `board-crude.png`,
  joined by `board-planed.png` and `board-fine.png` (all three the same plank, differing only in how
  smooth and pale the worked surface is), and the shape's texture key `blade` is now `face`.
  **Anyone with a resource pack overriding the two metal textures loses that override.** The
  ripple's identically-named textures are untouched.
- The board's old hold-right-click conversion loop, and its `scutchDuration` / `scutchAmount`
  attributes.

### Notes
- **Existing boards keep working without being re-placed.** Adding `entityClass` does not retroactively
  create block entities in loaded worlds, so the block spawns one server-side on first interaction if
  it finds none.
- `MechScutcherTowShare` is a yield nerf to existing mill setups. Set it to `0` to restore the old
  behaviour; quality was never affected either way.
- Strokes are per *session*, not per bundle — loading fewer bundles than capacity wastes strokes,
  which is what makes the tier upgrade worth building. This mirrors the old `scutchAmount` model.
- The board keeps one shared shape across tiers; the tier read is texture-only. A distinct crude-log
  shape for the primitive tier is a later polish pass.
- Stroke counts and `ScutchSafeCleanliness` will likely want a playtest pass, as the carding angles did
  in v0.9.2. Everything is config-exposed and live-reloadable so tuning needs no rebuild.

## [0.11.0] — 2026-07-16 — Bloom-stage flax harvest & seeds only from mature crops

Two new gameplay rules, both **config-gated and enabled by default** (`ModConfig/rudiments.json`, surfaced in-game via AutoConfigLib's ImGui menu; both flags need a server restart to fully apply since they gate JSON patches).

### Added
- **`FlaxBloomHarvest`** (default `true`): flax drops **nothing** until it blooms. Cut it **in bloom (stage 8)** for seed-free bundles stamped *"cut in bloom"* — they ret from **standard** quality with a chance at the **fine** window. Let it stand to **full maturity (stage 9)** for the normal seed rate (avg 1.2) plus grain (avg 2.0) and *"fully mature"* bundles that ret **coarse → standard**, never fine — same range as nettle. Set to `false` to restore the pre-0.11 table (bundles from stage 3, seeds at every stage, all bundles ret coarse-to-fine).
- **`SeedsOnlyWhenMature`** (default `true`): vanilla crops only return seeds once fully grown. Three vanilla loopholes closed:
  - the immature-stage fallback drop (~0.7 seeds on all 16 vanilla crops) is removed;
  - farmland's `debuffUnaffectedDrops: ["seeds-*"]` exemption is removed, so damage multipliers apply to seeds too;
  - the engine's literal *"minor hack to make dead crop always drop seeds"* is bypassed via a new `rudiments:BlockDeadCrop` class — an animal-eaten crop (multiplier 0) now returns **nothing**. This one piece honors the config live (`/rudimentsreload`); the rest needs a restart.
- **Fiber harvest potential** (`fiberpotential` itemstack attribute): stamped on flax bundles at harvest, carried through stook-curing and rippling, consumed when retting assigns final quality. Visible in the bundle tooltip ("Cut in bloom…" / "Fully mature…").
- New patch file `patches/crop-seeds.json`; new lang lines for the two tooltips.

### Changed
- **Nettle is now hard-capped at standard quality regardless of config** — previously the retting RNG could roll a fine window for nettle too. Fine fibres now come exclusively from bloom-cut flax.
- **Rippling** only yields seeds/grain from bundles that carry ripe seed heads (mature or legacy bundles). Bloom-cut bundles convert to rippled with no seed, no grain, and no seed particles.
- Retting status text: the "fine fibre possible in ~X" countdown is only shown for batches that can actually reach fine; fine-eta lines now say "standard fibre" (bloom batches convert at standard, not coarse).
- Stook and field-retting/vat stack merges now require matching quality **and** harvest potential, so grades never average together.
- Handbook guide, flax item descriptions, and handbook steps rewritten around the bloom-vs-mature tradeoff.

### Notes
- Old-save bundles carry no potential attribute and ret over the full legacy coarse-to-fine range; in-flight retting batches finish under their old rules. No orphaned items.
- With `FlaxBloomHarvest: false`, flax keeps its legacy early-stage seed drops even if `SeedsOnlyWhenMature` is on (the legacy flax table is restored verbatim).

### Fixed
- **The v0.10.4–0.10.7 `fpHandTransform` edits never did anything.** Traced the actual renderer: `EntityShapeRenderer.RenderHeldItem` always requests `EnumItemRenderTarget.HandTp`/`HandTpOff`, never `HandFp`, and `CollectibleType.FpHandTransform` is marked `[Obsolete("Use TpHandTransform instead")]` in the engine source — first and third person have shared a single transform (`tpHandTransform`) for a while now, just rendered through different camera FOVs. Every fp-only tweak made via the JSON field was silently ignored, which is why manually editing values (and my own "fixes") produced zero visible change.
- Since third person already reads correctly off the shared transform and there's no separate JSON lever for fp alone, `ItemHandCards.OnBeforeRender` now clones and rolls the render transform 180° about its own forward axis (`rotation.Z += 180`, which by construction never moves the forward/board axis), applied **only** when the target is `HandTp`, the local camera is first-person, and the rendered slot is verifiably this player's own `RightHandItemSlot` — so third person and other players' views are untouched.

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
