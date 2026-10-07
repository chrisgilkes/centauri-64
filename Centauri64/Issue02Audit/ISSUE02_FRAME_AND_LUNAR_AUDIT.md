# Issue #2 — Frame model & Lunar Rescue animation audit

Focus: whether retained primitives + CLS + KEY + YIELD can animate a lander without sprites.

**Runtime polish (post-audit):** `GOSUB` now continues a `CLS` presentation burst so beginners may structure `CLS` → draw `GOSUB`s → `YIELD` without a blank frame. See §2.

---

## Verdict (short)

**Yes — Lunar Rescue is achievable with current BASIC**, using:

1. Update variables (KEY, gravity, position).
2. `CLS` then redraw with `RECT` / `LINE` / `PLOT` / `PRINTAT` (draw work may live in `GOSUB`s).
3. `YIELD`.
4. `GOTO` loop.

There is **no framebuffer erase/redraw API**. Graphics are a **retained draw list** re-issued every host frame on top of a PAPER-coloured clear. Animation = rebuild that list every BASIC frame.

Teaching model for the magazine:

1. Update your variables.  
2. `CLS`.  
3. Draw the new frame (subroutines welcome).  
4. `YIELD`.  
5. Repeat.

---

## 1. How graphics exist across frames

### Storage (retained)

| Primitive | Storage | Growth without CLS |
|-----------|---------|---------------------|
| `PLOT` | `Dictionary<(X,Y), PlotPoint>` | New cells accumulate; **same (X,Y) overwrites** |
| `LINE` | `List<LinePrimitive>` | **Appends every call** |
| `RECT` | `List<RectPrimitive>` | **Appends every call** |
| `CIRCLE` | `List<CirclePrimitive>` | **Appends every call** |

Source: `Machine/CentauriMachine.Graphics.cs`.

`CLS` → `ClearScreen()` clears plot dict + line/rect/circle lists (+ text console, PRINTAT list, image blits, tile map). **Not** sprites / BG / FG layers.

### Host render (every MonoGame frame)

`Game1.DrawTextMode` (MODE 1) / `DrawGame` (MODE 2):

1. `GraphicsDevice.Clear(PaperColour)`
2. Draw retained IMAGE layers / blits
3. `DrawGraphics` — re-walk **all** retained PLOT/LINE/RECT/CIRCLE
4. Tiles, sprites, FG
5. Program console + PRINTAT text

So: **pixels are not sticky framebuffer ink**. The “screen” is PAPER + whatever is still in the lists. Omitting CLS does not “leave last frame’s pixels”; it **leaves last frame’s commands**, which are painted again **and** joined by new commands.

### Implication for animation

| Technique | Result |
|-----------|--------|
| Move lander, `RECT` new place, no CLS | Old lander rectangles remain → trails / smears |
| “Erase” with PAPER-coloured RECT | Still **appends** another rect; list grows; not a true erase |
| `PLOT` paper colour over old pixel | Works for that cell only; dict still holds the cell |
| **CLS + full redraw + YIELD** | Correct; list stays tiny; matches PONG |

**Magazine-ready animation = CLS every game frame, then redraw everything you want visible.**

---

## 2. CLS presentation burst (including GOSUB)

`ExecuteCls` returns `ScreenPresentation.Clear`.

After any presentation-tagged result, if the **next** statement is not in the “continues presentation” set, the interpreter returns `ExecutionAction.Yield` — **end of this frame’s instruction slice**.

For `Clear`, allowed followers include:

`PRINT`, `PRINTAT`, `CLS`, `PLOT`, `LINE`, `RECT`, `CIRCLE`, `INK`, `PAPER`, `FOR`, `NEXT`, `IF`, **`GOSUB`**

Source: `Interpreter.NextContinuesPresentation`.

**Root cause of the former blank-frame trap:** `GOSUB` was not in that set. After `CLS`, the interpreter auto-yielded before entering the draw subroutine, so the host presented an empty PAPER frame for one tick.

**Fix:** `GosubStatement` continues `ScreenPresentation.Clear`. `RETURN` did **not** need changes — after the jump, draw statements return `Presentation.None` and remain in the same instruction slice until an explicit `YIELD`, a presentation break, or the 700-instruction cap.

### Canonical pattern (preferred for teaching)

```basic
1000 GOSUB 300      : REM input
1010 GOSUB 400      : REM physics
1020 CLS
1030 GOSUB 100      : REM draw terrain
1040 GOSUB 200      : REM draw lander
1050 GOSUB 270      : REM HUD
1060 YIELD
1070 GOTO 1000
```

### Still valid (PONG / original audit prototype)

```basic
1000 GOSUB 2000
...
2000 CLS
2010 RECT ...
2020 RETURN
```

### Still ends the Clear burst (by design)

- Assignments after `CLS` (update variables **before** `CLS`)
- Bare `GOTO` after `CLS`
- Anything else not in the continue list

`MAX_INSTRUCTIONS_PER_FRAME = 700` still caps runaway work inside a draw `GOSUB`.

Headless coverage: `--verify-presentation`.

---

## 3. YIELD vs WAIT vs 700-instruction slice

| Mechanism | Behaviour |
|-----------|-----------|
| `YIELD` | Explicit end of frame slice; PC advances; next host frame resumes |
| Auto-yield after Clear/Text/Sprite presentation | Same idea when next stmt breaks the burst |
| `WAIT n` | Wall-clock pause (~ms); blocks game loop — fine for title pauses, **not** for per-frame lander |
| `MAX_INSTRUCTIONS_PER_FRAME = 700` | Hard cap per host frame if nothing yields |

Realtime loop needs **`YIELD` every iteration** (or rely on presentation auto-yield + careful structure).

Press-space waits should **`YIELD` each host frame** (see clean Lunar listing). Busy `IF KEYPRESSED… GOTO` without `YIELD` burns the 700-cap and only samples keys between host frames.

Keyboard: `Game1` calls `_machine.UpdateInput()` each Update before BASIC steps, so `KEY` / `KEYPRESSED` see fresh state **once per host frame**, not mid-slice.

---

## 4. Colour vs INK (graphics)

`PLOT` / `LINE` / `RECT` / `CIRCLE` **require an explicit colour argument**.  
`INK` does **not** feed those APIs — only text console / PRINTAT ink.

Syntax (actual parser):

```text
PLOT x,y,c
LINE x1,y1,x2,y2,c
RECT x,y,w,h,c
RECT x,y,w,h,c,FILL
CIRCLE x,y,r,c
CIRCLE x,y,r,c,FILL
```

`PAPER n` sets `_paperColour` used by host `Clear`.  
`CLS` clears lists; the visible clear colour is PAPER on the next draw.

No coordinate clipping (documented in `CENTAURI64_SPEC.md`); off-screen draws may still be submitted.

---

## 5. KEY / KEYPRESSED (realtime)

```text
KEY("LEFT")       → 1 if held, else 0
KEYPRESSED("SPC") → 1 on rising edge only
```

Names (case-insensitive): `LEFT` `RIGHT` `UP` `DOWN` `SPACE` `ENTER`/`RETURN` `ESC`/`ESCAPE` `0`–`9` `A`–`Z`.  
Unknown name → 0 (no error).

Magazine form:

```basic
IF KEY("LEFT") THEN X=X-2
IF KEY("RIGHT") THEN X=X+2
IF KEY("UP") THEN VY=VY-2
```

`AND` / `OR` exist (numeric nonzero logic) — collision can be one IF or sequential IFs.

---

## 6. Integer gravity / collision — practical?

Yes, at one `YIELD` per frame:

```basic
VY=VY+1
Y=Y+VY
IF KEY("UP") THEN VY=VY-2
```

Clamp `VX`; pad test with range + `VY` threshold (PONG-style sequential IFs already proven).  
No floats required. Feel will be “chunky” — appropriate for Issue #2.

---

## 7. Lunar Rescue prototype status

| File | Role |
|------|------|
| `Issue02Audit/11_lunar_rescue_prototype.bas` | Original audit PoC — **CLS inside draw GOSUB** (still valid) |
| `Issue02Audit/13_lunar_rescue_clean_loop.bas` | Clean teaching loop — **CLS in main, draw via GOSUBs**; SPACE waits use `YIELD` |
| `Issue02Audit/14_cls_gosub_visual.bas` | Tiny visual check for CLS→GOSUB (solid box, no flash) |
| `Programs/LUNAR.bas` | Playable My Software copy of the clean loop |

Includes: MODE 1, PAPER/CLS, surface + pad, lander RECT/LINE, KEY thrust/strafe, integer gravity, fuel, pad vs crash, restart on SPACE.

Approximate beginner cover-tape size if polished: **~120–180 lines**, still LISTable.

---

## 8. Career gate note

`PLOT`/`LINE`/`RECT`/`CIRCLE` require `FeatureId.Graphics` (`FeatureGate`). Issue #2 grants it.  
`MODE` itself is **not** gated. Headless/Hardcore use `FeatureAvailability.AllReleased()`.

---

## 9. Capability matrix (animation-relevant)

| Feature | Implemented | Verified | Magazine Ready | Notes |
|---------|-------------|----------|----------------|-------|
| MODE 1 640×480 | YES | CODE | YES | Explicit; RUN does not auto-select |
| Retained PLOT/LINE/RECT/CIRCLE | YES | CODE | YES | Must teach CLS rebuild |
| CLS clears primitives | YES | CODE | YES | |
| Host clear to PAPER each draw | YES | CODE | YES | |
| CLS then redraw animation | YES | CODE+PONG | YES | **Canonical** |
| CLS then GOSUB draw | YES | CODE+TESTS | YES | Continues Clear burst |
| Erase-by-PAPER without CLS | PARTIAL | CODE | **NOT APPROPRIATE** | List growth |
| Endless PLOT stars no CLS | PARTIAL | CODE | **NO** | Dict growth |
| Stars CLS+YIELD | YES | CODE | YES | |
| INK colours primitives | **NO** | CODE | N/A | Explicit `c` argument instead |
| KEY held | YES | CODE+PONG | YES | `KEY("LEFT")` |
| KEYPRESSED edge | YES | CODE+PONG | YES | |
| YIELD frame loop | YES | CODE+PONG | YES | Teach early |
| WAIT in game loop | YES | CODE | **NOT APPROPRIATE** | Use for pauses only |
| Integer gravity | YES | CODE | YES | |
| Collision IF/AND | YES | CODE | YES | |
| PRINT/PRINTAT in MODE 1 | YES | CODE | YES | HUD |
| INPUT in MODE 1 | YES | CODE | PARTIAL | Blocks; not for flight |
| Sprites for lander | YES | — | **DEFER** | Issue #3 |
| Lunar Rescue w/o sprites | YES | DESIGN | YES | Prototype + clean loop |

---

## 10. Blockers vs polish

### A. READY NOW
MODE 1, PLOT/LINE/RECT/CIRCLE(+FILL), PAPER/CLS, KEY/KEYPRESSED, YIELD loops, RND+CLS stars, integer physics, PRINTAT HUD, AND/OR, CLS→GOSUB draw organisation.

### B. WORKS BUT NEEDS POLISH (editorial / UX)
- Teach **colour argument** (not INK) for draw commands.  
- Teach **update → CLS → draw → YIELD** (subroutines for each stage).  
- Auto-yield after draw is invisible magic — magazine should still show explicit `YIELD` in game loops (PONG does both).  
- No clipping — keep coords on-screen in listings.

### C. BLOCKER
**None** for a simple Lunar Rescue.

### D. NOT NEEDED
Framebuffer XOR erase; float physics; sprite collision; MODE 2 for this cover (MODE 1 is the Issue #2 reveal).

### E. DEFER
Sprites, maps, images, arcade MODE teaching as primary path.

---

## 11. Recommended Issue #2 curriculum (frame-aware)

1. MODE 1 — High Resolution reveal (`SWIDTH`/`SHEIGHT` optional).  
2. PLOT x,y,c — first pixel (**colour argument**).  
3. LINE / RECT / CIRCLE (+FILL) — still pictures.  
4. PAPER + CLS — clearing the draw list.  
5. RND starfield — **CLS + PLOT loop + YIELD** (show why CLS matters).  
6. KEY — move a RECT.  
7. Game loop — update → CLS → redraw → YIELD (GOSUBs OK).  
8. Gravity — integer `VY`.  
9. Type-in mini lander / pad.  
10. Lunar Rescue cover tape.  
11. Hack Lunar Rescue.

**Do not** teach “CLS must be the first line of your draw GOSUB” as a special rule — that was a former interpreter trap, now fixed.

---

## 12. Files

| Path | Role |
|------|------|
| `Issue02Audit/01_mode1.bas` … `10_gravity.bas` | Minimal probes |
| `Issue02Audit/11_lunar_rescue_prototype.bas` | Original PoC (CLS inside draw GOSUB) |
| `Issue02Audit/12_frame_model_notes.bas` | REM documentation of frame rules |
| `Issue02Audit/13_lunar_rescue_clean_loop.bas` | Clean teaching loop |
| `Issue02Audit/14_cls_gosub_visual.bas` | Visual CLS→GOSUB regression |
| `Issue02Audit/ISSUE02_FRAME_AND_LUNAR_AUDIT.md` | This report |
| `Analysis/PresentationVerification.cs` | `--verify-presentation` |

---

## 13. Doc vs code

| Topic | Match? |
|-------|--------|
| MODE 1 = 640×480; RUN stays Console until MODE | Match |
| PLOT/LINE/RECT/CIRCLE take colour | Match (spec tables) |
| No clipping | Match |
| KEY/KEYPRESSED strings | Match |
| Retained lists until CLS | Match |
| CLS burst may include GOSUB | Match (runtime polish) |

---

## Bottom line

Retained lists + mandatory CLS rebuild + presentation auto-yield remain the animation contract. Organising the draw stage with `GOSUB` is now safe after `CLS`. Issue #2 can teach a natural game loop without exposing interpreter internals.
