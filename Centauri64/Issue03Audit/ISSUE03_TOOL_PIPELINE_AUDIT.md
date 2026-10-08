# Issue #3 — Sprite tool pipeline audit

**Phase 1** — inspect and report (original section below).  
**Phase 2** — Image → Sprite bridge implemented: restricted Sprite Artwork mode when `FeatureId.Sprites` is unlocked; Sprite Editor `I` / `B` bind-and-bake; optional `IMAGE` metadata in `.sprites`; playback remains baked pixels. See `--verify-sprite-pipeline`.

Target magazine: **SPRITES!** / cover game **Ghost Catcher**.  
Desired architecture: **Image Editor → Sprite Editor → BASIC runtime**.  
Do not introduce a second pixel-art editor as the long-term model.

---

## Verdict (short)

Today Centauri64 has **two independent pixel pipelines**:

1. **Image Editor** (`ImageAsset` / `.images`) — multi-size, multi-frame artwork with transparency and categories (including SPRITE).
2. **Sprite Editor** (`SpriteAsset` / `.sprites`) — fixed **16×16** hardware-sprite frames with its **own** painted pixels.

They do **not** share image data. The Sprite Editor cannot select Image artwork. Animations **copy frame pixels**, they do not reference image frames. Docs (`CREATIVE-TOOLS-IMAGES.md`, `ImageAsset` comments) describe a future “Sprite Builder” consuming SPRITE images — **not built**.

**BASIC sprite display/animation and tape SAVE/LOAD for `.sprites` already work** if pixels were authored inside the Sprite Editor (or shipped sidecars).

Career unlocks **Sprites at Issue 3** and **Images at Issue 5**, so Bedroom players get the Sprite Designer **before** the Image Editor — the opposite of the intended Image→Sprite teaching order unless Issue #3 also opens a limited artwork path.

---

## 1. Current asset architecture

### Image assets

| Piece | Location | Notes |
|-------|----------|--------|
| `ImageAsset` | `Machine/Images/ImageAsset.cs` | Named; mode; W×H; `ImageCategory`; 1–64 `ImageFrame`s |
| `ImageFrame` | `Machine/Images/ImageFrame.cs` | Own `int[,]` pixels; `Revision`; default fill `Transparent = -1` |
| `ImageAssetStore` | `Machine/Images/ImageAssetStore.cs` | In-memory tape store |
| `ImageStorage` | `Machine/Images/ImageStorage.cs` | `{NAME}.images` text sidecar |
| Categories | `ImageCategory.cs` | General / Sprite / Tileset / Background |

Images are **variable size** (validated against mode max 640×480 or 320×240). **16×16 is a template default**, not a hard requirement for SPRITE-category images. **32×32 “SPRITE” templates are still images**, not hardware sprites (`ImageTemplates.cs`, SPEC §5.2).

### Sprite assets (hardware)

| Piece | Location | Notes |
|-------|----------|--------|
| `SpriteAsset` | `Machine/SpriteAsset.cs` | Named; dictionary of `SpriteAnimation` |
| `SpriteAnimation` | `Machine/Sprites/SpriteAnimation.cs` | Name + list of `SpriteFrame` — **no speed, no loop fields** |
| `SpriteFrame` | `Machine/Sprites/SpriteFrame.cs` | Fixed **16×16** `int[,]`; clear to `CentauriSprite.TRANSPARENT` (-1) |
| `SpriteAssetStore` | `Machine/Sprites/SpriteAssetStore.cs` | In-memory tape store |
| `SpriteStorage` | `Machine/Sprites/SpriteStorage.cs` | `{NAME}.sprites` text sidecar |
| Runtime slot | `Machine/CentauriSprite.cs` | Copies pixels from asset frames; anim name, loop flag, timer |

### Shared vs duplicated

- **Duplicated.** No `ImageAsset` / frame index on `SpriteFrame` or `SpriteAnimation`.
- Editing an image **does not** update sprites.
- Runtime `CopyFrameToSprite` copies sprite-asset pixels again into the hardware slot.
- Maps bind tiles to **sprite** asset `DEFAULT` frame 0 (not images) — `CentauriMachine.Tiles.cs`.

### Identity / selection

| Domain | Identity |
|--------|----------|
| Images | **Name** (normalized uppercase, letter-start, A–Z0–9_, ≤16) |
| Sprites | **Name** (editor entry A–Z0–9, ≤12; store lookup by name) |
| UI browse | Sprite Editor Left/Right over **sorted asset list** (index only for navigation, not an API) |
| BASIC | `SPRITE i, "NAME"` — **string name**, hardware slot index `i` |

No stable GUID / asset-ID system. Prefer keeping **name-based** selection for Issue #3 (avoid numeric picker UIs).

### Serialization

Tape folder: `%LocalAppData%\Centauri64\Tapes` (`TapeFolder`).

| Sidecar | Content |
|---------|---------|
| `.bas` | Program |
| `.tape` | Label metadata |
| `.sprites` | `SPRITE` / `ANIMATION` / `FRAME` + 16 CSV rows × 16 cols (palette or `-1`) |
| `.images` | `IMAGE` / `MODE` / `SIZE` / `CATEGORY` / `FRAMES` / `FRAME` + CSV rows |
| `.maps` / `.cover` | Separate |

`SAVE` / `LOAD` (`BasicMachine.Storage.cs`) always attempt sprites, maps, and images together.

`SoftwareGrant.GrantTapes` copies `.bas/.tape/.cover/.sprites/.maps` — **not `.images`**. Relevant if cover games later ship image sidecars.

---

## 2. Current Image Editor workflow

**Open:** F7 → `TryOpenAuthoringTool(FeatureId.Images, …)` → `ImageEditor.Open()`  
**Gate:** Issue **5** (`FeatureId.Images`) in Bedroom; Hardcore unrestricted.

| Capability | Status |
|------------|--------|
| New image wizard | **Works** — default template index = **16×16 SPRITE**, category Sprite, transparent fill |
| Other sizes | **Works** — 8×8 tile, 32×32 sprite, 64×64, illustration, wide, full-screen BG, custom |
| Pencil / eraser / fill / shapes / pick | **Works** — eraser → transparent |
| Multi-frame (new / duplicate / delete / navigate) | **Works** — max 64 frames |
| Category filter in browser | **Works** |
| Undo/redo, zoom, grid | **Works** |
| Persist | **Works** via tape `SAVE` → `.images` (dirty notice: save tape to keep) |
| In-editor standalone Save to disk | **Missing** (by design: tape-centric) |

Artwork for sprites **can** be created here today, but nothing consumes it for hardware sprites.

---

## 3. Current Sprite Editor workflow

**Open:** F5 → `TryOpenAuthoringTool(FeatureId.Sprites, …)` → `SpriteEditor.Open()`  
**Gate:** Issue **3** (`FeatureId.Sprites`).

| Capability | Status |
|------------|--------|
| New sprite | **Works** — creates `SpriteAsset`, auto `DEFAULT` animation, one blank frame |
| Cannot delete DEFAULT | **Works** |
| Extra animations | **Works** — add / copy / delete (not last; not DEFAULT) |
| Paint 16×16 frames | **Works** — local pixels; right-click transparent |
| Add / duplicate / delete frames | **Works** (cannot delete last frame) |
| Preview playback | **Works** — fixed `PreviewFrameTime = 0.125f`, always loops if >1 frame |
| Onion skin | **Works** |
| Select Image artwork by name | **Missing** |
| Bind animation frames to image frames | **Missing** |
| Per-animation speed in editor | **Missing** (hardcoded 0.125 s everywhere) |
| Per-animation loop toggle in editor | **Missing** (runtime `SPRITEANIM …, 0|1` only) |
| Persist | **Works** via tape `SAVE` → `.sprites` |

Sprite Editor is currently a **second full pixel editor**, not a thin animation layer over Image assets.

---

## 4. Existing BASIC sprite integration

| Command / API | Status |
|---------------|--------|
| `SPRITE i,"NAME"` | **Works** — loads DEFAULT, loop on, visible, frame 0 |
| `SPRITEPOS` / `SHOW` / `HIDE` / `FLIP` | **Works** |
| `SPRITEANIM i,"ANIM",loop` | **Works** — loop 0 = one-shot then previous; 1 = loop |
| `COLLIDE(a,b)` / `ANIMPLAYING(…)` | **Works** — gated Sprites |
| Frame rate | Fixed **0.125 s** in `UpdateSprites` — no BASIC speed arg |
| Hardware size | Fixed **16×16**, 64 slots |
| Reload after SAVE/LOAD | **Works** for `.sprites` pixel data |

Sprites are **not** cleared by `CLS`. Presentation burst treats sprite statements as `ScreenPresentation.Sprite`.

---

## 5. Desired 13-step journey — status

| # | Step | Status | Notes |
|---|------|--------|-------|
| 1 | Create 16×16 image with transparency | **Works** (Image Editor) | Needs `FeatureId.Images` (Issue 5) or Hardcore |
| 2 | Add a second frame | **Works** | Image Editor frames |
| 3 | Save the artwork | **Works** | Tape SAVE → `.images` |
| 4 | Open Sprite Editor | **Works** | F5 / Issue 3 |
| 5 | Select the artwork | **Missing** | No image picker; sprites are separate assets |
| 6 | Define DEFAULT using both frames | **Partial** | DEFAULT auto-created, but frames are **painted copies**, not image frames |
| 7 | Set playback speed and looping | **Missing** (editor) | Speed fixed; loop only via BASIC `SPRITEANIM` |
| 8 | Preview animation | **Works** | Uses sprite’s own frames @ 0.125 s |
| 9 | Save sprite definition | **Works** | Tape SAVE → `.sprites` (baked pixels) |
| 10 | Return to BASIC | **Works** | |
| 11 | Display and animate | **Works** | If sprite pixels exist in `.sprites` |
| 12 | Save, close, reload tape | **Works** | Independent sidecar reload |
| 13 | Animation still works | **Works** | For baked `.sprites` data |

**End-to-end Image→Sprite→BASIC journey: broken at steps 5–7** (and career order blocks step 1 for Bedroom Issue-3-only players).

---

## 6. Architecture answers

| Question | Answer |
|----------|--------|
| Do editors share image data? | **No** — duplicate pixel buffers |
| Sprite→artwork reference? | **By sprite name only**; no image link |
| Edit image updates animation? | **No** |
| Animations reference frames or copy pixels? | **Copy pixels** into `SpriteFrame` / runtime slot |
| 16×16 enforced? | **Yes** for hardware sprites; **optional template** for images |
| Transparency preserved? | **Yes** within each pipeline (`-1`) |
| DEFAULT created automatically? | **Yes** on new sprite |
| Existing sprite assets migrate safely? | **Yes today** (self-contained `.sprites`). Future shared refs must keep loading baked FRAME data |
| Feature gates tools + BASIC authoring? | **Yes** — tools via `TryOpenAuthoringTool`; BASIC via `EnsureAuthoringFeature` on **every** executed statement |
| Hardcore unrestricted? | **Yes** — `AllReleased()` |
| Locked-feature tapes still playable? | **Documented yes, implemented no** — see §7 |

---

## 7. Career feature-gating status

| Feature | Magazine | Tool | BASIC |
|---------|----------|------|-------|
| Graphics | Issue 2 | — | PLOT/LINE/RECT/CIRCLE |
| **Sprites** | **Issue 3 SPRITES!** | **F5 Sprite Designer** | SPRITE* / COLLIDE / ANIMPLAYING |
| Maps | Issue 4 | F6 Map Editor | Map/tile commands |
| **Images** | **Issue 5 PICTURE THIS!** | **F7 Image Editor** | IMAGE / BG / FG |

Sources: `MagazineCatalog.cs`, `FeatureGate.cs`, `FeatureId.cs`, `GameSession`, `CAREER.md`.

**Hardcore:** all released tools and statements available; magazines are reference-only.

### Playability gap (important)

- `FeatureId` / `CAREER.md` / SPEC intent: **authoring** is gated; **playing saved software** that uses locked capabilities should still work.
- **Code:** `Interpreter.Execute` always calls `EnsureAuthoringFeature`. There is **no play-bypass**. A Bedroom player without Issue 3 who `LOAD`/`RUN`s a sprite tape gets `SPRITE GRAPHICS NOT YET AVAILABLE`.

Ghost Catcher as a **purchased cover tape** is fine (purchase grants `FeatureId.Sprites`). Sharing / earlier-career play of sprite tapes is blocked contrary to docs.

---

## 8. Save/load compatibility concerns

1. **Existing `.sprites`** — baked 16×16 frames; must keep loading unchanged (Ghost Catcher / HAG / player tapes).
2. **New image-linked sprites** — if `.sprites` gains `IMAGE`/`FRAME` refs, either:
   - keep emitting baked `FRAME` pixels (compatible), or
   - dual-read: prefer refs when present, else pixels.
3. **`.images` grant** — `SoftwareGrant` omits `.images`; cover games that need image sidecars will not install them until grant is extended (Issue 5 / cover packaging).
4. **Name rules differ** — image names vs sprite names (length / charset). Binding UI must validate both.
5. **Non-16×16 SPRITE images** — 32×32 templates exist; hardware sprites cannot use them without crop/scale policy (Issue #3 should **enforce 16×16** for bindable sprite artwork).
6. **Map tiles** — still keyed off sprite assets; do not break when changing sprite internals.

---

## 9. Gaps against desired workflow

### Essential for Issue #3 (Ghost Catcher + teaching SPRITES!)

1. **Artwork path at Issue 3** without waiting for Issue 5’s full Image Editor:
   - Prefer: unlock **Image Editor in a sprite-artwork mode** (SPRITE category / 16×16 templates only) when `FeatureId.Sprites` is granted, **or** grant a narrow `Images` subset with Sprites.
   - Avoid teaching a permanent second paint tool as the curriculum end-state; Sprite Editor paint can remain as transitional fallback for old tapes.
2. **Sprite Editor consumes named Image assets** (no numeric index picker): choose artwork by **name**, pick which **image frames** build DEFAULT (and other anims).
3. **Persist sprites in a compatible way** (bake pixels into `.sprites` and/or store name+frame refs with bake-on-save).
4. **DEFAULT** remains auto-created (already OK).
5. **Preview** continues to work from the resolved frame list.
6. **BASIC path unchanged** for magazine listings (`SPRITE` / `SPRITEANIM` / collision).
7. **Decide play-bypass:** implement documented “tapes still run” behaviour, or explicitly revise CAREER/SPEC for Issue 3 (cover-tape-only unlock may be enough short-term).
8. **Magazine copy** teaches: draw frames → define animation → BASIC — not “paint twice in two editors.”

### Can wait until Issue #4 (maps / tiles)

- Map Editor consuming **TILESET** `ImageAsset` frames (today tiles use sprite frame 0).
- Tile animation.
- Broader asset browser shared with maps.

### Can wait until Issue #5 (full Image Editor)

- Full illustration / background / multi-size teaching as primary curriculum.
- `IMAGE` / `BG` / `FG` BASIC for Ghost Catcher (not required if cover is sprite-only).
- Granting `.images` on cover tapes (unless Ghost Catcher ships images early).
- Removing Sprite Editor pixel painting entirely (optional cleanup after shared model is solid).

### Explicitly out of scope / avoid

- Generic asset database / large editor framework.
- Second competing pixel-art product.
- Changing hardware sprite size or adding float animation systems.
- Index-based asset pickers in UI.

---

## 10. Recommended minimum changes for Issue #3

**Goal:** Bedroom Issue-3 players can create 16×16 transparent multi-frame artwork, bind it into a sprite’s DEFAULT animation, preview, SAVE, and animate in BASIC — with **existing `.sprites` still loading**.

### Recommended shape (smallest coherent)

1. **Career:** When Issue 3 grants Sprites, also allow opening Image Editor **restricted** to creating/editing SPRITE-category **16×16** images (or grant Images early only for that subset). Full templates stay Issue 5.
2. **Data:** Add optional link on sprite animation frames: `ImageName` + `ImageFrameIndex`. On bind/save, **bake** pixels into `SpriteFrame` so `.sprites` format stays compatible. Live preview can read through the link when the image is in memory.
3. **Sprite Editor UI:** “Select artwork…” list of eligible **named** images (16×16, Sprite category); assign frames to animation; keep DEFAULT rules.
4. **Speed / loop in editor:** Optional for Issue 3 minimum — magazine can teach fixed 0.125 s and `SPRITEANIM` loop. If included, store on `SpriteAnimation` and use in preview + runtime (FORMAT change — version carefully). **Defer speed persistence if it risks tape breaks;** document fixed rate in magazine.
5. **Regression:** Headless load of existing `.sprites`; bind→bake→SAVE→LOAD; BASIC `SPRITE`/`SPRITEANIM` smoke; Hardcore still opens both tools; Bedroom pre-Issue-3 still locks F5 (and restricted F7).
6. **Docs:** Update `CREATIVE-TOOLS-IMAGES.md` / CAREER playability note to match chosen gate behaviour.

**Prefer shared references in the editor model; prefer baked pixels on disk for Issue #3 compatibility.** Full live-only refs without bake can wait.

---

## 11. Ordered implementation plan

1. **Audit sign-off** (this document) — done when accepted.  
2. **Career unlock policy** — decide Issue-3 artwork access (restricted Image Editor vs early Images grant).  
3. **Eligibility rules** — 16×16 + Sprite category (+ transparency preserved).  
4. **Bind API** — Sprite Editor selects image by name; map image frames → animation frames; bake into `SpriteFrame`.  
5. **UI** — name list (not indices); DEFAULT still guaranteed; preview uses bound frames.  
6. **SAVE/LOAD** — confirm unchanged `.sprites` round-trip; new sprites still emit FRAME pixels.  
7. **BASIC regression** — existing commands only; no new sprite syntax required for Ghost Catcher MVP.  
8. **Play-bypass decision** — implement or document exception.  
9. **Ghost Catcher** artwork authored via new path; cover `.sprites` (and `.images` if needed + grant fix).  
10. **Magazine curriculum** draft from working pipeline.  
11. **Defer** editor speed/loop fields, Map←Image tiles, full Image Editor Issue 5 polish, deletion of Sprite paint tools.

---

## 12. Exact source files involved

### Images
- `Machine/Images/ImageAsset.cs`
- `Machine/Images/ImageFrame.cs`
- `Machine/Images/ImageAssetStore.cs`
- `Machine/Images/ImageStorage.cs`
- `Machine/Images/ImageCategory.cs`
- `Machine/Images/ImageTemplates.cs`
- `Machine/Images/ImageEditor.cs`
- `Machine/Images/ImageEditor.Actions.cs`
- `Machine/Images/ImageEditor.Tools.cs`
- `Machine/Images/ImageEditor.Dialogs.cs`
- `Machine/CentauriMachine.Images.cs`

### Sprites
- `Machine/SpriteAsset.cs`
- `Machine/CentauriSprite.cs`
- `Machine/CentauriMachine.Sprites.cs`
- `Machine/Sprites/SpriteAnimation.cs`
- `Machine/Sprites/SpriteFrame.cs`
- `Machine/Sprites/SpriteAssetStore.cs`
- `Machine/Sprites/SpriteStorage.cs`
- `Machine/Sprites/SpriteEditor.cs`
- `Machine/Sprites/SpriteEditor.Edit.cs`

### BASIC / tape
- `Basic/BasicMachine.cs`
- `Basic/BasicMachine.Storage.cs`
- `Basic/TapeFolder.cs`
- `Basic/Interpreter.cs` (feature gate)
- `Basic/Interpreter.Sprites.cs`
- `Basic/Tokenizer.cs` / `Basic/Parser.cs`
- `Basic/Syntax/Sprite*.cs`
- `Session/SoftwareGrant.cs`

### Career / UI entry
- `Session/FeatureId.cs`
- `Session/FeatureGate.cs`
- `Session/FeatureAvailability.cs`
- `Session/GameSession.cs`
- `Session/MagazineCatalog.cs`
- `Game1.cs` (F5/F6/F7, `TryOpenAuthoringTool`)

### Docs (intent vs code)
- `docs/CREATIVE-TOOLS-IMAGES.md`
- `docs/CENTAURI64_SPEC.md` (§5 sprites, §15 career)
- `docs/CAREER.md`

### Example sidecars
- `Listings/HAG.sprites` (and any Programs cover sprites)

---

## Bottom line

Issue #3’s magazine story assumes **one artwork editor** feeding a **sprite animation tool** feeding **BASIC**. The runtime half (BASIC + `.sprites` reload) is ready. The missing product is the **bridge** (and an Issue-3-legal way to create that artwork). Sprite Editor paint is a workable interim authoring path for Ghost Catcher pixels, but it conflicts with the preferred shared-asset architecture and duplicates the Image Editor.

**STOP** — no implementation in this phase.
