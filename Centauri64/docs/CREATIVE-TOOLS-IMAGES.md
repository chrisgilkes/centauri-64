# Creative Tools V2 — Images (amended)

## Role

The **Image Editor** is the single raster/pixel drawing tool.

Future tools reuse its artwork:

- **Sprite Builder** — animations from SPRITE Image frames
- **Map Editor** — tile palettes from TILESET Image frames
- **BASIC** — `IMAGE` blits and `BG`/`FG` layers

## Asset model

`ImageAsset` on a tape (`{NAME}.images`):

- variable width/height (within current screen mode max)
- category hint: GENERAL / SPRITE / TILESET / BACKGROUND
- one or more frames (same size/mode)
- transparency = `-1`

Legacy full-screen files load as one frame; full-screen legacy images are
categorised BACKGROUND.

## Modes

| Mode | Max size | Colours |
|------|----------|---------|
| STANDARD (high-res / MODE 1 screen) | 640×480 | 32 |
| ARCADE (MODE 2 screen) | 320×240 | 32 |

## BASIC

```text
IMAGE "NAME"           ' blit at 0,0  (frame 0)
IMAGE "NAME",X,Y       ' blit at X,Y
IMAGE "NAME",X,Y,F     ' blit frame F (0-based)
IMAGE OFF              ' clear IMAGE blits (CLS also clears)
BG 0,"NAME"            ' persistent full-screen layer
BG 1,"NAME"
FG "NAME"
```

`IMAGE` is a draw/blit into the graphics list (cleared by CLS).
Text printed afterwards stays visible.

`BG`/`FG` require a technically full-screen image for the current mode
(category alone is not enough).

## Draw order

1. Paper
2. BG0 / BG1
3. Graphics (`PLOT`/`LINE`/… + `IMAGE` blits)
4. Tiles
5. Sprites
6. FG
7. BASIC text / `PRINTAT`
