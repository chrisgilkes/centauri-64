# Issue #2 capability audit material

Development / audit listings for Issue #2 (GRAPHICS! / Lunar Rescue).
They require `FeatureId.Graphics` unlocked (Issue #2 career grant) except where noted.

See `ISSUE02_FRAME_AND_LUNAR_AUDIT.md` for the full report.

| Listing | Notes |
|---------|--------|
| `01`…`10` | Minimal probes |
| `11_lunar_rescue_prototype.bas` | Original PoC — CLS inside draw GOSUB |
| `12_frame_model_notes.bas` | REM rules |
| `13_lunar_rescue_clean_loop.bas` | Preferred teaching loop (CLS in main) |
| `14_cls_gosub_visual.bas` | Visual CLS→GOSUB check |

Playable copy on My Software: `Programs/LUNAR.bas` (clean loop).

Headless: `dotnet run --project Centauri64 -- --verify-presentation`
