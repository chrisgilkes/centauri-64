10 REM ISSUE02 — FRAME / RETAINED-PRIMITIVE BEHAVIOUR
20 REM THIS FILE DOCUMENTS THE RULES; RUN THE NUMBERED TESTS BELOW.
30 REM
40 REM 1) PRIMITIVES ARE RETAINED until CLS / ResetProgramDisplay.
50 REM    LINE/RECT/CIRCLE APPEND to lists every call.
60 REM    PLOT overwrites the same (X,Y) key only.
70 REM
80 REM 2) DRAWING DOES NOT PAINT A FRAMEBUFFER.
90 REM    Each host frame clears the render target to PAPER, then
100 REM    re-issues every retained primitive (see Game1.DrawTextMode).
110 REM
120 REM 3) ANIMATION WITHOUT SPRITES = rebuild the draw list each frame:
130 REM      update variables
140 REM      CLS
150 REM      redraw world + player with RECT/LINE/PLOT
160 REM      YIELD
170 REM      GOTO loop
180 REM
190 REM 4) CLS RETURNS ScreenPresentation.Clear.
200 REM    The interpreter AUTO-YIELDS unless the NEXT statement continues
210 REM    the Clear burst. Continuing statements include:
220 REM      PRINT PRINTAT CLS PLOT LINE RECT CIRCLE INK PAPER FOR NEXT IF
230 REM      GOSUB
240 REM    So CLS → GOSUB draw… → YIELD builds ONE frame (no blank flash).
250 REM    Assignments / bare GOTO after CLS still end the burst (update
260 REM    your variables BEFORE CLS, not between CLS and draw).
270 REM    Explicit YIELD still ends the BASIC frame.
280 REM    See 13_lunar_rescue_clean_loop.bas for the teaching shape.
290 REM    11_lunar_rescue_prototype.bas keeps the older CLS-inside-GOSUB
300 REM    pattern for comparison; both are valid.
310 REM
320 REM 5) Erasing by redrawing in PAPER colour still ADDS primitives
330 REM    (except PLOT same cell). Prefer CLS each frame.
340 REM
350 REM 6) Starfields must CLS+YIELD; endless PLOT without CLS grows the dict.
360 REM
370 REM 7) PRESS-SPACE waits should YIELD each host frame:
380 REM      PRINTAT …,"PRESS SPACE"
390 REM      YIELD
400 REM      IF KEYPRESSED("SPACE")=0 THEN GOTO (the YIELD line)
410 REM    Busy loops without YIELD burn the 700-instruction cap.
420 END
