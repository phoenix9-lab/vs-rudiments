# Rudiments — AI Art Prompts (OpenAI Image Generation)

Prompts for **DALL-E 3** (ChatGPT free tier) or **GPT-4o image generation**.
Paste each prompt directly into ChatGPT's image generation box. One prompt = one image.

---

## Workflow

1. **Generate** at ChatGPT's default size (1024×1024).
2. **Downscale** to 32×32 (128×128 for the modicon) using nearest-neighbour — never bicubic, never "auto":
   ```python
   from PIL import Image
   img = Image.open("generated.png").convert("RGBA")
   img = img.resize((32, 32), Image.Resampling.NEAREST)
   img.save("output.png")
   ```
3. **Background removal**: for items that need a transparent background, either ask ChatGPT
   to generate on a solid **hot-pink #FF00FF** background and chroma-key it in Aseprite,
   or ask directly for a transparent/white background and erase in Aseprite.
4. **Optional cleanup**: a 5-minute pass in Aseprite to tighten silhouette edges and enforce
   the alpha boundary makes a noticeable difference, especially on crop sprites.

---

## VS colour palette reference

Look at the existing flax bundle textures in `textures/item/resource/flax/` — those are
the gold standard. Key rules:

- **Greens**: muted sage and forest green — think lichen or dried herbs, NOT grass-green or lime.
  Hex ballpark: `#3d6b1a` to `#5a8c28`.
- **Straw / dried fibre**: pale warm grey-tan, NOT golden yellow. Think aged linen or dry hay in
  shade. Hex ballpark: `#b8a868` to `#d4c890`.
- **Browns**: weathered clay and wood — dull, slightly grey. NOT chocolate-brown or amber.
- **Rule of thumb**: if a colour looks vivid on screen it is too saturated. Desaturate 30–40%.
- **Palette size**: 3–5 colours per texture. Hard pixel edges, no gradients.

---

## Priority order (use your free credits here first)

| # | Asset | Impact |
|---|---|---|
| 1 | Crop stages 1–9 | Highest — seen every time nettle grows in the world |
| 2 | Nettle bundle ×4 | High — seen every processing step |
| 3 | Nettleleaves, nettlefiber | Medium — common inventory items |
| 4 | Coarsefibers, finefibers, finecord | Medium — end-product items |
| 5 | Linseedoil, linseedcake | Low-medium |
| 6 | Rhizome, stub, modicon | Low — rare or UI-only |

---

## How to use these prompts

**Each `###` heading = one image = one ChatGPT message.** The `##` section headers are
just organisation — don't paste those.

Each prompt contains `[STYLE PREFIX]` as a placeholder. Replace that with the shared
style text below, or use the **session trick** to avoid doing it every time:

> **Session trick (recommended):** At the start of a ChatGPT conversation, send:
> *"For all images I'm about to request, apply this style: [paste the SHARED STYLE PREFIX
> below]. I'll send individual descriptions one at a time."*
> Then just paste each description on its own — ChatGPT keeps the style for the whole session.

---

## SHARED STYLE PREFIX

Paste this at the start of your session (see above), or replace `[STYLE PREFIX]` with it
in each individual prompt:

> Vintage Story medieval survival game sprite. Muted, earthy, desaturated colour palette —
> no bright or saturated colours. Hard pixel edges, flat cel shading, no gradients,
> no anti-aliasing, no drop shadows, no outlines heavier than 1 pixel, no background
> unless specified. 3 to 5 colours maximum. Clean readable silhouette at 32×32 pixels.

---

## 1 · Crop stage sprites (32×32, transparent background, plant grows from bottom)

These appear on VS cross-shaped crop geometry — two intersecting vertical planes.
Draw the plant as a **side-on silhouette**, rooted at the **bottom centre** of the frame.
The plant must read clearly from any cardinal direction, so leaves should be symmetrical
left-right. All transparent except the plant pixels.

### normal1 — seedling
> [STYLE PREFIX] A tiny stinging nettle seedling, 6 pixels tall, rooted at the bottom
> centre of a 32×32 transparent frame. Two small rounded cotyledon leaves on a pale green
> stalk. Muted sage green. Almost nothing visible — just a tiny sprout.

### normal2 — first true leaves
> [STYLE PREFIX] A young stinging nettle plant, 10 pixels tall, rooted at the bottom centre
> of a 32×32 transparent frame. Short pale-green stalk, one pair of small toothed oval leaves
> pointing left and right symmetrically. Muted sage green.

### normal3 — two leaf pairs
> [STYLE PREFIX] A stinging nettle plant, 14 pixels tall, rooted at the bottom centre of a
> 32×32 transparent frame. Thin upright stalk with two pairs of opposite toothed leaves, lower
> pair slightly larger. Muted forest green, leaves slightly darker than the stalk.

### normal4 — three leaf pairs
> [STYLE PREFIX] A stinging nettle plant, 18 pixels tall, rooted at the bottom centre of a
> 32×32 transparent frame. Upright stalk, three tiers of opposite toothed leaves increasing in
> size from top to bottom. Muted medium green. The silhouette should look like a simple
> cross shape with three horizontal arm pairs.

### normal5 — four leaf pairs, fuller
> [STYLE PREFIX] A stinging nettle plant, 21 pixels tall, rooted at the bottom centre of a
> 32×32 transparent frame. Four tiers of opposite toothed leaves on an upright stalk, leaves
> noticeably wider than earlier stages. Muted mid-green with slightly darker leaf edges.

### normal6 — tall, five leaf pairs
> [STYLE PREFIX] A stinging nettle plant, 24 pixels tall, rooted at the bottom centre of a
> 32×32 transparent frame. Five tiers of opposite serrated leaves on a slightly thick green
> stalk. Leaves are broad and toothed. Dark muted forest green. The silhouette is a clear
> ladder of leaf pairs from bottom to top.

### normal7 — mature, flower wisps beginning
> [STYLE PREFIX] A mature stinging nettle plant, 27 pixels tall, rooted at the bottom centre
> of a 32×32 transparent frame. Five tiers of opposite serrated leaves plus small drooping
> wispy flower-cluster strings hanging from the top leaf axils. Muted dark green, flower
> wisps slightly yellow-green and thin.

### normal8 — heavy seed clusters
> [STYLE PREFIX] A mature stinging nettle plant, 29 pixels tall, rooted at the bottom centre
> of a 32×32 transparent frame. Five leaf-pair tiers plus prominent drooping catkin-like seed
> clusters hanging from near the top. Seeds are small olive-green clusters on drooping strings.
> Dark forest green stalk and leaves, olive-tan seeds.

### normal9 — fully ripe, ready to harvest
> [STYLE PREFIX] A fully-grown stinging nettle ready for harvest, 31 pixels tall, rooted at
> the bottom centre of a 32×32 transparent frame. Heavy drooping seed clusters hanging from
> the upper leaf axils, slight yellowing on the lowest leaves. The silhouette is tall and
> narrow with a top-heavy cluster of hanging seeds. Muted dark green fading to pale olive-tan
> at the seed ends. This is the harvestable stage — it should look full and slightly past peak.

---

## 2 · Nettle bundle material textures (32×32, NO transparent background — solid tile)

These are tiled onto a 3D bundle mesh. They should look like a dense wall of parallel plant
stalks viewed from the side — no isolated object, fill the entire 32×32. Match the visual
style and density of the existing flax bundle textures (`textures/item/resource/flax/`).
No binding cord needed — the 3D mesh handles that.

### nettlebundle-unprocessed
> [STYLE PREFIX] 32×32 seamless texture tile of dense fresh stinging nettle stalks packed
> tightly side by side, viewed from the side, filling the entire frame with no gaps. Stalks
> are slightly oval in cross section. Deep muted forest green, darker on shadow sides,
> lighter highlight stripe on one edge per stalk. Faint reddish-brown tint at the very base
> of some stalks. Solid background — no transparency. Matches the density and visual style
> of packed flax bundles.

### nettlebundle-retted (after retting in water or field)
> [STYLE PREFIX] 32×32 seamless texture tile of densely packed retted nettle stalks, filling
> the entire frame. Same stalk-bundle density as unprocessed but colour has changed: muted
> brownish olive-green, slightly damp and softened looking, fibres just beginning to separate.
> Think decomposing plant material — dull, slightly grey-green with hints of brown.
> Solid background, no transparency.

### nettlebundle-dried (after drying on rack)
> [STYLE PREFIX] 32×32 seamless texture tile of densely packed dried nettle stalks filling
> the entire frame. Same stalk-bundle density as before but now pale and straw-coloured —
> muted grey-tan with a very faint greenish tinge, like aged linen or dried hay in shadow.
> NOT golden yellow — deliberately dull and pale. Solid background, no transparency.

### nettlebundle-broken (after breaking)
> [STYLE PREFIX] 32×32 seamless texture tile of broken dried nettle stalks filling the
> entire frame. Similar to the dried variant but fibres are visibly split and fraying — the
> stalks have splintered lengthwise exposing pale inner fibres. Colour is pale straw with
> some slightly lighter splintered streaks. Very slightly more chaotic than the dried
> version. Solid background, no transparency.

---

## 3 · Item icons (32×32, isolated object, transparent or magenta background)

For each icon: generate on a **solid hot-pink (#FF00FF) background**, then chroma-key it out
in Aseprite or a similar editor. The object should occupy roughly 70–80% of the 32×32 frame,
centred slightly below the vertical midpoint.

### nettleleaves
> [STYLE PREFIX] A small cluster of 2–3 stinging nettle leaves as a 32×32 item icon on a
> solid magenta background. Heart-shaped leaves with clearly serrated / toothed edges and a
> visible central midrib. Mid to dark muted green, slightly matte. No stems visible — just
> the leaf cluster. The silhouette must be clearly identifiable as a toothed leaf at 32×32.

### nettlefiber
> [STYLE PREFIX] A small loose hank of plant bast fibre as a 32×32 item icon on a solid
> magenta background. Fine parallel strands loosely gathered, slight natural waviness.
> Muted dark green — think dried nettle bast, similar colour to dried nettles.
> The strand texture should read clearly at 32×32.

### coarsefibers
> [STYLE PREFIX] A small clump of rough coarse plant fibre as a 32×32 item icon on a solid
> magenta background. Short tangled strands, visibly rougher and more matted than fine fibre.
> Dull grey-tan, slightly dirty looking. Low quality, rough texture. Clearly different from
> fine fibre in appearance.

### finefibers
> [STYLE PREFIX] A neat small hank of fine combed plant bast fibre as a 32×32 item icon on a
> solid magenta background. Smooth parallel strands, evenly aligned. Pale muted straw colour
> with a very faint warm tone — like cleaned linen thread. Clearly higher quality than coarse
> fibres — neater, more uniform.

### finecord
> [STYLE PREFIX] A short length of tightly twisted two-ply plant fibre cord as a 32×32 item
> icon on a solid magenta background. Diagonal orientation, clear twist pattern visible.
> Pale muted straw-tan colour, slightly darker on shadow side of the twist.
> About 4–5 pixels wide. The twist should be readable at 32×32.

### linseedoil
> [STYLE PREFIX] A small ceramic earthenware vial or stoppered bottle of linseed oil as a
> 32×32 item icon on a solid magenta background. Simple rounded body, short narrow neck,
> small cork or clay stopper. Dull grey-brown earthenware exterior. The oil colour visible
> at the top or through the neck should be a muted amber-brown, not bright gold.
> Medieval, hand-made pottery look. No label.

### linseedcake
> [STYLE PREFIX] A flat round pressed seed cake as a 32×32 item icon on a solid magenta
> background. A compact disc roughly 20×8 pixels — wider than tall, seen from a slight
> angle. Dark muted brown, slightly rough compressed surface. A faint cross-hatch or
> pressed pattern on the top face. Looks like dried compressed animal feed or fire-starter.

### nettlerhizome
> [STYLE PREFIX] A knobbly pale plant rhizome root cutting as a 32×32 item icon on a solid
> magenta background. A short fat segment of root, roughly horizontal or diagonal.
> **Pale grey-cream colour** — like a fresh-cut parsnip or ginger root in shade, NOT yellow
> or golden. Irregular surface with 2–3 visible node bumps or growth points. A few tiny
> root hairs. Muted, cool-toned pale cream.

---

## 4 · Block textures (32×32, solid or transparent as noted)

### nettlestub (transparent background)
> [STYLE PREFIX] A short cut stinging nettle stem stub as a 32×32 sprite on a transparent
> background. A single small green cylinder 4–5 pixels wide and 8–10 pixels tall, centred
> in the lower half of the frame. The cut top shows a slightly darker cross section. Two
> tiny stub leaves at the base. Muted dark green. This represents the root crown left
> after cutting nettle — small, low to the ground, mostly empty frame above it.

---

## 5 · Modicon (128×128, the mod's thumbnail icon)

> Vintage Story video game mod icon, 128×128 pixels. A square icon split diagonally from
> top-left to bottom-right. Upper-left half: pale straw/linen background with a simple
> illustration of a flax plant bundle — pale golden stalks. Lower-right half: dark muted
> forest green background with a simple illustration of a stinging nettle sprig — serrated
> dark green leaves. A small tied bundle of pale bast fibre crosses the diagonal split at
> the centre. Text "Rudiments" in small clean serif lettering at the bottom.
> Muted, earthy medieval colour palette. Flat illustration style, no gradients,
> no drop shadows. Readable at small sizes.

---

## Notes

- **nettlefiber** is currently acceptable — lower priority than the others.
- **Flax textures** (`textures/item/resource/flax/`) are from OppoOtis's original AgeOfFlax
  and must NOT be changed.
- After replacing any texture, rebuild and repackage the mod zip (see CHANGELOG.md checklist).
  Texture-only changes are a **PATCH** bump (e.g. 2.2.1 → 2.2.2).
