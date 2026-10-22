# Rudiments

**AI Disclosure:** This mod builds on the original source code of [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by @Oppo. Ongoing modifications and maintenance are done primarily with the assistance of Anthropic's Claude models (Opus/Sonnet). If you'd prefer to avoid AI-assisted content, please take this into account.

**A Vintage Story mod that puts the depth back into pre-industrial craft.**

Rudiments adds realistic multi-step production chains for the materials and tools people relied on before iron. You work through them one process at a time, and every step counts. Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://rhsworks.org).

Feature-stable as of 2.1.10. Fibre production and pottery/kilns are both complete, tested chains, and no new systems are planned. From here on, releases are minor bug fixes only.

---

## Fibre Production

Flax and stinging nettle go through multi-step chains modelled on the real process. Harvest timing sets how good the fibre can get. Retting timing decides the grade within that range. Fine fibre then gives real mechanical bonuses further down the line.

### Flax: 7 steps

`Harvest after bloom` → `Stook-cure` → `Ripple` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Flax drops nothing until it has bloomed. After that, when you cut it matters:

- **Cut in bloom (stage 8):** the best fibre. Bundles ret from *standard* quality with a chance at the brief *fine* window. The catch: they carry **no seed at all**, not from the crop and not at the ripple.
- **Fully mature (stage 9):** the plant drops seeds and grain, and rippling its bundles recovers more of both. By now, though, the stems have coarsened. Mature bundles ret from *coarse* and top out at *standard*, like nettle.

Fine fibre only comes from bloom-cut flax. Seeds and linseed oil only come from mature flax. You can't have both from one plant.

### Nettle: 6 steps

`Harvest mature` → `Stook-cure` → `Ret` → `Dry` → `Break` → `Scutch` → `Hatchel`

Nettle skips rippling. Young plants (stages 3–6) give edible leaves. Hold right-mouse for a slow, careful harvest that avoids the sting, or wear gloves and break them normally. Mature plants (stages 7–9) give fibre bundles.

### Retting quality

Harvest timing sets the quality range, and retting timing decides where in it you land.

- **Bloom-cut flax** rets **Standard → Fine → Standard → Rot**. The fine window is short. Catch it or settle for standard.
- **Mature flax and nettle** ret **Coarse → Standard → Rot**. Waiting improves them, but fine is out of reach.

- **Field retting:** free, but it depends on the weather. Dry spells stall it and rain gets it going again.
- **Barrel retting:** seal bundles and water in a barrel and it reopens right away as a retting bath, with a progress readout you can watch. Faster than the field, and you're in control of it.
- **Lime retting:** seal them in limewater instead. It's the fastest option, but quality is permanently capped at Standard. Good for bulk. Not for craft work.

### Scutching: the second quality lever

Retting isn't the only decision that matters. Scutching is hands-on. Load broken bundles onto a **scutch board**, then hold left-mouse with a **scutching sword** to beat the woody boon off them.

- **Only the end facing you gets worked.** Sneak + right-click turns the bundle so you can do the other half. Never turn it and there's a cap on how clean the batch can get.
- **Listen for the change.** While there's boon left, the strike lands dull and throws brown shives. Once that end runs clean, the pitch goes up and it throws pale fibre fluff. That's your cue to turn it. From then on the blade has nothing left to ride on, so it starts cutting the long *line* into short *tow*.
- **The two mistakes cost you different things.** Under-scutching leaves boon bound in and grades the batch down. Over-scutching keeps the grade but turns more and more of the batch into coarse fibres. Neither one is a timer. Both come out of the same curve.
- **Retting is still the ceiling.** Scutching can only lose quality that retting gave you. It can't add any.

What separates the board tiers is the quality of the woodwork. A better-cut notch holds more bundles *and* clears more boon per stroke. Nettle carries far more boon than flax and needs about half again as many strokes per end.

The **mechanical scutch mill** is still the hands-off option, but it's a trade-off. It shreds a share of every batch into tow, which drops out at the mill. You lose yield. The grade stays the same. Historically the millers were paid in that tow, and this was the complaint they kept hearing.

### Fibre quality bonuses

Fine fibre twisted into **fine cord** gives bows a significant durability bonus. It's also used to craft gambeson armour.

### Equipment

| Block | Function |
|---|---|
| Stook | Weather-aware curing and drying. Bundles are placed directly on the ground |
| Ripple | Combs seed heads from cured bundles. Mature bundles yield seeds and grain; bloom-cut bundles yield none |
| Retting bath (barrel) | Seal bundles + water or limewater in any barrel and it reopens as a timed retting bath |
| Retting vat | Legacy block. Existing vats still work, but they can't be crafted anymore |
| Drying rack | Safe indoor drying; quality is preserved |
| Break | Breaks dried bundles |
| Scutch board | Interactive scutching. Load bundles and beat them with a scutching sword, turning them halfway through. Split-log / planed / joiner's tiers scale capacity and boon cleared per stroke |
| Scutching sword | The wooden swingle you beat the board with. Deliberately dulled so it scrapes instead of cutting |
| Hatchel | Final combing; primitive / simple / advanced tiers |
| Mechanical scutch mill | Axle-driven. Automates breaking and scutching, but shreds a share of each batch into tow |
| Vanilla fruit press | Presses flax grain into linseed oil (vanilla `oilportion-flax`) and seed cake (`rudiments:linseedcake`). It's a `juiceableProperties` patch; there's no custom block |

---

## Nettle: ~~invasive weed~~ resilient crop

Nettle no longer drops seeds. It spreads the way real nettle does, by **rhizome**.

- **Root crowns.** Cutting a plant at any stage leaves a root crown (stub). It regrows on its own, or you can dig it up with a shovel to get a rhizome you can transplant.
- **It spreads.** Wild nettle creeps into nearby fertile ground and strongly prefers tilled farmland. A built-in density cap and an outward radius limit keep patches from overrunning a world.
- **Networked feeder.** On its own soil nettle is efficient, using roughly half the nitrogen of an ordinary crop. As it grows, though, it leaches nitrogen from adjacent farmland. It never drains its own kind. Keep it away from your fields, or use it on purpose to exhaust ground you want to fallow.
- **Invasive mode (off by default).** Turn it on and nettle spreads as invisible buried rhizomes that surface without warning. Tilling the soil clears them before they come up.

All of this is tunable. See [Configuration](#configuration).

---

## Seeds only from mature crops

In vanilla you get seeds back almost no matter what happens to a crop. Rudiments closes those loopholes for **every** vanilla crop, not just flax:

- **Breaking an immature crop returns nothing.** No more pulling half-grown plants for a guaranteed seed refund.
- **Damaged crops lose their seed exemption.** Vanilla quietly left seeds out of the frost/heat damage multipliers. Now seeds take the same hit as the rest of the harvest.
- **A crop eaten by an animal returns nothing.** Vanilla's dead-crop code literally contains a "minor hack to make dead crop always drop seeds". That hack is gone. If the deer got your turnips, it got your seeds too. Fence your fields.

Nettle isn't affected. It never dropped seeds; it regrows from rhizomes.

You can turn off both this and the bloom-stage flax harvest in the [config](#configuration) for a gentler game.

---

## Pottery & Kilns

Fired clay comes in three bodies, each better than the last. Which one you get depends on the kiln you fire it in and, for the two custom kilns, the fuel you use.

### Ware tiers

| Tier | How you get it | Behaviour |
|---|---|---|
| Earthenware | The default. A pit kiln, or a small brick/updraft kiln fired on wood, peat or lignite | Porous. Leaks liquids over time unless glazed, and it's the most fragile in the hand |
| Stoneware | Any beehive firing, or a small brick/updraft kiln fired on charcoal, coke, bituminous or anthracite coal | Vitrified and sealed; sturdier than earthenware |
| Porcelain | Its own clay (`clay-porcelain`, from blue clay via a pulverizer or a firepit+quern route), fired white only in a beehive kiln with every door shut | Vitrified and dense. Ordinary use never wears it; only dropping or throwing it does |

- **Seepage.** Unsealed earthenware leaks. Half empty at 6 in-game hours, dry at 12. It loses a share of *each vessel's own capacity*, so a 1 L bowl and a 3 L jug empty on the same clock.
- **Wear fragility.** Each use of a vessel (drinking, filling, emptying, pouring) carries a shatter chance. Earthenware's is the highest. Porcelain's is zero by default.
- **Drop breakage.** Any fired clay item shatters if it's thrown or dropped hard enough. Its contents spill out instead of vanishing.
- **The watering-can gate.** An unsealed earthenware can won't fill from a water source. Glaze it, or move up the ladder.

### What counts as handling a vessel

By default only active use rolls the wear chance: drinking, filling, emptying, pouring. Three switches widen that, and all of them default off. None adds a chance of its own. Each one just lets the same per-tier number roll at one more moment:

| Setting | Adds the roll when |
|---|---|
| `BreakageIncludesPlacedContainers` | You open a container you've set down |
| `BreakageIncludesGroundStorage` | A vessel lands in ground storage (a shelf or the bare ground) |
| `BreakageIncludesFirepitCooking` | A cooking vessel's meal finishes, standing in for the heat-then-cool cycle. If the roll fails, the meal goes down with the pot |

Fireclay (crucibles, ingot molds and tool molds) sits outside the earthenware/stoneware/porcelain ladder and never rolls any of these. It carries a different risk; see [Crucibles and molds](#crucibles-and-molds).

### The kiln ladder

`Pit kiln` (free, earthenware only) → `Small brick kiln` / `Updraft kiln` → `Beehive kiln` (vanilla, downdraft, even heat)

- **Small brick kiln.** A single block with four ware slots, built from bricks the pit kiln already gives you. The cheapest way off earthenware.
- **Updraft kiln.** Double the capacity, and it needs a chimney directly above it. It's the first kiln that can fire porcelain, though unevenly: expect to lose around 30% of a porcelain load to shards. The vanilla beehive kiln's even downdraft heat is the reliable answer to that loss.
- **Fuel decides the tier.** There's no pass/fail gate, and nothing is refused at the mouth. Charcoal, coke, bituminous and anthracite coal fire stoneware, and in the updraft kiln they give porcelain its chance at surviving whole. Wood, peat and lignite still fire the kiln, just cooler. The load comes out earthenware instead, and porcelain gets no roll at all.
- **One firing's worth, no more.** Each kiln's fuel slot caps at exactly what one firing needs (4 for the small kiln, 8 for the updraft) and burns down to nothing when it lights. No stockpiling a 64-stack for dozens of firings.

### Glazes

Right-click bone-dry greenware to apply a glaze. It works on a pile on the ground or on a piece held in your off hand. There's no glaze item and no second firing: the glaze fires along with the piece in any kiln, the pit kiln included.

| Glaze | Applied with | Cost |
|---|---|---|
| Lead | A galena nugget, one per vessel | The cheapest seal in the game. Eating or drinking from leaded ware builds up a burden, though, and past a threshold it costs max health. It decays on its own if you ease off; `/rudimentslead` shows your number |
| Tin | A cassiterite nugget over already-leaded greenware, two nuggets | Same seal, no poisoning. It's a lead glaze opacified with tin, as it was historically |
| Salt | A handful thrown into a lit small brick or updraft kiln | Seals the entire load at once. Needs a kiln hot enough to fire stoneware |

Lead poisoning is fully configurable and decays daily whether you're online or not. Servers can turn it off with `LeadPoisoningEnabled`. See [Configuration](#configuration).

### Crucibles and molds

Fireclay is its own family. It has no ware tier, and it doesn't seep or wear from handling. Its risk comes at the fire instead, and vanilla only applies that risk to half of it. Shatter an ingot mold and you get a fraction of the pour back as bits. A crucible, on the other hand, has never had a bad day in its life. Two opt-in settings even that up. **Both default off.** Leave them alone and smelting behaves exactly like vanilla.

- **A smelt can fail** (`BreakageIncludesSmeltingFailure`). The roll happens the moment the smelt finishes: 5% by default (`SmeltingFailureChance`), flat, since fireclay isn't on the ware ladder. The metal isn't gone. The crucible becomes an inert **cracked crucible** that still holds it, sitting in the firepit where the good one would have been. Take it out and it tells you what's stuck inside.
- **Getting the metal back takes tools.** A cracked crucible can't be poured or refilled, so it has to be broken open. Set it down in ground storage or on a shelf, hold a **hammer in your off hand** and a **chisel** in your active hand, and right-click. You get `SmeltingFailureYield` (20% by default) of the metal back as bits, plus the usual ceramic shards. Both tools lose a little durability. Recovery reads the same vanilla data an ingot mold does, so every metal and alloy the game already knows about works with no per-metal setup.
- **Molds can be held to the same standard** (`MoldRecoveryRequiresTool`). Vanilla lets you recover a shattered mold's bits by breaking it bare-handed, even though it demands a hammer and chisel to work a *hardened* pour loose. Turn this on and the shattered case wants the same combo. Ingot molds only. Tool molds are untouched.

---

## Configuration

All settings live in `VintagestoryData/ModConfig/rudiments.json`, which is created on first launch. If [AutoConfigLib](https://mods.vintagestory.at) is installed you can edit them in-game instead of touching the file.

Apply changes without restarting by running `/rudimentsreload` (requires the `controlserver` privilege). The exception is the two harvest-realism flags below. They gate JSON patches, so they need a **server restart** to fully apply.

### Harvest realism

| Setting | Default | Effect |
|---|---|---|
| `FlaxBloomHarvest` | `true` | Staged flax harvest: nothing before bloom, fine-capable seed-free bundles at stage 8, seeds/grain plus standard-capped bundles at stage 9. `false` restores the pre-0.11 table (bundles from stage 3, seeds at every stage, all bundles ret coarse-to-fine). Nettle stays capped at standard either way. Restart required. |
| `SeedsOnlyWhenMature` | `true` | Vanilla crops only return seeds once fully grown. Immature breaks, damaged crops and animal-eaten crops return nothing. `false` restores vanilla behavior. Restart required. |

### Retting

| Setting | Default | Effect |
|---|---|---|
| `BarrelRettingLitresPerBundle` | `1.0` | Litres of water/limewater a barrel retting bath needs per bundle. A 50 L barrel fits 50 divided by this many bundles (e.g. `4.0` → 12 bundles). Restart required. |

### Scutching

| Setting | Default | Effect |
|---|---|---|
| `ScutchBoonPerStrokeMultiplier` | `1.0` | Global multiplier on boon cleared per stroke. Raise it to shorten the whole minigame |
| `ScutchDamagePerStroke` | `0.10` | Fibre integrity lost per stroke once the worked end is clean. `0` makes over-scutching harmless |
| `ScutchSafeCleanliness` | `0.75` | How clean an end can get before strokes start cutting line into tow. Also the point where the strike changes sound |
| `ScutchCrossSideBleed` | `0.10` | Share of each stroke's cleaning that bleeds onto the end you are *not* working. At `0` you have to flip the bundle; otherwise flipping just helps |
| `ScutchNettleBoonMultiplier` | `1.5` | How much more boon nettle carries than flax, as a divisor on boon cleared per stroke |
| `ScutchTowFibersPerBundle` | `2` | Coarse fibres handed out per bundle lost to over-scutching or wasted by the mill |
| `ScutchStrokesPerSecond` | `1.456` | Stroke rate. Matched to the sword's swing animation, so if you change one, change the other |
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
| `NettleSpreadMaxDensity` | `5` | Local density cap. Doesn't limit outward reach |
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
| `BreakageIncludesGroundStorage` | `false` | If `true`, the per-tier wear chance also rolls the moment a vessel is set down in ground storage (a shelf or the bare ground) |
| `BreakageIncludesFirepitCooking` | `false` | If `true`, a fragile cooking vessel (pot, crock, dirtypot) rolls when its firepit cook finishes, standing in for the heat-then-cool cycle. A failure loses the meal too. Fireclay never rolls this |
| `ThrownClayBreakChance` | `1.0` | Chance a deliberately dropped fired clay item shatters on landing. `0` disables it (the hard-landing trigger below still applies) |
| `ThrownClayBreakWholeStack` | `true` | Break the whole dropped stack (Ctrl+Q) rather than one item (Q) |
| `ThrownBreakOnDeathDrop` | `false` | Whether pottery scattered on death also shatters |
| `ClayImpactBreakSpeed` | `0.4` | Downward landing speed above which any fired clay shatters, however it got there. `0` disables the hard-landing trigger |
| `EarthenwareEmptyHours` | `12` | In-game hours for a full unsealed earthenware vessel to seep dry. `0` disables seepage |
| `KilnMinFuelTemperature` | `1200` | Minimum burn temperature (paired with burn duration > 30) a fuel needs to fire stoneware instead of earthenware in a small brick or updraft kiln |
| `SmallBrickKilnBurnHours` | `10` | In-game hours a small brick kiln takes to finish a firing |
| `SmallBrickKilnFuelPerFiring` | `4` | Fuel a small brick kiln needs per firing. Also the hard cap on its fuel slot |
| `UpdraftKilnBurnHours` | `12` | In-game hours an updraft kiln takes to finish a firing |
| `UpdraftKilnFuelPerFiring` | `8` | Fuel an updraft kiln needs per firing. Also the hard cap on its fuel slot |
| `UpdraftKilnPorcelainFailChance` | `0.30` | Per-item chance porcelain comes out of an updraft kiln as shards instead of whole, on a hot-enough firing |
| `SealedWareRequiredForWateringCan` | `true` | Whether an unsealed earthenware watering can refuses to fill |
| `PorcelainClayPerQuartz` | `2` | Blue clay converted per crushed quartz on the pulverizer route to porcelain clay. Requires restart |
| `PorcelainClayPerFlint` | `1` | Blue clay converted per powdered flint + bonemeal on the bone-china route. Requires restart |

### Crucible and mold failure

All off by default. Smelting behaves exactly like vanilla until you turn one on.

| Setting | Default | Effect |
|---|---|---|
| `BreakageIncludesSmeltingFailure` | `false` | Whether a crucible's smelt can fail on completion, cracking the crucible into an inert item that still holds the metal |
| `SmeltingFailureChance` | `0.05` | Flat per-smelt chance of that failure. It isn't tiered, because fireclay sits outside the earthenware/stoneware/porcelain ladder |
| `SmeltingFailureYield` | `0.2` | Share of a cracked crucible's metal recovered as bits when it's cracked open with hammer + chisel. The crucible's version of an ingot mold's 20% shatter payout |
| `MoldRecoveryRequiresTool` | `false` | Whether recovering a *shattered ingot mold's* bits also requires hammer + chisel, instead of vanilla's free bare-handed break. Ingot molds only; tool molds unaffected |

### Lead poisoning

| Setting | Default | Effect |
|---|---|---|
| `LeadPoisoningEnabled` | `true` | Whether lead-glazed vessels poison whoever eats or drinks from them. Server-authoritative, so a client can't quietly turn off the warnings while still being poisoned |
| `LeadPerServing` | `1.0` | Burden gained per helping consumed from a lead-glazed vessel |
| `LeadDecayPerDay` | `5.0` | Burden shed per in-game day, online or off. The only cure is time away from leaded ware |
| `LeadOnsetBurden` | `15.0` | Below this burden nothing happens at all, so occasional use of leaded ware is free |
| `LeadBurdenPerHealthPoint` | `12.0` | Burden above the onset threshold per point of max health lost |
| `LeadMaxHealthPenalty` | `5.0` | Most max health lead poisoning can ever take, out of 15 |

### Other

| Setting | Default | Effect |
|---|---|---|
| `StookMaxBundles` | `64` | Maximum bundles a single ground stook can hold |
| `RippleGrainYieldMultiplier` | `1.0` | Global multiplier on grain yields from rippling; `0` disables grain drops |
| `RippleSeedYieldMultiplier` | `1.0` | Global multiplier on seed yields from rippling; `0` disables seed drops |
| `LinseedOilLitresPerGrain` | `0.078` | Litres of vanilla `oilportion-flax` extracted per flax grain pressed in the vanilla fruit press; ~2 stacks of flax fills the press's fixed 10-litre capacity. Restart required |

---

## Compatibility

| Version | Status |
|---|---|
| VS 1.22.x | Supported |

**Wool & More.** If [Wool & More](https://mods.vintagestory.at) is installed, Rudiments adds a **hand carding** step to the wool chain. Washed fleece has to be carded into **rolags** before it can be twisted or spun into twine. Craft a pair of **hand cards** (2 planks + leather + metal nails & strips, 128 durability). Hold washed wool fibres in your off hand and the cards in your active hand, then hold right-mouse. A couple of seconds of brushing gives you a rolag per fibre. Rolags twist into wool twine in the grid, 4 per twine. All of it ships disabled and gets switched on by patches gated on the wool mod. Without that mod it leaves zero footprint.

**Immersive Fibercraft** (spinning wheel / drop spindle). Nettle fibre and fine fibre get `spinningProps` and can be spun on the wheel: nettle into flax twine, fine fibre into fine cord. If Wool & More is also installed, the carding requirement applies on the wheel and spindle too. Washed wool fibres lose their direct spinnability, and **rolags** become the spinnable stage instead (2 rolags → 1 wool twine, the same ratio Immersive Fibercraft uses for raw fibre). The drop spindle charges Rudiments fibres the same as the wheel does. Immersive Fibercraft's spindle takes the full input on each of its two spins, so Rudiments splits it across them. These integrations switch on by themselves and do nothing if the mod isn't there.

**Clayworks.** If [Clayworks](https://mods.vintagestory.at) is installed, its clay barrels work as proper retting baths. Seal bundles and water in one and it pops open into the same timed, quality-tracked retting process as a wooden barrel. (Without this patch the clay barrel would complete the raw fallback recipe and ret almost instantly.)

**Fittin' & Shardin'.** With [Fittin' & Shardin'](https://mods.vintagestory.at/fitnshard) installed, pottery that Rudiments shatters drops its **potsherd** instead of vanilla's dead-end shard pile, so broken ware feeds its crush-and-grind route to grog. Stray vanilla shard piles from breakage Rudiments doesn't handle, like a shattered tool mold, grind straight to grog as a fallback. Rudiments also keeps porcelain clay out of the mod's brick and lithodipyra recipes. Otherwise they'd try to produce porcelain variants that don't exist. All of it is gated on the mod and does nothing without it.

**String Sense.** [String Sense](https://mods.vintagestory.at/stringsense) adds a crude cordage tier below twine and re-points primitive recipes at it. That pairs well with Rudiments, where twine takes more work to make. Rudiments wires its flax chain into that tier the same way String Sense's own AgeOfFlax compat did. String Sense's vanilla-flax strand shortcuts (orphaned by Rudiments' crop changes) are disabled, and **crude flax cord twists directly from rippled bundles** (3 → 1) with no retting needed. The full chain is still the only route to proper twine and fine cord.

**Toolsmith.** If [Toolsmith](https://mods.vintagestory.at/toolsmith) is installed, Rudiments' fine cord (`rudiments:finecord`) is registered as a premium binding material in its tool-tinkering system. That puts it a step above leather-tier cordage, which matches fine cord's established "uniform, strong, and resistant to repeated stress" character. Nettle-spun twine needs no separate registration since it produces vanilla flax twine, and Toolsmith already supports that natively. The integration is pure data with no hard dependency. If Toolsmith is absent, it does nothing. (It replaces Rudiments' earlier homegrown tool-binding system, which clashed with Toolsmith's more complete take on the same idea.)

**Biomes.** With [Biomes](https://mods.vintagestory.at/biomes) installed, wild stinging nettle generates in the Palearctic and Nearctic regions. *Urtica dioica* is native across Europe and Asia, and its subspecies *gracilis* across North America. Biomes only spawns plants its config assigns to a region, so without this entry wild nettle wouldn't generate at all. Rudiments' other plants are vanilla, and Biomes covers those itself. Needs a new world, same as Biomes.

**AutoConfigLib / ConfigLib.** Supported for in-game config editing.

---

## Credits

Expanded from [AgeOfFlax](https://mods.vintagestory.at/show/mod/33768) by @Oppo.  
Inspired by [Primitive Technology](https://www.youtube.com/channel/UCAL3JXZSzSm8AlZyD3nQdBA) and [RHSWorks](https://www.rhsworks.org).

---

## Licence

MIT. Do what you like; credit appreciated.
