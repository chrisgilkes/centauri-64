using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Session;
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
        Images,
        Sprites,
        Sound,
        Input,
        Memory,
        Hardware,
        Network1,
        Network2,
        Network3
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

            case ManualPage.Memory:
                Navigate(keyboard, ManualPage.Contents, ManualPage.Hardware);
                break;

            case ManualPage.Hardware:
                Navigate(keyboard, ManualPage.Memory, ManualPage.Contents);
                break;

            case ManualPage.Network1:
                Navigate(keyboard, ManualPage.Contents, ManualPage.Network2);
                break;

            case ManualPage.Network2:
                Navigate(keyboard, ManualPage.Network1, ManualPage.Network3);
                break;

            case ManualPage.Network3:
                Navigate(keyboard, ManualPage.Network2, ManualPage.Contents);
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
        {
            if (Unlocked(FeatureId.Graphics))
                _currentPage = ManualPage.Graphics;
        }
        else if (Pressed(keyboard, Keys.D0) || Pressed(keyboard, Keys.NumPad0))
        {
            if (Unlocked(FeatureId.Images))
                _currentPage = ManualPage.Images;
        }
        else if (Pressed(keyboard, Keys.D5) || Pressed(keyboard, Keys.NumPad5))
        {
            if (Unlocked(FeatureId.Sprites))
                _currentPage = ManualPage.Sprites;
        }
        else if (Pressed(keyboard, Keys.D6) || Pressed(keyboard, Keys.NumPad6))
            _currentPage = ManualPage.Sound;
        else if (Pressed(keyboard, Keys.D7) || Pressed(keyboard, Keys.NumPad7))
            _currentPage = ManualPage.Input;
        else if (Pressed(keyboard, Keys.D8) || Pressed(keyboard, Keys.NumPad8))
            _currentPage = ManualPage.Memory;
        else if (Pressed(keyboard, Keys.D9) || Pressed(keyboard, Keys.NumPad9))
        {
            if (Unlocked(FeatureId.Networking))
                _currentPage = ManualPage.Network1;
        }
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
            case ManualPage.Images:
                DrawImages(spriteBatch);
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
            case ManualPage.Hardware:
                DrawHardware(spriteBatch);
                break;
            case ManualPage.Network1:
                DrawNetwork1(spriteBatch);
                break;
            case ManualPage.Network2:
                DrawNetwork2(spriteBatch);
                break;
            case ManualPage.Network3:
                DrawNetwork3(spriteBatch);
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

        var y = 192;
        if (Unlocked(FeatureId.Graphics))
        {
            DrawText(spriteBatch, "[4]", 64, y, Header);
            DrawText(spriteBatch, "GRAPHICS", 104, y, Ink);
            y += 24;
        }
        if (Unlocked(FeatureId.Images))
        {
            DrawText(spriteBatch, "[0]", 64, y, Header);
            DrawText(spriteBatch, "IMAGES / SCREEN IMAGES", 104, y, Ink);
            y += 24;
        }

        if (Unlocked(FeatureId.Sprites))
        {
            DrawText(spriteBatch, "[5]", 64, y, Header);
            DrawText(spriteBatch, "SPRITES", 104, y, Ink);
            y += 24;
        }

        DrawText(spriteBatch, "[6]", 64, y, Header);
        DrawText(spriteBatch, "SOUND", 104, y, Ink);
        y += 24;
        DrawText(spriteBatch, "[7]", 64, y, Header);
        DrawText(spriteBatch, "INPUT", 104, y, Ink);
        y += 24;
        DrawText(spriteBatch, "[8]", 64, y, Header);
        DrawText(spriteBatch, "MEMORY & MACHINE", 104, y, Ink);
        y += 24;

        if (Unlocked(FeatureId.Networking))
        {
            DrawText(spriteBatch, "[9]", 64, y, Header);
            DrawText(spriteBatch, "NETWORK BASIC", 104, y, Ink);
        }

        DrawText(spriteBatch, "LEARN IT. TYPE IT. CHANGE IT.", 184, 380, Muted);
        DrawManualFooter(spriteBatch, "[0]-[9] SELECT       ESC BACK");
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
            ("MODE 1", "640X480 HIGH RES"),
            ("MODE 2", "320X240 ARCADE"),
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
        DrawText(spriteBatch, "DEFAULT BASIC NEEDS NO MODE. COLOURS 0 TO 31", 48, 380, Muted);
        DrawManualFooter(spriteBatch, "ESC CONTENTS");
    }

    private void DrawImages(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "0. IMAGES", "GENERAL PIXEL ARTWORK");
        DrawReference(spriteBatch,
            ("F7", "IMAGE EDITOR"),
            ("IMAGE \"N\",X,Y", "DRAW IMAGE AT X,Y"),
            ("IMAGE \"N\"", "DRAW AT 0,0"),
            ("IMAGE \"N\",X,Y,F", "DRAW FRAME F (0+)"),
            ("IMAGE OFF", "CLEAR IMAGE DRAWS"),
            ("BG 0,\"N\"", "FULL-SCREEN BG LAYER 0"),
            ("BG 1,\"N\"", "FULL-SCREEN BG LAYER 1"),
            ("FG \"N\"", "FULL-SCREEN FOREGROUND"),
            ("BG/FG OFF", "HIDE LAYER"));
        DrawText(spriteBatch, "IMAGES: VARIABLE SIZE + FRAMES. CATEGORIES:", 48, 250, Muted);
        DrawText(spriteBatch, "GENERAL / SPRITE / TILESET / BACKGROUND", 48, 274, Muted);
        DrawText(spriteBatch, "IMAGE DRAWS ONCE (CLS CLEARS). BG/FG PERSIST.", 48, 298, Muted);
        DrawText(spriteBatch, "BG/FG NEED FULL-SCREEN SIZE FOR CURRENT MODE.", 48, 322, Muted);
        DrawText(spriteBatch, "10 CLS", 80, 352, Header);
        DrawText(spriteBatch, "20 IMAGE \"FOREST\",80,8", 80, 376, Header);
        DrawText(spriteBatch, "30 PRINT \"YOU ARE IN A DARK FOREST.\"", 80, 400, Header);
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
        DrawText(spriteBatch, "RIGHT FOR HARDWARE SPECS", 48, 384, Muted);
        DrawManualFooter(spriteBatch, "RIGHT NEXT                 ESC CONTENTS");
    }

    private void DrawHardware(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "8. MEMORY & MACHINE", "HARDWARE SPECS");
        DrawReference(spriteBatch,
            ("SYSTEM RAM", "64K"),
            ("BASIC RAM", "48K"),
            ("MODE 1", "640X480"),
            ("MODE 2", "320X240"),
            ("COLOURS", "32  (0-31)"),
            ("SPRITES", "64  (0-63)"),
            ("SOUND", "BEEP TONE"),
            ("KEYS", "ARROWS SPACE 0-9 A-Z"),
            ("NETWORK", "2 PLAYERS"),
            ("MEM", "SHOW FREE BASIC RAM"));
        DrawText(spriteBatch, "MODE 2 IS THE ARCADE GAME SCREEN.", 48, 360, Muted);
        DrawText(spriteBatch, "USE SWIDTH AND SHEIGHT FOR THE ACTIVE MODE.", 48, 384, Muted);
        DrawManualFooter(spriteBatch, "LEFT PREVIOUS              ESC CONTENTS");
    }

    private void DrawNetwork1(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "9. NETWORK BASIC", "TWO PLAYER GAMES");
        DrawLines(spriteBatch, 80,
            "CENTAURI64 CAN LINK TWO PLAYERS.",
            "PLAYER 1 HOSTS. PLAYER 2 JOINS.",
            "BOTH MUST RUN THE SAME PROGRAM.",
            "",
            "FOR LOCAL TESTING, RUN TWO COPIES",
            "OF CENTAURI64 ON ONE COMPUTER.",
            "LOAD THE SAME TAPE ON BOTH, THEN",
            "HOST ON ONE AND JOIN ON THE OTHER.",
            "",
            "NET HOST     START A GAME",
            "NET JOIN     JOIN A GAME",
            "NET WAIT     WAIT FOR PLAYER 2",
            "NET LEAVE    LEAVE THE SESSION");
        DrawManualFooter(spriteBatch, "RIGHT NEXT                 ESC CONTENTS");
    }

    private void DrawNetwork2(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "9. NETWORK BASIC", "SEND AND RECEIVE");
        DrawReference(spriteBatch,
            ("NET SEND \"X\",N", "SHARE A NUMBER"),
            ("V=NET(\"X\")", "READ THEIR VALUE"),
            ("NETPLAYER", "0 NONE 1 HOST 2 JOIN"),
            ("NETCONNECTED", "1 WHILE LINKED"));
        DrawText(spriteBatch, "NET SEND SETS THE LATEST VALUE.", 48, 200, Muted);
        DrawText(spriteBatch, "SHORT NAMES HELP: P1Y BX SCORE", 48, 224, Muted);
        DrawText(spriteBatch, "ONLY NUMBERS ARE SENT IN V1.", 48, 248, Muted);
        DrawText(spriteBatch, "NET() RETURNS 0 UNTIL A VALUE ARRIVES.", 48, 272, Muted);
        DrawText(spriteBatch, "CHECK NETCONNECTED EACH FRAME.", 48, 296, Muted);
        DrawManualFooter(spriteBatch, "LEFT PREVIOUS   RIGHT NEXT   ESC CONTENTS");
    }

    private void DrawNetwork3(SpriteBatch spriteBatch)
    {
        DrawManualHeader(spriteBatch, "9. NETWORK BASIC", "FIRST NETWORK PROGRAM");
        DrawLines(spriteBatch, 80,
            "HOST:",
            "  NET HOST",
            "  NET WAIT",
            "  NET SEND \"TEST\",123",
            "",
            "JOIN:",
            "  NET JOIN",
            "  IF NETCONNECTED=0 THEN ...",
            "  X=NET(\"TEST\")",
            "",
            "TRY LOAD \"NETTEST\" OR LOAD \"PONG\".",
            "BOTH MACHINES MUST LOAD THE SAME",
            "TAPE BEFORE RUNNING.");
        DrawManualFooter(spriteBatch, "LEFT PREVIOUS              ESC CONTENTS");
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

    private static bool Unlocked(FeatureId feature) =>
        FeatureGate.Current.IsAvailable(feature);

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}
