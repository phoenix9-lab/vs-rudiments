# Rudiments — Spinning Wheel Compat

A small optional addon that bridges **Rudiments** and **Immersive Fibercraft**
(`spinningwheel`, formerly "Spinning Wheel") so that nettle fiber produced by
Rudiments can be spun on the Spinning Wheel.

## Requirements

Both parent mods must be installed for this addon to do anything useful:

| Mod | ModDB |
|---|---|
| Rudiments (`rudiments`) | local / private release |
| Immersive Fibercraft (`spinningwheel`) | https://mods.vintagestory.at/show/mod/34327 |

The addon itself has **no hard dependency on either mod**. The single JSON
patch inside uses `dependsOn` guards, so if one or both parent mods are absent
the patch silently does nothing and the game loads without errors.

## What it adds

| Input | Quantity | Output | Spin time |
|---|---|---|---|
| `rudiments:nettlefiber` | 2 | `game:flaxtwine` | 4 s |

The ratio (2 fibers → 1 twine at 4 s) matches the vanilla flax-fiber recipe
already built into Immersive Fibercraft.

### Note on fiber quality

Rudiments tracks fiber quality (coarse / standard / fine) as a runtime
stack attribute (`fiberquality` integer) on the same item code. The Spinning
Wheel reads `spinningProps` from the item's JSON attributes (type-level), not
from per-stack attributes, so **this recipe applies to nettle fiber of any
quality**. If you want quality-differentiated spinning outputs (e.g. fine
fiber → fine cord), that would require a C# code mod to intercept the spinning
result — out of scope for a pure content addon.

### Note on flax fiber

Immersive Fibercraft already adds `spinningProps` to `game:flaxfibers` in its
own patch (`game-flaxfibers.json`). Rudiments patches `game:flaxfibers`
with its `FiberQuality` behavior but does not change the item code. Therefore
flax fiber spinning already works if both mods are installed — **no additional
patch is needed for flax**.

## Installation

Drop this zip (or the unzipped folder) into your `Mods/` folder alongside
Rudiments and Immersive Fibercraft. Load order is handled automatically by the
`dependsOn` guards in the patch.

## Technical details

- **Mod type**: content (JSON-only, no C# build needed)
- **Patch file**: `assets/rudimentsspinningcompat/patches/rudiments-nettlefiber.json`
- **Recipe mechanism**: `spinningProps` attribute injected via JSON patch, read
  by `BlockEntitySpinningWheel.GetSpinResult()` in Immersive Fibercraft
- **Verified against**: Immersive Fibercraft v1.2.8 (GitHub: HeadPilgrim/vs-spinningwheel)

## Changelog

- **1.0.0** — Initial release: nettle fiber → flax twine via Immersive Fibercraft Spinning Wheel
