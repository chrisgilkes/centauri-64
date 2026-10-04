using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class ProgrammingManual
{
    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;

    private KeyboardState _previousKeyboard;

    private static readonly Color Background = new(238, 232, 190);
    private static readonly Color Header = new(36, 72, 110);
    private static readonly Color Cyan = new(91, 214, 205);
    private static readonly Color Yellow = new(232, 205, 92);
    private static readonly Color Ink = new(32, 38, 42);
    private static readonly Color Muted = new(100, 110, 105);

    public event Action? ExitSelected;

    private enum ManualPage
    {
        Contents,
        GettingStarted1,
        GettingStarted2,
        GettingStarted3,
        Basic,
        TextColour,
        Palette,
        Graphics,
        Sprites,
        Sound,
        Input,
        Memory
    }

    private ManualPage _currentPage = ManualPage.Contents;

    public ProgrammingManual(
        BitmapFont font,
        Texture2D whitePixel)
    {
        _font = font;
        _whitePixel = whitePixel;
    }

    public void Open()
    {
        _currentPage = ManualPage.Contents;
        // Ignore the key that opened the manual (bedroom menu uses 2).
        _previousKeyboard = Keyboard.GetState();
    }

    public void Update()
    {
        var keyboard = Keyboard.GetState();

        switch (_currentPage)
        {
            case ManualPage.Contents:
                HandleContents(keyboard);
                break;

            case ManualPage.GettingStarted1:
                Navigate(keyboard, ManualPage.Contents, ManualPage.GettingStarted2);
                break;

            case ManualPage.GettingStarted2:
                Navigate(keyboard, ManualPage.GettingStarted1, ManualPage.GettingStarted3);
                break;

            case ManualPage.GettingStarted3:
                Navigate(keyboard, ManualPage.GettingStarted2, ManualPage.Contents);
                break;

            case ManualPage.TextColour:
                Navigate(keyboard, ManualPage.Contents, ManualPage.Palette);
                break;

            case ManualPage.Palette:
                Navigate(keyboard, ManualPage.TextColour, ManualPage.Contents);
                break;

            default:
                if (Pressed(keyboard, Keys.Escape))
                    _currentPage = ManualPage.Contents;
                break;
        }

        _previousKeyboard = keyboard;
    }

    private void HandleContents(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
            return;
        }

        if (Pressed(keyboard, Keys.D1) || Pressed(keyboard, Keys.NumPad1))
            _currentPage = ManualPage.GettingStarted1;
        else if (Pressed(keyboard, Keys.D2) || Pressed(keyboard, Keys.NumPad2))
            _currentPage = ManualPage.Basic;
        else if (Pressed(keyboard, Keys.D3) || Pressed(keyboard, Keys.NumPad3))
            _currentPage = ManualPage.TextColour;
        else if (Pressed(keyboard, Keys.D4) || Pressed(keyboard, Keys.NumPad4))
            _currentPage = ManualPage.Graphics;
        else if (Pressed(keyboard, Keys.D5) || Pressed(keyboard, Keys.NumPad5))
            _currentPage = ManualPage.Sprites;
        else if (Pressed(keyboard, Keys.D6) || Pressed(keyboard, Keys.NumPad6))
            _currentPage = ManualPage.Sound;
        else if (Pressed(keyboard, Keys.D7) || Pressed(keyboard, Keys.NumPad7))
            _currentPage = ManualPage.Input;
        else if (Pressed(keyboard, Keys.D8) || Pressed(keyboard, Keys.NumPad8))
            _currentPage = ManualPage.Memory;
    }

    private void Navigate(
        KeyboardState keyboard,
        ManualPage left,
        ManualPage right)
    {
        if (Pressed(keyboard, Keys.Escape))
            _currentPage = ManualPage.Contents;
        else if (Pressed(keyboard, Keys.Left))
            _currentPage = left;
        else if (Pressed(keyboard, Keys.Right))
            _currentPage = right;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawBox(
            spriteBatch,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            Background);

        switch (_currentPage)
        {
            case ManualPage.Contents:
                DrawContents(spriteBatch);
                break;
            case ManualPage.GettingStarted1:
                DrawGettingStarted1(spriteBatch);
                break;
            case ManualPage.GettingStarted2:
                DrawGettingStarted2(spriteBatch);
                break;
            case ManualPage.GettingStarted3:
                DrawGettingStarted3(spriteBatch);
                break;
            case ManualPage.Basic:
                DrawBasic(spriteBatch);
                break;
            case ManualPage.TextColour:
                DrawTextColour(spriteBatch);
                break;
            case ManualPage.Palette:
                DrawPalette(spriteBatch);
                break;
            case ManualPage.Graphics:
                DrawGraphics(spriteBatch);
                break;
            case ManualPage.Sprites:
                DrawSprites(spriteBatch);
                break;
            case ManualPage.Sound:
                DrawSound(spriteBatch);
                break;
            case ManualPage.Input:
                DrawInput(spriteBatch);
                break;
            case ManualPage.Memory:
                DrawMemory(spriteBatch);
                break;
        }

        spriteBatch.End();
    }

    private void DrawContents(SpriteBatch spriteBatch)
    {
        DrawBox(spriteBatch, new Rectangle(16, 16, 608, 48), Header);
        DrawText(spriteBatch, "CENTAURI64 PROGRAMMING MANUAL", 184, 24, Color.White);
        DrawText(spriteBatch, "USER GUIDE", 280, 44, Cyan);

        DrawText(spriteBatch, "CONTENTS", 64, 88, Ink);

        DrawText(spriteBatch, "[1]", 64, 120, Header);
        DrawText(spriteBatch, "GETTING STARTED", 104, 120, Ink);
        DrawText(spriteBatch, "[2]", 64, 144, Header);
        DrawText(spriteBatch, "CENTAURI BASIC", 104, 144, Ink);
        DrawText(spriteBatch, "[3]", 64, 168, Header);
        DrawText(spriteBatch, "TEXT & COLOUR", 104, 168, Ink);
        DrawText(spriteBatch, "[4]", 64, 192, Header);
        DrawText(spriteBatch, "GRAPHICS", 104, 192, Ink);
        DrawText(spriteBatch, "[5]", 64, 216, Header);
        DrawText(spriteBatch, "SPRITES", 104, 216, Ink);
        DrawText(spriteBatch, "[6]", 64, 240, Header);
        DrawText(spriteBatch, "SOUND", 104, 240, Ink);
        DrawText(spriteBatch, "[7]", 64, 264, Header);
        DrawText(spriteBatch, "INPUT", 104, 264, Ink);
        DrawText(spriteBatch, "[8]", 64, 288, Header);
        DrawText(spriteBatch, "MEMORY & MACHINE", 104, 288, Ink);

        DrawText(spriteBatch, "LEARN IT. TYPE IT. CHANGE IT.", 184, 336, Muted);

        DrawManualFooter(spriteBatch, "[1]-[8] SELECT       ESC BACK");
    }

    private void DrawGettingStarted1(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "1. GETTING STARTED", "YOUR FIRST PROGRAM");
        DrawLines(spriteBatch, 88,
            "WHEN YOU SEE READY. TYPE:",
            "10 PRINT \"HELLO WORLD\"",
            "PRESS ENTER. NOW TYPE:",
            "RUN",
            "YOUR CENTAURI64 WILL DISPLAY:",
            "HELLO WORLD",
            "PRESS ANY KEY TO RETURN TO THE EDITOR.",
            "PROGRAM LINES START WITH A LINE NUMBER.");
        DrawManualFooter(spriteBatch, "RIGHT NEXT                 ESC CONTENTS");
    }

    private void DrawGettingStarted2(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "1. GETTING STARTED", "MORE THAN ONE LINE");
        DrawLines(spriteBatch, 88,
            "TRY THIS:",
            "10 PRINT \"HELLO\"",
            "20 PRINT \"WELCOME TO CENTAURI64\"",
            "30 PRINT \"READY TO CODE?\"",
            "NOW TYPE RUN.",
            "CENTAURI BASIC RUNS LINES IN",
            "LINE NUMBER ORDER.",
            "TRY CHANGING THE WORDS AND RUN IT AGAIN.");
        DrawManualFooter(spriteBatch, "LEFT PREVIOUS   RIGHT NEXT   ESC CONTENTS");
    }

    private void DrawGettingStarted3(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "1. GETTING STARTED", "MAKING A LOOP");
        DrawLines(spriteBatch, 88,
            "TRY THIS:",
            "10 PRINT \"CENTAURI64\"",
            "20 GOTO 10",
            "GOTO TELLS THE COMPUTER TO CONTINUE",
            "FROM ANOTHER LINE.",
            "LINE 20 SENDS THE PROGRAM BACK TO LINE 10,",
            "SO IT WILL KEEP RUNNING.",
            "PRESS ESC TO BREAK A RUNNING PROGRAM.");
        DrawManualFooter(spriteBatch, "LEFT PREVIOUS              ESC CONTENTS");
    }

    private void DrawBasic(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "2. CENTAURI BASIC", "PROGRAM FLOW");
        DrawReference(spriteBatch,
            ("LET A=10", "ASSIGN A NUMBER"),
            ("A$=\"HI\"", "STRING VARIABLE"),
            ("IF A=1 THEN ...", "BRANCH IF TRUE"),
            ("IF A=1 AND B=2", "AND / OR / NOT"),
            ("GOTO 100", "JUMP TO A LINE"),
            ("GOSUB 200", "CALL A ROUTINE"),
            ("RETURN", "BACK FROM GOSUB"),
            ("FOR I=1 TO 10", "LOOP"),
            ("NEXT I", "END OF LOOP"),
            ("DIM A(20)", "NUMERIC ARRAY"),
            ("REM COMMENT", "IGNORED LINE"),
            ("END", "STOP THE PROGRAM"),
            ("YIELD", "WAIT ONE FRAME"));
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawTextColour(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "3. TEXT & COLOUR", "WORDS ON SCREEN");
        DrawReference(spriteBatch,
            ("PRINT \"HI\"", "WRITE A LINE"),
            ("PRINTAT X,Y,T$", "TEXT AT PIXEL XY"),
            ("INPUT A$", "READ A LINE"),
            ("INPUT \"NAME\";N$", "PROMPT THEN READ"),
            ("INK C", "TEXT COLOUR 0-31"),
            ("PAPER C", "BACKGROUND 0-31"),
            ("CLS", "CLEAR THE SCREEN"),
            ("LEN(A$)", "STRING LENGTH"),
            ("LEFT$(A$,N)", "FIRST N CHARS"),
            ("RIGHT$(A$,N)", "LAST N CHARS"),
            ("MID$(A$,S,N)", "SLICE FROM S"),
            ("UPPER$(A$)", "UPPERCASE COPY"),
            ("A$+B$", "JOIN STRINGS"));
        DrawText(spriteBatch, "RIGHT FOR SYSTEM COLOURS", 48, 392, Muted);
        DrawManualFooter(spriteBatch, "RIGHT NEXT                 ESC CONTENTS");
    }

    private void DrawPalette(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "3. TEXT & COLOUR", "SYSTEM COLOURS");

        var names = new[]
        {
            "BLACK", "WHITE", "RED", "CYAN",
            "PURPLE", "GREEN", "BLUE", "YELLOW",
            "ORANGE", "BROWN", "LT RED", "DK GREY",
            "GREY", "LT GREEN", "LT BLUE", "LT GREY",
            "MIDNIGHT", "NAVY", "TEAL", "AQUA",
            "DK GREEN", "MOSS", "DK BROWN", "TAN",
            "PLUM", "ROSE", "VIOLET", "LAVENDER",
            "GOLD", "PEACH", "STEEL", "WARM WHT"
        };

        for (var i = 0; i < names.Length; i++)
        {
            var column = i < 16 ? 0 : 1;
            var row = i % 16;
            var x = 48 + (column * 300);
            var y = 80 + (row * 20);
            var swatch = new Rectangle(x, y, 12, 12);

            DrawBox(spriteBatch, swatch, CentauriPalette.Get(i));
            DrawText(
                spriteBatch,
                $"{i,2} {names[i]}",
                x + 20,
                y + 2,
                Ink);
        }

        DrawManualFooter(spriteBatch, "LEFT PREVIOUS              ESC CONTENTS");
    }

    private void DrawGraphics(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "4. GRAPHICS", "POINTS LINES SHAPES");
        DrawReference(spriteBatch,
            ("MODE 0", "640X480 TEXT/GFX"),
            ("MODE 1", "320X240 ARCADE"),
            ("PLOT X,Y,C", "ONE PIXEL"),
            ("LINE X1,Y1,X2,Y2,C", ""),
            ("RECT X,Y,W,H,C", ""),
            ("RECT ...,FILL", "FILLED RECTANGLE"),
            ("CIRCLE X,Y,R,C", ""),
            ("CIRCLE ...,FILL", "FILLED CIRCLE"),
            ("SWIDTH", "SCREEN WIDTH"),
            ("SHEIGHT", "SCREEN HEIGHT"),
            ("RND(N)", "0 TO N-1"),
            ("WAIT MS", "PAUSE MS"));
        DrawText(spriteBatch, "COLOURS ARE 0 TO 31", 48, 380, Muted);
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawSprites(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "5. SPRITES", "MOVING PICTURES");
        DrawReference(spriteBatch,
            ("F5", "SPRITE EDITOR"),
            ("SPRITE N,\"NAME\"", "USE A SPRITE"),
            ("SPRITEPOS N,X,Y", "PLACE SPRITE N"),
            ("SPRITESHOW N", "MAKE VISIBLE"),
            ("SPRITEHIDE N", "HIDE SPRITE N"),
            ("SPRITEFLIP N,F", "F=1 OR F=-1"),
            ("SPRITEANIM N,\"A\",L", "PLAY ANIMATION"),
            ("COLLIDE(A,B)", "1 IF OVERLAP"),
            ("ANIMPLAYING(N)", "1 IF ANIM RUNS"),
            ("TDEF ID,\"NAME\"", "TILE FROM SPRITE"),
            ("MAP A,COLS,ROWS", "DRAW ARRAY MAP"),
            ("LOADMAP \"LEVEL\"", "LOAD SAVED MAP"),
            ("TILEAT(X,Y)", "TILE AT PIXEL"),
            ("F6", "MAP EDITOR"));
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawSound(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "6. SOUND", "BEEPS AND RESTS");
        DrawReference(spriteBatch,
            ("BEEP HZ,MS", "TONE FOR MS"),
            ("BEEP 0,MS", "SILENT REST"));
        DrawText(spriteBatch, "BEEP HOLDS UNTIL THE TONE FINISHES.", 48, 140, Muted);
        DrawText(spriteBatch, "EXAMPLE:", 48, 180, Ink);
        DrawText(spriteBatch, "10 BEEP 440,200", 80, 208, Header);
        DrawText(spriteBatch, "20 BEEP 0,100", 80, 232, Header);
        DrawText(spriteBatch, "30 BEEP 880,200", 80, 256, Header);
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawInput(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "7. INPUT", "KEYS AND CONTROLS");
        DrawReference(spriteBatch,
            ("KEY(\"LEFT\")", "1 WHILE HELD"),
            ("KEY(\"RIGHT\")", ""),
            ("KEY(\"UP\")", ""),
            ("KEY(\"DOWN\")", ""),
            ("KEY(\"SPACE\")", ""),
            ("KEYPRESSED(\"SPACE\")", "1 ON PRESS"),
            ("CAMERA X,Y", "SET VIEW"),
            ("CAMERA FOLLOW N", "FOLLOW SPRITE"),
            ("CAMOFF", "RESET CAMERA"));
        DrawText(spriteBatch, "USE YIELD OR WAIT IN GAME LOOPS", 48, 320, Muted);
        DrawText(spriteBatch, "SO THE SCREEN CAN UPDATE.", 48, 344, Muted);
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawMemory(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "8. MEMORY & MACHINE", "TAPES AND TOOLS");
        DrawReference(spriteBatch,
            ("NEW", "CLEAR PROGRAM"),
            ("LIST", "SHOW LISTING"),
            ("LIST 10-100", "LIST A RANGE"),
            ("EDIT 10", "EDIT ONE LINE"),
            ("RUN", "START PROGRAM"),
            ("SAVE \"NAME\"", "WRITE TAPE"),
            ("LOAD \"NAME\"", "READ TAPE"),
            ("DELETE \"NAME\"", "REMOVE TAPE"),
            ("DIR", "LIST TAPES"),
            ("MEM", "BYTES FREE"),
            ("RESET", "RESET DISPLAY"));
        DrawText(spriteBatch, "F5 SPRITES   F6 MAPS   F12 BEDROOM", 48, 360, Muted);
        DrawText(spriteBatch, "TAPES KEEP .BAS .SPRITES .MAPS .TAPE .COVER", 48, 384, Muted);
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawReference(
        SpriteBatch spriteBatch,
        params (string Command, string Note)[] rows)
    {
        var y = 80;

        foreach (var row in rows)
        {
            DrawText(spriteBatch, row.Command, 48, y, Header);

            if (!string.IsNullOrEmpty(row.Note))
                DrawText(spriteBatch, row.Note, 280, y, Ink);

            y += 22;
        }
    }

    private void DrawLines(SpriteBatch spriteBatch, int startY, params string[] lines)
    {
        var y = startY;

        foreach (var line in lines)
        {
            var colour =
                line.StartsWith("10 ") ||
                line.StartsWith("20 ") ||
                line.StartsWith("30 ") ||
                line == "RUN" ||
                line == "HELLO WORLD"
                    ? Header
                    : line.StartsWith("TRY ") ||
                      line.StartsWith("PROGRAM ") ||
                      line.StartsWith("CENTAURI BASIC") ||
                      line.StartsWith("LINE NUMBER") ||
                      line.StartsWith("TRY CHANGING")
                        ? Muted
                        : Ink;

            DrawText(spriteBatch, line, 48, y, colour);
            y += 24;
        }
    }

    private void DrawManualHeader(SpriteBatch spriteBatch, string title, string subtitle)
    {
        DrawBox(spriteBatch, new Rectangle(16, 16, 608, 48), Header);
        DrawText(spriteBatch, title, 224, 24, Color.White);
        DrawText(spriteBatch, subtitle, 232, 44, Cyan);
    }

    private void DrawManualFooter(SpriteBatch spriteBatch, string text)
    {
        DrawBox(spriteBatch, new Rectangle(16, 424, 608, 24), Header);
        DrawText(spriteBatch, text, 48, 432, Color.White);
    }

    private void DrawText(
        SpriteBatch spriteBatch,
        string text,
        int x,
        int y,
        Color colour)
    {
        _font.Draw(spriteBatch, text, new Vector2(x, y), colour);
    }

    private void DrawBox(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color colour)
    {
        spriteBatch.Draw(_whitePixel, rectangle, colour);
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}
