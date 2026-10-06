# Centauri64 machine specification (as implemented)

**Canonical location:** `Centauri64/docs/CENTAURI64_SPEC.md`

This document is the technical source of truth for what the **currently implemented** Centauri64 virtual machine and software format support, as read from source. It does not propose features.

Use it when writing magazine issues, the Programming Manual, tape packaging, tutorials, and career copy. Fictional 1986 marketing may describe implemented capabilities; it must not invent hardware this document does not record as implemented.

Status markers:

| Marker | Meaning |
|--------|---------|
| **Enforced** | Checked at runtime and rejected or clamped |
| **Not enforced** | Constant or display exists; no hard reject |
| **Incomplete** | Parsed or named but missing/broken behaviour |
| **Planned** | Named in career/magazine data only; no machine implementation |
| **Ambiguous** | Code exists but behaviour is unclear or inconsistent |

Host application: MonoGame DesktopGL, `Centauri64/Centauri64.csproj`, target `net9.0`.

---

## 1. Virtual display and screen modes

### 1.1 Resolutions

Source: `Machine/CentauriMachine.cs`

| Constant | Value | Role |
|----------|------:|------|
| `SCREEN_WIDTH` | 640 | High-resolution / development width |
| `SCREEN_HEIGHT` | 480 | High-resolution / development height |
| `DEVELOPMENT_WIDTH` | 640 | Alias of `SCREEN_WIDTH` (editor, bedroom, tools) |
| `DEVELOPMENT_HEIGHT` | 480 | Alias of `SCREEN_HEIGHT` |
| `ARCADE_WIDTH` | 320 | Arcade / “game” width |
| `ARCADE_HEIGHT` | 240 | Arcade / “game” height |
| `GAME_WIDTH` | 320 | Alias of `ARCADE_WIDTH` |
| `GAME_HEIGHT` | 240 | Alias of `ARCADE_HEIGHT` |

`ScreenWidth` / `ScreenHeight` follow the current `CentauriDisplayMode`.

### 1.2 Display modes

Source: `Machine/CentauriDisplayMode.cs`, `CentauriMachine.SetDisplayMode`

| Presentation | BASIC | Integer | Pixel size |
|--------------|-------|--------:|------------|
| Default BASIC environment | *(none — not a MODE)* | — | 640×480 editor/console |
| `HighResolution` | `MODE 1` | 1 | 640×480 |
| `Arcade` | `MODE 2` | 2 | 320×240 |

Internal enum: `Console = 0` (default BASIC environment; **not** selected by `MODE`), `HighResolution = 1`, `Arcade = 2`.

BASIC: `MODE n` (`Basic/Interpreter.Graphics.cs` → `SetDisplayMode`). Only `1` and `2` are accepted. `MODE 0` and other values throw `Unsupported display mode`.

**Enforced.** Default after `ResetProgramDisplay` is `Console` (normal BASIC editor/console). `RUN` does **not** switch to a graphics display unless the program executes `MODE`.

Presentation (`Game1.cs`): while a program is running or has finished:
- `Console` — keep drawing the BASIC editor/console (`DrawCodeEditor`); text I/O uses the editor console.
- `HighResolution` — 640×480 target (`DrawTextMode`).
- `Arcade` — 320×240 target (`DrawGame`).

`.images` tape sidecars still encode mode as legacy file integers `0` = standard/high-res, `1` = arcade (independent of BASIC `MODE` numbers).

### 1.3 Paper vs “border”

Sources: `CentauriMachine.DEFAULT_INK` = 1, `DEFAULT_PAPER` = 0; `Game1.DrawGame`

Arcade present path comments `// BORDER` then `GraphicsDevice.Clear` with **paper colour**, then fills a 320×240 rectangle with the program console background (also paper). There is **no separate border colour** and **no BASIC `BORDER` statement**.

`BORDER` appears only as magazine curriculum copy (`Session/MagazineCatalog.cs`) and as a comment in `Game1.cs`. **Planned / not implemented.**

### 1.4 Host window (not the fictional machine)

Sources: `Settings/CentauriSettings.cs`, `Game1.ApplySettings`

Window scale is an integer multiple of 640×480 (clamped 1–5). Scaling: pixel-perfect integer or fit-window. Optional fullscreen and VSync. These are **host** settings, not BASIC-visible.

CRT overlay (`Game1.DrawCrtOverlay`) applies only when presenting the computer framebuffer (`applyCrt: true`). Presets: Off, Subtle, 1986 (`Settings/CentauriSettings.CrtPreset`). **Not** a BASIC POKE/effect API.

---

## 2. Colour palette

Source: `Machine/CentauriPalette.cs` — `MAX_COLORS = 32`

`Get(index)` **throws** if `index` is outside `0..31`.

`CentauriMachine.ValidateColour` requires `0 <= colour < 32` for INK, PAPER, PLOT, LINE, RECT, CIRCLE.

RGB values are 8-bit per channel, opaque (no palette alpha):

| Index | Name (comment in source) | R | G | B |
|------:|--------------------------|--:|--:|--:|
| 0 | Black | 0 | 0 | 0 |
| 1 | White | 255 | 255 | 255 |
| 2 | Red | 136 | 0 | 0 |
| 3 | Cyan | 170 | 255 | 238 |
| 4 | Purple | 204 | 68 | 204 |
| 5 | Green | 0 | 204 | 85 |
| 6 | Blue | 0 | 0 | 170 |
| 7 | Yellow | 238 | 238 | 119 |
| 8 | Orange | 221 | 136 | 85 |
| 9 | Brown | 102 | 68 | 0 |
| 10 | Light Red | 255 | 119 | 119 |
| 11 | Dark Grey | 51 | 51 | 51 |
| 12 | Grey | 119 | 119 | 119 |
| 13 | Light Green | 170 | 255 | 102 |
| 14 | Light Blue | 0 | 136 | 255 |
| 15 | Light Grey | 187 | 187 | 187 |
| 16 | Midnight Blue | 20 | 30 | 55 |
| 17 | Navy | 35 | 55 | 90 |
| 18 | Teal | 40 | 100 | 120 |
| 19 | Aqua | 70 | 180 | 170 |
| 20 | Dark Green | 25 | 90 | 55 |
| 21 | Moss Green | 110 | 160 | 70 |
| 22 | Dark Brown | 90 | 55 | 35 |
| 23 | Tan | 175 | 110 | 65 |
| 24 | Plum | 105 | 45 | 80 |
| 25 | Rose | 170 | 80 | 120 |
| 26 | Violet | 110 | 70 | 170 |
| 27 | Lavender | 165 | 130 | 220 |
| 28 | Gold | 220 | 170 | 90 |
| 29 | Peach | 255 | 205 | 150 |
| 30 | Steel Blue | 150 | 190 | 210 |
| 31 | Warm White | 225 | 225 | 205 |

Sprite/image pixel value `-1` is transparency (`CentauriSprite.TRANSPARENT`, `ImageAsset.Transparent`), **not** a palette index.

---

## 3. Text console and font

### 3.1 Bitmap font

Source: `Graphics/BitmapFont.cs`

| Property | Value |
|----------|------:|
| Glyph size | 8×8 pixels |
| Glyphs per row in atlas | 16 |
| First character | ASCII 32 (space) |
| Drawn range | indices `0..94` → characters 32–126 |
| Out of range | advance without drawing |

Asset: `Content/Fonts/centauri64-font` (loaded in `Game1`).

### 3.2 Program / editor consoles

Source: `Console/TextConsole.cs`

| Constant | Value |
|----------|------:|
| `DEFAULT_COLUMNS` | 80 |
| `DEFAULT_ROWS` | 60 |
| Cell size | 8×8 (`CHARACTER_WIDTH` / `CHARACTER_HEIGHT`) |
| Cursor flash | 0.5 s |
| Key repeat delay | 0.4 s |
| Key repeat interval | 0.05 s |

`Game1` constructs the editor console as **80 × 55** (`CODE_EDITOR_ROWS = 55`) and the program console as **80 × 60**.

Each cell: `Machine/ScreenCell.cs` — `char`, foreground index, background index.

`ScreenMargin = 16` exists on `TextConsole` (layout helper).

Default editor ink/paper after `ResetDisplay`: ink 1, paper 0 (`CentauriMachine.ResetDisplay`). Editor chrome also uses palette 16/31 for the listing console (`Game1.LoadContent`).

---

## 4. Graphics primitives (BASIC draw list)

Source: `Machine/CentauriMachine.Graphics.cs`

Stored until `ClearScreen` / `ResetProgramDisplay`:

| Primitive | Storage | Notes |
|-----------|---------|--------|
| PLOT | `Dictionary<(X,Y), PlotPoint>` | Same coordinate overwrites |
| LINE | `List<LinePrimitive>` | Bresenham, 1×1 pixels |
| RECT | `List<RectPrimitive>` | Optional fill; outline uses width/height − 1 |
| CIRCLE | `List<CirclePrimitive>` | Midpoint circle; optional fill |

**Not enforced:** coordinates are **not clipped** to the current screen. Off-screen pixels may still be submitted to the sprite batch.

Camera (`_cameraX`, `_cameraY`) is subtracted at draw time (same camera as tiles/sprites/PRINTAT).

RECT/CIRCLE: `FILL` keyword after an extra comma (`Parser.ParseRectStatement` / `ParseCircleStatement`). Width/height/radius must be `> 0` (`Interpreter.Graphics.cs`).

Draw order of primitives: IMAGE blits, then PLOT, LINE, RECT, CIRCLE.

---

## 5. Sprites

### 5.1 Hardware slots

Source: `Machine/CentauriMachine.Sprites.cs`, `Machine/CentauriSprite.cs`

| Limit | Value | Enforcement |
|-------|------:|-------------|
| Hardware sprites | 64 (`MAX_SPRITES`) | Index `0..63` or throw |
| Pixel size | 16×16 | Fixed |
| Transparency | `-1` | Skip on draw |
| Flip | `FlipX` only | `SPRITEFLIP i, facing` — `facing < 0` flips |
| Visibility | `Visible` | `SPRITESHOW` / `SPRITEHIDE` |

Position: integer `X`, `Y` (unclipped).

Draw: `Machine/SpriteRenderer.cs` — per-pixel 1×1 quads, camera subtracted, `FlipX` mirrors X.

Collision: `SpritesCollide` — axis-aligned bounds of **non-transparent** pixels. Hidden sprites do not collide. Fully transparent sprites return no bounds (**false**).

### 5.2 Assets and animation

Sources: `Machine/SpriteAsset.cs`, `Sprites/SpriteAnimation.cs`, `Sprites/SpriteFrame.cs`, `CentauriMachine.Sprites.cs`

- Named assets (string keys, typically uppercased on save).
- Each asset has one or more **animations**; at least one animation must remain (`RemoveAnimation` refuses the last).
- Each animation has one or more **16×16 frames**; last frame cannot be removed.
- `SPRITE i, "NAME"` loads animation `"DEFAULT"` (required), starts looping, makes sprite visible.
- `SPRITEANIM i, "NAME", loop` — `loop` must be `0` or `1` (`Interpreter.Sprites.cs`). Loop `0` is one-shot; returns to previous looping animation if stored.
- Frame time: **0.125 s** per frame (`UpdateSprites`).
- Built-in asset `"PLAYER"`: yellow (palette 7) X-shape on transparent (`CreateBuiltInSpriteAssets`). Recreated if missing.

**No FlipY. No per-sprite scale. No hardware sprite size other than 16×16.** Image-editor 32×32 “sprite” templates are **image assets**, not hardware sprites (`ImageTemplates.cs`).

### 5.3 Persistence

`Machine/Sprites/SpriteStorage.cs` — sidecar `name.sprites` under the tape folder. Text format: `SPRITE`, `ANIMATION`, `FRAME`, comma-separated palette indices.

---

## 6. Maps and tiles

Sources: `CentauriMachine.Tiles.cs`, `Maps/MapAsset.cs`, `Maps/MapEditor.cs`, `Maps/MapStorage.cs`

| Item | Current behaviour |
|------|-------------------|
| Tile size | 16×16 (`TileSize = CentauriSprite.WIDTH`) |
| Tile graphics | Sprite asset `DEFAULT` animation **frame 0 only** — **not animated** |
| Cell 0 | Empty (not drawn) |
| Tile IDs | `TDEF id, name` requires `id > 0` |
| Runtime map | `MAP array, columns, rows` copies a 1D BASIC integer array, length ≥ `columns * rows` |
| `LOADMAP "NAME"` | Loads a map asset and its `TDEF` bindings |
| Camera | `CAMERA x,y`, `CAMERA FOLLOW sprite`, `CAMOFF` |
| Camera clamp | Only when a tile map is set; clamped to world size minus screen |
| `TILEAT(x,y)` | Pixel coordinates; **0** if no map or out of world |

Map **assets** (`MapAsset`): columns/rows must be `> 0`. No coded maximum size. Map Editor default new map: **40×15** (`DefaultColumns` / `DefaultRows`), view **18×12** cells at scale 2, undo 32.

Sidecar: `name.maps` (`MAP`, `SIZE`, `TDEF`, `ROW`).

---

## 7. Image system

Sources: `Machine/Images/ImageAsset.cs`, `CentauriMachine.Images.cs`, `ImageCategory.cs`, `ImageTemplates.cs`, `ImageStorage.cs`, `ImageLayers.cs`

### 7.1 Asset model

| Limit | Value | Enforcement |
|-------|------:|-------------|
| Size | ≥ 1×1, ≤ current mode (640×480 or 320×240) | `ValidateSize` |
| Frames | 1–64 (`MaxFrames`) | Throw `TOO MANY FRAMES` |
| Mode | Must match `CentauriDisplayMode` of the running program for blit/BG/FG | `IMAGE MODE MISMATCH` |
| Transparency | `-1` | |
| Category | `General`, `Sprite`, `Tileset`, `Background` | Organisational; **not** a runtime type |

Editor templates (`ImageTemplates.ForMode`): 8×8 tile, 16×16 sprite, 32×32 sprite, 64×64, illustration (min 160×100 vs mode), wide (full width × min 100), full-screen background, custom.

Editor zoom steps: **1, 2, 3, 4, 6, 8, 12, 16, 24, 32** (`ImageEditor.ZoomSteps`). Fit-zoom also exists. Undo 32.

**Sprite Builder / Map Editor consuming image categories is documented as future** (`ImageAsset` / `ImageCategory` comments). **Incomplete / planned for tools**, not runtime.

### 7.2 BASIC image commands

| Command | Behaviour |
|---------|-----------|
| `IMAGE "N"` | Blit frame 0 at (0,0) |
| `IMAGE "N", x, y` | Blit |
| `IMAGE "N", x, y, f` | Blit frame `f` |
| `IMAGE OFF` | Clears **blit list only** |
| `BG layer, "N"` | Layer **0 or 1**; image must be **full-screen** for current mode |
| `BG layer, OFF` | Hide that layer |
| `FG "N"` | Full-screen overlay |
| `FG OFF` | Hide FG |

Blits are a list (`_imageBlits`), cleared by CLS and `IMAGE OFF`. BG0/BG1/FG persist across CLS (`ClearScreen` does not call `ClearImageLayers`). Layers clear on `ResetProgramDisplay` / `NEW`.

### 7.3 Draw order (arcade / running program)

Documented in `CentauriMachine.Images.cs`:

1. Paper  
2. BG0  
3. BG1  
4. Graphics (IMAGE blits + PLOT/LINE/RECT/CIRCLE)  
5. Tiles  
6. Sprites  
7. FG  
8. PRINTAT text  

(`Game1.DrawGame` / `DrawTextMode` match this sequence.)

Sidecar: `name.images`.

---

## 8. Sound / audio

Sources: `Machine/Audio/CentauriAudio.cs`, `CentauriMachine.Audio.cs`, `Basic/Interpreter.Audio.cs`

| Item | Value |
|------|------|
| API | `BEEP frequency, durationMs` |
| Synthesis | Mono sine, 44100 Hz, amplitude `0.20 * Volume` |
| Volume | Host master × SFX, clamped 0–1 (`SetAudioVolume`) |
| Duration 0 | No sound, continues |
| Negative freq/duration | Runtime error |
| Concurrent beeps | New beep **silences** the previous (`Silence`) |
| `BEEP` and interpreter | Also `BeginWait(durationMs)` — program **waits** for the beep duration |

**No** music engine (settings `MusicVolume` persisted, unused). **No** multi-channel PSG. **No** sample playback API.

---

## 9. Input

### 9.1 BASIC

Source: `CentauriMachine.GetKey` / `IsKeyDown` / `IsKeyPressed`

`KEY("NAME")` — held. `KEYPRESSED("NAME")` — edge (down this frame, up last frame).

Recognised names (case-insensitive trim):

`LEFT`, `RIGHT`, `UP`, `DOWN`, `SPACE`, `ENTER`/`RETURN`, `ESC`/`ESCAPE`, `0`–`9`, `A`–`Z`.

Unknown names → 0 (not an error).

Return values: 1 / 0 (`Interpreter.cs`).

**No joystick, mouse, or gamepad BASIC functions.** Bundle “joysticks” are career flavour (`ComputerBundleCatalog.cs`), not hardware.

### 9.2 INPUT statement

`INPUT [prompt,] var` (`Parser` / `Interpreter.Text.cs`). String variables (`name$`) store text. Numeric variables parse integer or print `?REDO FROM START` and re-prompt.

### 9.3 Host / editor

Keyboard for BASIC listing (`TextConsole`). Mouse used by sprite/map/image editors and some UI screens. Escape stops a running program (`Game1`).

---

## 10. Memory and execution

Sources: `CentauriMachine.cs`, `Basic/BasicProgram.cs`, `Basic/BasicMachine.cs`, `BasicMachine.Execution.cs`

| Constant | Value | Enforcement |
|----------|------:|-------------|
| `SYSTEM_MEMORY_BYTES` | 65536 (64K) | Displayed as “64K” in editor chrome (`Game1.DrawEditorChrome`). **Not** an allocator |
| `BASIC_MEMORY_BYTES` | 49152 (48K) | `MEM` reports `FREE = BASIC_MEMORY_BYTES − used`. **Storing lines does not reject overflow** |
| Line usage | `len(source) + 4` overhead per line | Accounting only |
| Instructions per frame | `MAX_INSTRUCTIONS_PER_FRAME = 700` | Yield / Wait / Input break the slice |
| `WAIT n` / beep wait | `n` milliseconds (`Stopwatch`) | |
| `YIELD` | End instruction slice this frame | |
| Types | Integer (`int`) or string | No floats |
| Integer divide | Truncating; divide by zero throws | |
| Integer overflow | C# `int` wrap; **not** specified as 16-bit | **Ambiguous vs 8-bit/16-bit micros** |
| Arrays | 1-D `int[]` via `DIM A(n)`, `n > 0` | Index `0 .. n-1`. **No max `n`** |
| String vars | Name ends with `$` | Default `""` |
| Numeric vars | Default `0` | |
| `GOSUB`/`FOR` stacks | `Stack<T>` | No depth limit. `RETURN`/`NEXT` mismatch throws |

`MEM` immediate command: `BasicMachine.Commands.ShowMemory`.

---

## 11. BASIC language

Sources: `Basic/Tokenizer.cs`, `Basic/Parser.cs`, `Basic/Interpreter*.cs`, `Basic/TokenType.cs`

### 11.1 Program model

- Stored lines **must** start with a line number (`Parser.ParseLineNumber` → `int.Parse`).
- Unnumbered input is executed immediately when it parses as an allowed statement (`Parser.ParseImmediate` → `Interpreter.ExecuteImmediate`). Immediate statements are **not** stored in the listing.
- Host commands (`RUN`, `LIST`, `DIR`, …) are matched first and never enter the statement parser.
- One statement per line. `IF cond THEN statement` — no `ELSE`, no multi-statement THEN.
- `GOTO` / `GOSUB` take a **numeric literal** line number, not an expression.
- `REM` consumes the rest of the line.
- Keywords are case-sensitive as tokenized from the typed source (typically uppercase in listings).

### 11.2 Immediate (READY) commands

Source: `Basic/BasicMachine.cs` (exact string match unless noted)

| Command | Action |
|---------|--------|
| `RUN` | Start interpreter |
| `LIST` / `LIST a-b` | List program |
| `DIR` | List `.bas` tapes |
| `MEM` | Memory report |
| `ANALYSE` / `ANALYZE` | Software analyser |
| `EDIT n` | Edit a line |
| `DELETE name` | Delete a tape |
| `NEW` | Clear program and editor assets, new program id |
| `CLS` | Clear editor console |
| `RESET` | `ResetDisplay` + boot message (**immediate only**) |
| `SAVE name` | Write tape |
| `LOAD name` | Read tape |

### 11.2.1 Immediate BASIC statements

Unnumbered statements at the READY prompt (after host commands):

| Statement | Notes |
|-----------|--------|
| `PRINT expr` | Required; writes to the default BASIC console |
| `CLS` | Clears the default BASIC console (no graphics MODE change) |
| `INK n` / `PAPER n` | Same semantics as in programs |
| `BEEP f,d` | Tone; wait is not held in the run loop |
| `name = expr` | Scalar assignment; variables persist until next `RUN` |
| `REM …` | No-op |

**Not** supported immediately (program-control / multi-line flow): `GOTO`, `GOSUB`, `RETURN`, `FOR`, `NEXT`, `INPUT`, `MODE`, graphics, networking, sprites, maps, etc. These report `Cannot execute that statement immediately.`

Numbered `RESET` as a **program statement** calls `ResetDisplay` then **falls through** and throws `Unsupported statement: ResetStatement` (`Interpreter.Execute`). **Incomplete.**

### 11.3 Statements (implemented)

| Syntax | Notes |
|--------|--------|
| `PRINT expr` | Newline via `Print` |
| `PRINTAT x,y,expr` | Pixel position; one string/value per cell key `(x,y)` |
| `INPUT` | See §9.2 |
| `CLS` | Active text console, plots, lines, rects, circles, image **blits**, tile **map**; **not** sprites; **not** BG/FG layers. In default BASIC environment clears the editor console without changing MODE. |
| `INK n` / `PAPER n` | Palette 0–31 |
| `MODE n` | `1` = High Resolution 640×480; `2` = Arcade 320×240 |
| `PLOT x,y,c` | |
| `LINE x1,y1,x2,y2,c` | |
| `RECT x,y,w,h,c` optional `,FILL` | |
| `CIRCLE x,y,r,c` optional `,FILL` | |
| `BEEP freq,ms` | |
| `WAIT ms` | |
| `YIELD` | |
| `GOTO n` | |
| `GOSUB n` / `RETURN` | |
| `IF cond THEN stmt` | Numeric cond; nonzero true |
| `FOR v=a TO b [STEP s]` / `NEXT v` | Numeric `v`; `STEP` ≠ 0 |
| `LET`-less `v=expr` | Assignment |
| `DIM a(n)` | Numeric array only |
| `a(i)=n` | |
| `END` | Stop |
| `REM` | |
| `SPRITE i,"name"` | |
| `SPRITEPOS i,x,y` | |
| `SPRITESHOW i` / `SPRITEHIDE i` | |
| `SPRITEFLIP i,facing` | |
| `SPRITEANIM i,"anim",loop` | |
| `TDEF id,"sprite"` | |
| `MAP array,cols,rows` | |
| `LOADMAP expr` | |
| `CAMERA x,y` / `CAMERA FOLLOW i` / `CAMOFF` | |
| `IMAGE` / `BG` / `FG` | See §7.2 |
| `NET HOST` / `JOIN` / `WAIT` / `LEAVE` / `SEND name,value` | See §13 |

### 11.4 Functions (implemented)

| Function | Returns |
|----------|---------|
| `KEY(s)` | 1 if held |
| `KEYPRESSED(s)` | 1 on edge |
| `RND(n)` | `0 .. n-1` (`Random.Next`); `n > 0` |
| `SWIDTH` / `SHEIGHT` | Current mode size |
| `COLLIDE(a,b)` | Sprite AABB, 1/0 |
| `ANIMPLAYING(i [, "anim"])` | 1/0 |
| `TILEAT(x,y)` | Tile id |
| `LEN(s)` | |
| `LEFT$(s,n)` / `RIGHT$(s,n)` | |
| `MID$(s,start [,len])` | `start` is **1-based** |
| `UPPER$(s)` | |
| `NET("name")` | Integer channel |
| `NETPLAYER` | 0 idle, 1 host, 2 join (`NetworkService.PlayerNumber`) |
| `NETCONNECTED` | 1/0 |

### 11.5 Operators

`+ - * /` (ints); unary `-`; `NOT` (0→1, else 0); `AND` / `OR` (nonzero = true); comparisons `= <> < > <= >=`. String `+` concatenates (`ToString` of either side). String comparisons are ordinal.

### 11.6 Not implemented (named elsewhere)

| Name | Where named | Machine status |
|------|-------------|----------------|
| `BORDER` | Magazine issue 1 curriculum | **Planned** |
| `DATA` / `READ` / `RESTORE` | Magazine issue 6 | **Planned** (`FeatureId.DataStatements`) |
| `PEEK` / `POKE` / `SYS` | Magazine issue 10 | **Planned** (`FeatureId.LowLevelMachine`) |
| Custom fonts / imported bitmaps as a machine API | Magazine issue 10 | **Planned** (`FeatureId.CustomAssets`) — Image Editor import is a **tool**, not BASIC PEEK |
| Joystick BASIC | Bundle copy | **Not implemented** |
| Floats | — | **Not implemented** |
| Multi-statement lines / `ELSE` | — | **Not implemented** |

---

## 12. Storage / tape format

Sources: `Basic/TapeFolder.cs`, `ProgramStorage.cs`, `TapeLabel.cs`, `TapeCover.cs`, `TapeKind.cs`, `GameGenre.cs`, `TapePlayers.cs`

### 12.1 Location

`%LocalAppData%\Centauri64\Tapes` (`TapeFolder.Location`). **Not** the install directory, except one-time/refresh copy of shipped `Programs/` demos (newer install file overwrites tape of the same name).

### 12.2 Files per tape name

| Extension | Content |
|-----------|---------|
| `.bas` | Program source lines |
| `.tape` | Label metadata |
| `.cover` | 40×56 colour indices (`TapeCover.Width` / `Height`). Legacy 80×112 sidecars downsample 2× on load. |
| `.sprites` | Sprite assets |
| `.maps` | Map assets |
| `.images` | Image assets |

Tape names: non-empty, no `..`, no invalid filename characters, stored **uppercase**.

### 12.3 Label (`TapeLabel`)

| Field | Limit / default |
|-------|-----------------|
| `CurrentMachineVersion` | 1 |
| `DefaultProgramVersion` | 1 |
| Description | max 40 chars (`MaxDescriptionLength`) |
| Author | max 16 chars (`MaxAuthorLength`) |
| `Kind` | `Game`, `Utility`, `Demo`, `Tool`, `Experiment` |
| `Genre` | `None`, `Arcade`, `Action`, `Puzzle`, `Platform`, `Shooter`, `Adventure`, `Racing`, `Sports`, `Strategy`, `Other` |
| `Players` | `One=1`, `TwoLocal=2`, `TwoNetwork=3` |
| `ProgramId` | string, used for network matchmaking |
| `ProgramVersion` | int, network compatibility |

Cover “has art” if any pixel ≠ 0. Analyser custom-cover threshold: **12** non-zero pixels (`Analysis/SoftwareAnalyser.CustomCoverPixelThreshold`).

One shared tape format for Bedroom and Hardcore (no separate career/hardcore tape types).

---

## 13. Networking

Sources: `Network/LocalNetworkTransport.cs`, `NetworkProtocol.cs`, `NetworkService.cs`, `Interpreter.Network.cs`

| Item | Value |
|------|------|
| Transport | TCP **loopback only** `127.0.0.1:24640` |
| Protocol version | 1 |
| Frame | 4-byte length prefix + payload; payload length `0 .. 65536` |
| Messages | Hello(1), Accept(2), Reject(3), State(4) |
| Hello payload | protocol version, `programId`, `programVersion` |
| State | map of string → **int** |
| Players | Host = 1, joiner = 2 |
| `NET JOIN` timeout | 3000 ms (`NetworkService.Join`) |
| `NET SEND name, value` | Integer value; dirty-sent each frame |
| `NET("name")` | Last received int or 0 |

**Not** internet/LAN beyond localhost. **Not** more than two peers in this transport (one listener, one client).

Mismatch of program id/version → reject (handshake in `NetworkService`).

---

## 14. Presentation / compositing limits

- Sprite, tile, and primitive drawing use 1×1 pixel sprites (no batching atlas at runtime except image textures in `ImageTextureCache`).
- No scanline/raster BASIC API.
- No hardware scrolling except camera offset on tiles/sprites/primitives/PRINTAT.
- Text-mode `PRINT` uses the scrolling `TextConsole`; `PRINTAT` is a separate overlay list (one entry per unique pixel coordinate).

---

## 15. Authoring gates (software, not hardware)

Sources: `Session/FeatureGate.cs`, `Session/FeatureId.cs`, `Session/FeatureAvailability.cs`

The machine still **parses** locked statements. Bedroom Coder **execution** may throw e.g. `SPRITE GRAPHICS NOT YET AVAILABLE`. Hardcore uses `FeatureAvailability.AllReleased()`.

| FeatureId | Gates (if Bedroom-locked) |
|-----------|---------------------------|
| `CoreBasic` | Always on |
| `Graphics` | PLOT, LINE, RECT, CIRCLE |
| `Sprites` | Sprite statements, `COLLIDE`, `ANIMPLAYING`; F5 editor |
| `Maps` | TDEF/MAP/LOADMAP/CAMERA/CAMOFF, `TILEAT`; F6 editor |
| `Images` | IMAGE/BG/FG; F7 editor |
| `Networking` | NET statements and NET* functions |
| `Arrays` / `DataStatements` / `CustomAssets` / `LowLevelMachine` | Stored on career; **no extra interpreter commands yet** |

Runtime of already-saved tapes is **not** blocked by the player’s authoring unlocks (gates run in `Interpreter.Execute` / function eval of the **running** session).

---

## 16. Host paths (application, not the 64K machine)

| Path | Role |
|------|------|
| `%LocalAppData%\Centauri64\Tapes` | Software |
| `%LocalAppData%\Centauri64\settings.json` | Host settings |
| `%LocalAppData%\Centauri64\Careers\slotN.json` | Career slots |
| `%LocalAppData%\Centauri64\player.json` | Legacy career; migrated to slot 1 |

---

## 17. Source index (primary types)

| Area | Primary types |
|------|----------------|
| Machine core | `CentauriMachine`, `CentauriDisplayMode`, `CentauriPalette` |
| Sprites | `CentauriSprite`, `SpriteAsset`, `SpriteRenderer`, `SpriteEditor` |
| Maps | `MapAsset`, `MapEditor`, `CentauriMachine` (Tiles partial) |
| Images | `ImageAsset`, `ImageEditor`, `ImageLayers`, `ImageStorage` |
| Audio | `CentauriAudio` |
| Text | `TextConsole`, `BitmapFont`, `ScreenCell` |
| BASIC | `Tokenizer`, `Parser`, `Interpreter`, `BasicMachine`, `BasicProgram`, `BasicValue` |
| Tape | `TapeFolder`, `ProgramStorage`, `TapeLabel`, `TapeCover` |
| Network | `NetworkService`, `LocalNetworkTransport`, `NetworkProtocol` |
| Present | `Game1` |

---

## 18. Documentation policy

Do not publish a Centauri64 hardware or BASIC capability in permanent magazine, Programming Manual, or advertising content unless it is represented in this specification as **implemented**, or the capability has been explicitly approved as part of the development milestone for that issue.

The machine may already implement a capability before Career Mode or a magazine issue teaches it. Curriculum timing is not a hardware limit.

**Implemented vs planned.** Status markers in this document (Enforced, Not enforced, Incomplete, Planned, Ambiguous) remain authoritative. Planned names such as `BORDER`, `DATA` / `READ` / `RESTORE`, and `PEEK` / `POKE` / `SYS` are future development requirements, not current BASIC.

**Fictional marketing.** This file stays technical. Copy may later describe implemented facts (64K memory, 32 colours, 64 programmable sprites, 640×480 high-resolution mode, 320×240 arcade mode, Centauri BASIC, cassette software). Do not add invented CPU names, clock speeds, sound chips, joystick-port hardware, memory maps, or raster APIs here.

**Not technical canon yet:** bundle “joystick” flavour (`ComputerBundleCatalog`); localhost two-player TCP (do not describe as internet or general LAN); single-voice `BEEP` (do not describe a multi-channel sound chip).

---

*Derived from the Centauri64 source tree. Magazine, career, and shop flavour are not machine capabilities unless listed above.*
