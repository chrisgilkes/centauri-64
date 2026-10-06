# Settings V1

Persistent preferences live in:

```text
%LocalAppData%\Centauri64\settings.json
```

Career progression remains in `player.json` and is independent.

## Bedroom

`[6] SETTINGS` opens the configuration screen.

## Display

| Setting | Notes |
|---------|--------|
| Window mode | Windowed / Fullscreen |
| Window size | Integer scale of the 640×480 development display |
| Scaling | Pixel Perfect (integer) or Fit Window |
| VSync | MonoGame vertical retrace |
| CRT filter | Off / Subtle / 1986 (default: Subtle) |

**CRT applies only when presenting the Centauri64 computer display**
(BASIC editor, text mode, arcade mode). Bedroom, Magazines, Mail, Settings,
and My Software stay clean.

## Editor preferences

`Editor Experience`, Auto Indent, Syntax Colours, Line Highlight, Tooltips,
and Grid are persisted. The Image Editor (Creative Tools V2) uses Tooltips,
Grid, and Editor Experience. Sprite/Map editors are unchanged for now.

## Audio

Master / Music / SFX percentages. Master × SFX drives beep amplitude now.
Music is reserved for later.

## Reset Career

Resets cash, contracts, submissions, and mail only.

Does **not** delete tapes, listings, covers, sprites, maps, or settings.

Requires confirmation, then typing `RESET`.

`DELETE ALL USER DATA` is shown but disabled in this version.
