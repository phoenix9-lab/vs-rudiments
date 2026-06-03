# AgeOfFibers — Texture Art Pipeline

How to generate new 32×32 pixel art textures for this mod using the local ComfyUI / LoRA pipeline.

---

## What the pipeline is

A local **ComfyUI** instance (`/home/dwhite/comfyui/`) runs **SDXL** with:

| Component | File |
|---|---|
| Base model | `models/checkpoints/sd_xl_base_1.0.safetensors` |
| Pixel art LoRA | `models/loras/pixel-art-xl-v1.1.safetensors` (strength 0.9) |
| Transparent bg | `custom_nodes/ComfyUI-layerdiffuse` + `models/layer_model/layer_xl_transparent_attn.safetensors` |

The workflow (`aof_workflow.json`) generates 1024×1024 RGBA images with transparent backgrounds using LayeredDiffusion — no background removal step needed. A post-processing step then downscales them to 32×32 (or 128×128 for the modicon).

There are two ways to use it: **the automation script** (runs everything in one command) or **manually** through the browser UI.

---

## Method 1 — Automation script (all sprites at once)

### Step 1 — Start the script

```bash
cd /home/dwhite/comfyui
.venv/bin/python generate_aof_sprites.py
```

The script:
1. Starts ComfyUI as a background subprocess
2. Waits for it to be ready
3. Submits all 24 sprite jobs in sequence via the ComfyUI API
4. Downloads each result, downscales it, and writes it directly to the mod texture path
5. Shuts down ComfyUI when done

Output goes straight to:
- `Rudiments/assets/rudiments/textures/<path>.png`
- `Rudiments/modicon.png` (for the modicon)

### Step 2 — Rebuild the mod zip

```bash
cd /home/dwhite/lgr/VSRudiments/Rudiments
VINTAGE_STORY=/mnt/c/Users/danie/AppData/Roaming/Vintagestory \
PATH="/home/dwhite/.dotnet:$PATH" dotnet build -c Release

uv run --project /home/dwhite/lgr/VSRudiments python3 -c "
import zipfile, os
out='../../builds/AgeOfFibers-v2.3.1-vs1.22.zip'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as z:
    for f in ['modinfo.json','modicon.png','bin/Release/Rudiments.dll']:
        z.write(f, os.path.basename(f))
    for dp,_,fns in os.walk('assets'):
        for fn in fns: z.write(os.path.join(dp,fn),os.path.join(dp,fn))
print('done:', out)
"
```

Texture-only changes are a **PATCH** bump — update `modinfo.json` version before building.

### If ComfyUI is already running

Pass `--skip-launch` to skip starting a new instance:

```bash
.venv/bin/python generate_aof_sprites.py --skip-launch
```

---

## Method 2 — Browser UI (single sprite, manual control)

Use this when you want to tweak a single sprite, experiment with prompts, or inspect outputs before committing them.

### Step 1 — Launch ComfyUI

```bash
cd /home/dwhite/comfyui
bash launch.sh
```

Leave this terminal open. ComfyUI is now running at `http://127.0.0.1:8188`.

Open that URL in your **Windows browser** (Edge / Chrome — WSL network is bridged).

### Step 2 — Load the AoF workflow

In the browser UI:

1. Click the **gear icon** (⚙ Settings) → or press **Ctrl+O**
2. Load `aof_workflow.json` from `/home/dwhite/comfyui/`

You'll see a node graph with these nodes connected left-to-right:

```
CheckpointLoader → LoraLoader → LayeredDiffusionApply
                                        ↓
CLIPTextEncode (positive prompt) → KSampler → VAEDecode → LayeredDiffusionDecode
CLIPTextEncode (negative prompt) ↗              ↓
EmptyLatentImage ────────────────     JoinImageWithAlpha → SaveImage
```

### Step 3 — Edit the positive prompt

Double-click the **CLIPTextEncode** node labelled "POSITIVE_PLACEHOLDER" (node 4). Replace the text with your sprite prompt.

**Format:** `[STYLE PREFIX], [subject description]`

**Crop stage style prefix:**
```
pixel art, vintage story crop plant sprite, side view, single plant rooted at bottom center,
tall narrow composition, limited earthy palette, muted desaturated greens, soft top-left light,
crisp hard pixels, no antialiasing, transparent background
```

**Item icon style prefix:**
```
pixel art, vintage story game sprite, single centered object, item icon,
limited earthy palette, muted desaturated natural colors, soft directional light from top-left,
clean readable silhouette, crisp hard pixels, no antialiasing, flat shading, transparent background
```

**Subject descriptions** (from `generate_aof_sprites.py` — the exact prompts the automation uses):

| Sprite | Subject |
|---|---|
| nettle/unprocessed | `bundle of fresh-cut stinging nettle stalks, deep forest green, faint purple-red tint at stem bases, leafy, tied loosely, raw plant material` |
| nettle/retted | `bundle of retted nettle stalks, brownish-olive, slightly damp dull sheen, decomposed greenery, fibrous` |
| nettle/dried | `bundle of dried nettle stalks, pale khaki-green, straw-like, stiff dry stems, bleached` |
| nettle/broken | `bundle of broken nettle stalks, shredded straw texture, pale tan with faint green tinge, splintered fibres separating from core` |
| nettlefiber | `loose hank of nettle bast fibre, fine combed strands, dark muted green, subtle silky lustre, soft natural fibre` |
| coarsefibers | `loose clump of coarse plant fibre, rough uneven strands, dull greyish-tan, stiff and matted, low quality` |
| finefibers | `neat hank of fine combed bast fibre, smooth uniform strands, pale golden-green, soft lustre, high quality` |
| finecord | `short length of tightly twisted fine fibre cord, even spiral twist, pale tan-green, taut and strong, two-ply` |
| nettleleaves | `cluster of 2 to 3 serrated heart-shaped stinging nettle leaves, mid-to-dark matte green, visible central midrib and toothed edges, single dew drop` |
| linseedoil | `small ceramic vial of amber linseed oil, warm golden translucent liquid, cork stopper, glazed earthenware, slight highlight` |
| linseedcake | `flat round pressed seed cake, dark brown to tan, rough cross-hatched top surface, compressed oilseed pulp disc, matte` |
| nettlerhizome | `knobbly pale yellow nettle rhizome root, segmented with small node buds, thin pale root hairs, fresh dug, dirt flecks` |
| nettlestub | `short cut nettle stem stub, severed green stalk close to the ground, frayed top cut, couple of small basal leaves` |
| cropnettle/normal1 | `tiny nettle seedling, two small rounded seed-leaves on a short pale stem, very small, only bottom third of frame` |
| cropnettle/normal2 | `nettle seedling, first pair of small toothed true leaves, short pale-green stem` |
| cropnettle/normal3 | `young nettle, two opposite pairs of serrated heart-shaped leaves, light fresh green, low` |
| cropnettle/normal4 | `growing nettle, three tiers of opposite serrated leaves on an upright green stem, light green` |
| cropnettle/normal5 | `taller nettle, four tiers of opposite serrated leaves, mid green, fuller` |
| cropnettle/normal6 | `mid-growth nettle, larger jagged leaves, faint fine stinging hairs on the stem, mid-to-dark green` |
| cropnettle/normal7 | `mature tall nettle stalk, thin drooping greenish flower-cluster wisps at the top, dark green` |
| cropnettle/normal8 | `mature nettle, tall stalk with stringy hanging greenish flower and seed clusters at the leaf axils` |
| cropnettle/normal9 | `fully grown nettle, tallest, heavy hanging brown-green seed clusters, slightly yellowing fibrous stem, ready to harvest` |

### Step 4 — Change the seed (KSampler node 7)

The **seed** controls which image you get. Same prompt, different seed = completely different result. If a sprite looks bad, just change the seed and re-run.

- Click the **KSampler** node (node 7)
- Change the `seed` value — any integer works
- Or click the 🎲 randomise button next to the seed field

### Step 5 — Queue the generation

Click **Queue Prompt** (top-right of UI). Generation takes ~10–30 seconds on GPU.

The output appears in the **SaveImage** node preview and is saved under:
`/home/dwhite/comfyui/output/aof/`

### Step 6 — Post-process and copy to mod

The output is 1024×1024. You need to downscale and copy it:

```python
# Run from anywhere — adjust paths
from PIL import Image
import numpy as np

src  = "/home/dwhite/comfyui/output/aof/YOUR_OUTPUT.png"
dest = "/home/dwhite/lgr/VSRudiments/Rudiments/assets/rudiments/textures/item/resource/nettlefiber.png"
size = 32   # 128 for modicon

img = Image.open(src).convert("RGBA")

# Normalize alpha (LayeredDiffusion can produce soft alpha < 255 at max)
arr = np.array(img, dtype=np.float32)
a_max = arr[:,:,3].max()
if a_max > 0:
    arr[:,:,3] = np.clip(arr[:,:,3] / a_max * 255, 0, 255)
img = Image.fromarray(arr.astype(np.uint8), "RGBA")

# Downscale
img = img.resize((size, size), Image.Resampling.LANCZOS)

# Hard alpha threshold — removes blended-edge fuzz at 32px
arr2 = np.array(img)
arr2[:,:,3] = np.where(arr2[:,:,3] > 5, 255, 0)
img = Image.fromarray(arr2, "RGBA")

img.save(dest)
print("saved", dest)
```

Or use the workspace resize script (if you dropped the 1024px file at the correct target path):

```bash
cd /home/dwhite/lgr/VSRudiments
uv run python scripts/resize_sprites.py
```

---

## What went wrong last time (likely causes)

The previous run was described as "a disaster." Common failure modes with this setup:

**Bad output quality at 32×32**
The LANCZOS downscale from 1024→32 loses almost all detail. Subjects that look good at 1024px can become unreadable blobs at 32px. The crop stage sprites are especially sensitive because fine leaf detail disappears. Fixes to try: stronger LoRA weight (bump to 1.0–1.1), higher CFG (try 7.5–8.5), more explicit size constraints in the prompt (`"fills bottom half of frame"`, `"rooted at center-bottom"`).

**Alpha normalization missing**
LayeredDiffusion outputs soft alpha — the max value in the alpha channel is often 180–200, not 255. Without the normalization step in `pixelize()`, items look washed-out or mostly transparent after downscaling. The script handles this; the manual step above includes it.

**ComfyUI timeout**
The script waits up to 600 seconds per sprite. If the GPU stalls or a node errors, the script raises `RuntimeError`. Re-run with `--skip-launch` if ComfyUI is still up.

**Missing LayeredDiffuse node**
The `ComfyUI-layerdiffuse` custom node must be installed. It's at `/home/dwhite/comfyui/custom_nodes/ComfyUI-layerdiffuse` and the model files are in `models/layer_model/`. If ComfyUI complains about a missing node type on workflow load, this node wasn't installed correctly — reinstall via ComfyUI Manager.

---

## VS palette reference

Output will not automatically match the VS palette. After generation, check against:

| Role | Target hex |
|---|---|
| Greens (shadow → highlight) | `#2d3209` → `#4c5516` → `#616b21` → `#8a9c38` |
| Straw / dried fibre | `#75743c` · `#aeaa61` · `#c1bb70` |
| Browns (clay) | Weathered, never orange or chocolate |

The existing flax bundle textures (`textures/item/resource/flax/`) are the ground truth.
If a generated sprite has colours that are too vivid compared to those, either try a new seed
or add `"desaturated, earthy tones"` to the prompt and re-run.

---

## Do not touch

`textures/item/resource/flax/*.png` — these are the original OppoOtis AgeOfFlax textures and must not be replaced.
