# AgeOfFibers — Canvas-Design Sprite Generation
## Opus Orchestration Brief

Feed this document to **Claude Opus** as the opening message of a session.
Opus reads it, then spawns the agents listed in the **Sprite Catalog** in parallel
using the Agent SDK's `Agent()` tool. Each sub-agent has a single job: invoke the
`canvas-design` skill with the brief below, post-process the output to 32×32, and
write it to the target path.

The existing `art-prompts.md` (DALL-E prompts) is kept as a fallback.

---

## Opus: Execution Instructions

```
1. Read this entire document first.

2. Spawn all agents in parallel using a single message with multiple Agent() calls.
   — Each sprite in the Sprite Catalog below = one Agent() call.
   — Exception: the four nettle BUNDLE textures are handled by the Pillow script
     (fix_af_textures.py), not canvas-design; skip them.

3. Model selection per agent:
   — Haiku  (fast, low cost):  stages 1–4, coarsefibers, finecord, linseedcake
   — Sonnet (default):         stages 5–9, finefibers, nettleleaves, linseedoil
   — Sonnet + extended thinking: nettlerhizome, nettlestub, nettlefiber, modicon

4. Each agent prompt must include:
   a) The VS style block (copy verbatim from the STYLE BLOCK section below)
   b) The sprite-specific brief from the Sprite Catalog
   c) The output instructions (copy verbatim from OUTPUT INSTRUCTIONS below)

5. After all agents complete, run the post-processing script:
   uv run python scripts/resize_sprites.py

6. Rebuild the zip:
   uv run python scripts/package_mod.py   (or reuse the inline zip snippet)
```

---

## VS Art Style Block
*(Copy this verbatim into every agent prompt, before the sprite brief.)*

```
You are creating a sprite for the Vintage Story medieval survival game.
Art style rules — follow these exactly:

PALETTE: Use only muted, earthy, desaturated colours.
  — Greens:  olive and sage (#2d3209 shadow, #4c5516 mid, #616b21 base, #8a9c38 highlight)
  — Straw:   grey-olive tan (#75743c dark, #aeaa61 mid, #c1bb70 light)
  — Broken:  grey-olive (#948c5a base)
  — Browns:  weathered clay, never chocolate or orange
  — Rule: if a colour looks vivid on your screen it is too saturated. Desaturate 30–40%.

RENDERING:
  — Hard pixel edges. No anti-aliasing on outlines.
  — Flat cel shading with 3–5 colours maximum per sprite.
  — No gradients. No drop shadows. No glow effects.
  — Transparent background on all item icons and crop sprites.

REFERENCE: The in-game flax bundle textures use #616b21 as their dominant colour —
all greens in this mod must read as the same olive family, not pure grass-green.
```

---

## Output Instructions
*(Copy this verbatim into every agent prompt, after the sprite brief.)*

```
Output format:
  1. Use the canvas-design skill to create the image.
  2. Generate at 512×512 pixels (larger gives better detail before downscale).
  3. Transparent background unless explicitly noted as opaque.
  4. After canvas-design creates the PNG, write it to the exact TARGET PATH shown
     in the brief — overwrite whatever is there.
  5. Do NOT resize yourself; the post-processing script handles 512→32 downscaling.
```

---

## Post-Processing Script

Create `scripts/resize_sprites.py` if it does not exist. Its job is to take any
512×512 (or 1024×1024) PNGs that canvas-design deposited at the target paths and
downscale them to 32×32 (128×128 for the modicon):

```python
#!/usr/bin/env python3
"""
Post-process canvas-design sprite outputs → 32×32 game-ready textures.
Run from VSRudiments root after all canvas-design agents complete:
    uv run python scripts/resize_sprites.py
"""
from PIL import Image
import os

BASE = "Rudiments/assets/rudiments/textures"
MOD_ROOT = "Rudiments"

SPRITES_32 = [
    "block/plant/cropnettle/normal1.png",
    "block/plant/cropnettle/normal2.png",
    "block/plant/cropnettle/normal3.png",
    "block/plant/cropnettle/normal4.png",
    "block/plant/cropnettle/normal5.png",
    "block/plant/cropnettle/normal6.png",
    "block/plant/cropnettle/normal7.png",
    "block/plant/cropnettle/normal8.png",
    "block/plant/cropnettle/normal9.png",
    "block/plant/nettlestub.png",
    "item/food/nettleleaves.png",
    "item/resource/nettlefiber.png",
    "item/resource/nettlerhizome.png",
    "item/resource/coarsefibers.png",
    "item/resource/finefibers.png",
    "item/resource/finecord.png",
    "item/resource/linseedoil.png",
    "item/resource/linseedcake.png",
]

SPRITES_128 = [
    ("modicon.png", MOD_ROOT),  # modicon lives at mod root, not in textures/
]

def process(src_path, target_size):
    if not os.path.exists(src_path):
        print(f"  SKIP (not found): {src_path}")
        return
    img = Image.open(src_path).convert("RGBA")
    if img.size == (target_size, target_size):
        print(f"  already {target_size}px: {src_path}")
        return
    # Strip near-white background (for AI renders on white)
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if r > 230 and g > 230 and b > 230:
                px[x, y] = (0, 0, 0, 0)
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
    img.thumbnail((target_size, target_size), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (target_size, target_size), (0, 0, 0, 0))
    x = (target_size - img.width) // 2
    y = target_size - img.height   # pin to bottom (crops grow from ground)
    canvas.paste(img, (x, y), img)
    canvas.save(src_path)
    print(f"  {target_size}px: {src_path}")

print("Resizing canvas-design outputs…")
for rel in SPRITES_32:
    process(os.path.join(BASE, rel), 32)
for rel, root in SPRITES_128:
    process(os.path.join(root, rel), 128)
print("Done.")
```

---

## Sprite Catalog

Each entry below is one agent. Format:

```
AGENT N — <model>
TARGET PATH: <path relative to textures/ or mod root>
BRIEF:
  <design brief for canvas-design>
```

---

### CROP STAGE SPRITES
*(Plant cross-sprites — transparent background, plant rooted at bottom-centre of frame.
The key visual: a central upright stalk with pairs of opposite serrated nettle leaves
at regular intervals. Leaves point LEFT and RIGHT symmetrically — this is critical
because VS renders the sprite from both directions. Nettle leaves are heart-shaped
with clearly toothed/serrated edges.)*

---

**AGENT 1 — Haiku**
TARGET PATH: `block/plant/cropnettle/normal1.png`
BRIEF:
```
A tiny stinging nettle seedling, 512×512 transparent PNG.
Plant occupies only the bottom 15% of the frame, centred horizontally.
Two small rounded cotyledon seed-leaves on a 3-pixel pale stem. Almost
nothing — just the suggestion of a sprout emerging from nothing.
Muted olive-green #616b21. Hard pixel edges.
```

---

**AGENT 2 — Haiku**
TARGET PATH: `block/plant/cropnettle/normal2.png`
BRIEF:
```
A young stinging nettle plant, 512×512 transparent PNG.
Plant occupies the bottom 25% of the frame, centred horizontally.
Short upright stalk with one symmetrical pair of small toothed oval
leaves pointing left and right. Leaves slightly heart-shaped with
visible serrated edges. Muted olive-green, darker stem.
```

---

**AGENT 3 — Haiku**
TARGET PATH: `block/plant/cropnettle/normal3.png`
BRIEF:
```
A stinging nettle plant at early growth, 512×512 transparent PNG.
Plant fills the bottom 35% of the frame, centred horizontally.
Upright stalk with TWO pairs of opposite toothed leaves — lower pair
larger than upper pair. Heart-shaped serrated leaves, left-right
symmetrical. Muted forest-olive green #616b21, shadow #2d3209.
```

---

**AGENT 4 — Haiku**
TARGET PATH: `block/plant/cropnettle/normal4.png`
BRIEF:
```
A growing stinging nettle, 512×512 transparent PNG.
Plant fills the bottom 48% of the frame, centred horizontally.
Upright stalk, THREE tiers of opposite serrated leaves increasing in
size from top to bottom. Clearly a nettle — heart-shaped leaves with
toothed edges, left-right symmetrical. Muted olive greens only.
NOT a pine tree or fir tree — broad flat leaves, not needles.
```

---

**AGENT 5 — Sonnet**
TARGET PATH: `block/plant/cropnettle/normal5.png`
BRIEF:
```
A mid-growth stinging nettle, 512×512 transparent PNG.
Plant fills the bottom 62% of the frame, centred horizontally.
Stalk with FOUR tiers of opposite serrated nettle leaves. Leaves are
noticeably wide — nettle has broad heart-shaped leaves with clearly
toothed margins. Left-right symmetrical. Muted forest-olive #616b21
body, dark shadow #2d3209 on leaf undersides and between leaves.
```

---

**AGENT 6 — Sonnet**
TARGET PATH: `block/plant/cropnettle/normal6.png`
BRIEF:
```
A tall stinging nettle plant, 512×512 transparent PNG.
Plant fills the bottom 72% of the frame, centred horizontally.
Thick upright stalk, FIVE tiers of broad opposite serrated leaves.
Lower leaves are large and spread wide. Left-right symmetrical.
Darker muted olive overall — this is a mature leafy plant in shade.
Stem is slightly thicker than earlier stages.
```

---

**AGENT 7 — Sonnet**
TARGET PATH: `block/plant/cropnettle/normal7.png`
BRIEF:
```
A mature stinging nettle beginning to flower, 512×512 transparent PNG.
Plant fills the bottom 80% of the frame, centred horizontally.
Tall stalk with five leaf-pair tiers. At the upper leaf axils (where
leaves meet the stem), small drooping wispy greenish-white catkin-like
flower strings hang downward — thin, delicate, 4–6 pixels long. Leaves
are muted dark forest-olive. Flower wisps are pale grey-olive #aeaa61.
```

---

**AGENT 8 — Sonnet**
TARGET PATH: `block/plant/cropnettle/normal8.png`
BRIEF:
```
A mature seed-bearing stinging nettle, 512×512 transparent PNG.
Plant fills the bottom 88% of the frame, centred horizontally.
Tall stalk, five leaf tiers, plus prominent drooping seed clusters
hanging from the upper two leaf axils. Seed clusters are thicker and
heavier than stage 7 wisps — small olive-tan oval seeds on drooping
strings. Stalk muted dark olive, seeds grey-olive tan #aeaa61/#75743c.
```

---

**AGENT 9 — Sonnet**
TARGET PATH: `block/plant/cropnettle/normal9.png`
BRIEF:
```
A fully mature harvest-ready stinging nettle, 512×512 transparent PNG.
Plant fills nearly the full frame height, centred horizontally.
Tall thick stalk, six leaf tiers, heavy drooping seed clusters on the
upper three axils. Lowest leaves beginning to yellow slightly. The
plant looks full, heavy, and slightly past its peak — seeds are
prominent. Dark olive-green fading to straw-olive #aeaa61 at the seed
tips. This is the harvestable stage.
```

---

### ITEM ICON SPRITES
*(Isolated objects on transparent background. Subject should occupy ~70% of the
512×512 frame, centred slightly below vertical midpoint. No background fill.)*

---

**AGENT 10 — Sonnet + extended thinking**
TARGET PATH: `block/plant/nettlestub.png`
BRIEF:
```
A short cut stinging nettle stem stub, 512×512 transparent PNG.
A single small green cylinder 4–5 pixels wide and 8–10 pixels tall,
placed in the lower-centre of the frame. The cut top is slightly
darker, showing the cross-section. Two tiny stub leaves spread out
at the very base. Muted olive-green #616b21. The stub sits low —
this is the root crown left after cutting, small and close to the ground.
Most of the canvas is transparent. Do not add soil or ground.
```

---

**AGENT 11 — Haiku**
TARGET PATH: `item/resource/nettlefiber.png`
BRIEF:
```
A small loose hank of plant bast fibre, 512×512 transparent PNG.
Five to seven fine parallel strands loosely gathered, with a slight
natural waviness — like dried plant stems pulled apart into individual
fibres. Muted dark olive-green #4c5516 to #616b21 — this is dried
nettle bast fibre, darker than linen. The hank sits horizontally
centred, occupying about 60% of the frame width.
```

---

**AGENT 12 — Haiku**
TARGET PATH: `item/resource/coarsefibers.png`
BRIEF:
```
A clump of rough coarse plant fibre, 512×512 transparent PNG.
Short tangled strands going in different directions — visibly rougher
and more matted than fine fibre. Grey-olive tan #948c5a, slightly
dirty and uneven. Clearly low quality — the strands are thick, stiff,
and irregular. Centred in the frame.
```

---

**AGENT 13 — Sonnet**
TARGET PATH: `item/resource/finefibers.png`
BRIEF:
```
A neat small hank of fine combed plant bast fibre, 512×512 transparent PNG.
Smooth parallel strands, evenly aligned in a slight fan shape — clearly
higher quality than coarse fibres. Pale grey-olive straw colour
#aeaa61 to #c1bb70, like cleaned linen thread. The strands are thin,
uniform, and lie neatly together. A simple cord binding wraps the
centre. Centred in frame.
```

---

**AGENT 14 — Haiku**
TARGET PATH: `item/resource/finecord.png`
BRIEF:
```
A short length of tightly two-ply twisted plant fibre cord, 512×512 transparent PNG.
Diagonal orientation (bottom-left to top-right). Clear twist pattern —
the two plies visibly spiral around each other. 4–6 pixels wide at
game scale. Pale straw-tan #aeaa61 with a darker #75743c shadow side
on the twist. The cord is taut and uniform — higher quality than rough
twine. Transparent background.
```

---

**AGENT 15 — Sonnet**
TARGET PATH: `item/resource/linseedoil.png`
BRIEF:
```
A small hand-thrown ceramic earthenware bottle of linseed oil,
512×512 transparent PNG. Rounded bulbous body, short narrow neck, small
clay stopper. Dull grey-brown earthenware exterior — NOT shiny, NOT
modern. The body colour is weathered clay #6a5040. A small amount of
muted amber-brown oil visible at the neck #8a6020 — darker amber,
NOT bright gold. Medieval hand-made pottery. No label, no text.
Centred slightly below midpoint of frame.
```

---

**AGENT 16 — Haiku**
TARGET PATH: `item/resource/linseedcake.png`
BRIEF:
```
A flat round pressed seed cake, 512×512 transparent PNG.
A compact disc about 3× wider than tall — seen from a slight 3/4 angle
so both the top face and a thin side are visible. Top face has a faint
cross-hatch or pressed imprint pattern. Very dark muted brown #5a3010,
slightly rough compressed surface, matte finish. Looks like dried
compressed animal feed or fire-starter, NOT food. Centred in frame.
```

---

**AGENT 17 — Sonnet**
TARGET PATH: `item/food/nettleleaves.png`
BRIEF:
```
A small cluster of 2–3 stinging nettle leaves, 512×512 transparent PNG.
Heart-shaped leaves with deeply serrated / toothed edges and a visible
pale central midrib. Mid to dark muted olive-green #616b21 to #4c5516,
slightly matte surface. The serration is the key identifying feature —
clearly toothed, not smooth. No stems visible, just the leaf cluster.
Centred in frame, leaves slightly overlapping each other naturally.
```

---

**AGENT 18 — Sonnet + extended thinking**
TARGET PATH: `item/resource/nettlerhizome.png`
BRIEF:
```
A knobbly stinging nettle rhizome root cutting, 512×512 transparent PNG.
A short fat root segment, roughly diagonal orientation. PALE COOL
GREY-CREAM colour — like a fresh-cut parsnip or turnip in shade, NOT
yellow, NOT golden, NOT warm. Approximately #b8b0a0 pale grey with
slight warm undertone. Irregular bumpy surface with 2–3 clearly visible
node bumps or growth points along its length. A few tiny pale root hairs.
Muted, cool-toned. Centred in frame.
```

---

### MODICON

**AGENT 19 — Sonnet + extended thinking**
TARGET PATH: `modicon.png` *(at mod root, NOT in textures/)*
OUTPUT SIZE: 128×128 (post-processing script handles this)
BRIEF:
```
A square mod icon for a Vintage Story medieval survival game mod called
"Rudiments", 512×512 PNG (will be downscaled to 128×128).
Design: a square icon split diagonally from top-left corner to
bottom-right corner.
  LEFT/UPPER HALF: pale straw background #c1bb70, a simple illustration
    of a tied flax bundle — pale golden stalks tied with a cord.
  RIGHT/LOWER HALF: dark forest background #2d3209 to #4c5516, a simple
    illustration of a stinging nettle sprig with serrated leaves.
  CENTRE: where the diagonal meets, a small tied bundle of pale bast
    fibre crosses the boundary line.
  TEXT: "Rudiments" in small clean serif lettering at the very bottom,
    centred, in a dark neutral tone.
Muted earthy medieval palette throughout. Flat illustration style,
no gradients, no drop shadows. Readable at small sizes.
```

---

## Skipped sprites (handled by Pillow script — do NOT regenerate)

Run `uv run python scripts/fix_af_textures.py` for these:

| Sprite | Why Pillow |
|---|---|
| `item/resource/nettle/unprocessed.png` | Tiling material texture — needs seamless diagonal stalk pattern |
| `item/resource/nettle/retted.png` | Same |
| `item/resource/nettle/dried.png` | Same |
| `item/resource/nettle/broken.png` | Same |
| `item/resource/flax/*.png` | OppoOtis originals — do not touch |
