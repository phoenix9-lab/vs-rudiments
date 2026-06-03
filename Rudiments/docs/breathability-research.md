# Breathability Research — Fine Gambeson / VS Temperature API

**Date:** 2026-06-01  
**Verdict:** BLOCKED (no cooling mechanic) — perk retired, no mechanical change shipped (Option C).

---

## What the VS temperature system actually does

**Class:** `EntityBehaviorBodyTemperature` (code: `"bodytemperature"`, registered on `player.json`)

**Clothing stat:** `warmth` (float, degrees Celsius offset)  
- Set in `attributesByType` on wearable itemtype JSON  
- Formula: `warmth = maxWarmth * min(2 * condition, 1.0)` — items below 50% condition lose warmth proportionally  
- Displayed as `+X°C` in hover tooltip  
- Also supported as `warmthByType` (variant map) in the same `attributes` block (see `armor-hide.json`)

**Rain protection stat:** `rainProtectionPercByType` (float 0–1)  
- Found in head/shoulder clothing items  
- Reduces wetness accumulation rate, which indirectly speeds heat loss  
- This stat IS functional (not marked unimplemented)

**Unimplemented stats** (exist in game files but do nothing):  
- `rainProtection` (flat, older form)  
- `eyeProtection`

**Overheating:** As of VS 1.18 and confirmed still true in 1.22, **there is no overheating penalty**. High body temperature causes no damage. There is no mechanic for "hot climate discomfort" or "breathability bonus."

**Conclusion:** The warmth stat is unidirectional — it only ever helps in cold. A cooling stat (negative warmth reducing overheating risk) is structurally impossible because overheating has no penalty. Setting a negative warmth value would *hurt* cold survival and provide no hot-climate benefit.

---

## The gambeson items

Recipe file: `assets/rudiments/recipes/grid/finecord-gambeson.json`  
Outputs:
- `game:armor-head-sewn-linen` (head slot)
- `game:armor-legs-sewn-linen` (legs slot)

Vanilla definition: `assets/survival/itemtypes/wearable/seraph/armor.json`  
Vanilla warmth: **none set** (armor.json has no `warmth` or `attributesByType` warmth at all).  
This means linen gambeson currently provides exactly 0°C of thermal protection.

---

## Feasible approximations

### Option A — Low POSITIVE warmth (rejected)
Add a small warmth value (e.g. 0.5°C per piece) to `game:armor-*-sewn-linen` via JSON patch.

**Rejected because it is backwards.** `warmth` only helps in the cold, so a *positive* value
makes linen a *better* cold-weather armor — the opposite of the "breathable / cooler" identity.
A *negative* value (`-0.5`) would penalize linen in the cold while doing nothing in heat (no
overheating to mitigate). Neither direction expresses "breathability."

### Option B — wetnessProtection / rainProtectionPerc tweak
Give linen sewn armor a modest `rainProtectionPercByType` value.

**Pros:** Reduces wetness accumulation, which indirectly means less heat loss in rain — arguably "more comfortable"  
**Cons:** This is already handled by shoulder/head clothing layers; armor slot doesn't appear to use it; marginal benefit  

### Option C — Do nothing (IMPLEMENTED)
Leave warmth at 0 and correct only the handbook text.

**Pros:** No false promises; matches actual game behavior (linen is thermally neutral, like
other armor). A nonzero warmth in either direction would misrepresent the fabric.  
**Cons:** Linen has no distinct thermal identity — but that's accurate, since VS gives it none.

---

## Decision

Implemented **Option C** (no mechanical change).

An earlier draft set `warmth: 0.5` on sewn-linen armor, but that is backwards: `warmth` is
unidirectional (only helps in the cold), so a positive value makes linen a *better* cold
armor — the opposite of the intended "breathable, cooler" identity. A negative value would
penalize cold survival while doing nothing in heat. So the correct choice is **no warmth
modifier at all**.

- No patch to `armor-*-sewn-linen` (vanilla warmth 0 retained).
- The "breathability perk" promise in `item-handbooktext-finecord` is removed; the text now
  claims only the durability bonus and makes no climate claim.

**STATUS: BLOCKED — vanilla API has no cooling/overheating mechanic; perk retired, no
mechanical change shipped.**
