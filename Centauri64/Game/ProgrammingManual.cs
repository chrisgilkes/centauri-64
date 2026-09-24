using System;
using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Machine;

namespace Centauri64.Game;

public sealed class ProgrammingManual
{
    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;

    private KeyboardState _previousKeyboard;

    private static readonly Color Background = new(238, 232, 190);
    private static readonly Color Header     = new(36, 72, 110);
    private static readonly Color Cyan       = new(91, 214, 205);
    private static readonly Color Yellow     = new(232, 205, 92);
    private static readonly Color Ink        = new(32, 38, 42);
    private static readonly Color Muted      = new(100, 110, 105);

    public event Action? ExitSelected;

    private enum ManualPage
    {
        Contents,
        GettingStarted1,
        GettingStarted2,
        GettingStarted3
    }

    private ManualPage _currentPage = ManualPage.Contents;

    public ProgrammingManual(
        BitmapFont font,
        Texture2D whitePixel)
    {
        _font = font;
        _whitePixel = whitePixel;
    }

    public void Update()
    {
        var keyboard = Keyboard.GetState();

        switch (_currentPage)
        {
            case ManualPage.Contents:
            {
                if (Pressed(keyboard, Keys.D1) ||
                    Pressed(keyboard, Keys.NumPad1))
                {
                    _currentPage = ManualPage.GettingStarted1;
                }
                else if (Pressed(keyboard, Keys.Escape))
                {
                    ExitSelected?.Invoke();
                }

                break;
            }

            case ManualPage.GettingStarted1:
            {
                if (Pressed(keyboard, Keys.Right))
                {
                    _currentPage = ManualPage.GettingStarted2;
                }
                else if (Pressed(keyboard, Keys.Escape))
                {
                    _currentPage = ManualPage.Contents;
                }

                break;
            }

            case ManualPage.GettingStarted2:
            {
                if (Pressed(keyboard, Keys.Left))
                {
                    _currentPage = ManualPage.GettingStarted1;
                }
                else if (Pressed(keyboard, Keys.Right))
                {
                    _currentPage = ManualPage.GettingStarted3;
                }
                else if (Pressed(keyboard, Keys.Escape))
                {
                    _currentPage = ManualPage.Contents;
                }

                break;
            }

            case ManualPage.GettingStarted3:
            {
                if (Pressed(keyboard, Keys.Left))
                {
                    _currentPage = ManualPage.GettingStarted2;
                }
                else if (Pressed(keyboard, Keys.Escape))
                {
                    _currentPage = ManualPage.Contents;
                }

                break;
            }
        }

        _previousKeyboard = keyboard;
    }


    private void DrawContents(SpriteBatch spriteBatch)
    {
         DrawBox(
            spriteBatch,
            new Rectangle(
            0,
            0,
            CentauriMachine.DEVELOPMENT_WIDTH,
            CentauriMachine.DEVELOPMENT_HEIGHT),
            Background);

            // Header.
        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        DrawText(
            spriteBatch,
            "CENTAURI64 PROGRAMMING MANUAL",
            184,
            24,
            Color.White);

        DrawText(
            spriteBatch,
            "USER GUIDE",
            280,
            44,
            Cyan);

        // Contents.
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

        DrawText(
            spriteBatch,
            "LEARN IT. TYPE IT. CHANGE IT.",
            184,
            320,
            Muted);

        // Footer.
        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Header);

        DrawText(
            spriteBatch,
            "[1]-[8] SELECT       ESC BACK",
            48,
            432,
            Color.White);
    }

    private void DrawGettingStarted1(SpriteBatch spriteBatch)
    {
        DrawManualHeader(
            spriteBatch,
            "1. GETTING STARTED",
            "YOUR FIRST PROGRAM");

        DrawText(spriteBatch,
            "WHEN YOU SEE READY. TYPE:",
            48, 88, Ink);

        DrawText(spriteBatch,
            "10 PRINT \"HELLO WORLD\"",
            80, 120, Header);

        DrawText(spriteBatch,
            "PRESS ENTER. NOW TYPE:",
            48, 152, Ink);

        DrawText(spriteBatch,
            "RUN",
            80, 176, Header);

        DrawText(spriteBatch,
            "YOUR CENTAURI64 WILL DISPLAY:",
            48, 208, Ink);

        DrawText(spriteBatch,
            "HELLO WORLD",
            80, 232, Header);

        DrawText(spriteBatch,
            "PRESS ANY KEY TO RETURN TO THE EDITOR.",
            48, 264, Ink);

        DrawText(spriteBatch,
            "PROGRAM LINES START WITH A LINE NUMBER.",
            48, 296, Muted);

        DrawManualFooter(
            spriteBatch,
            "RIGHT NEXT                 ESC CONTENTS");
    }

    private void DrawGettingStarted2(SpriteBatch spriteBatch)
    {
        DrawManualHeader(
            spriteBatch,
            "1. GETTING STARTED",
            "MORE THAN ONE LINE");

        DrawText(spriteBatch,
            "TRY THIS:",
            48, 88, Ink);

        DrawText(spriteBatch,
            "10 PRINT \"HELLO\"",
            80, 120, Header);

        DrawText(spriteBatch,
            "20 PRINT \"WELCOME TO CENTAURI64\"",
            80, 144, Header);

        DrawText(spriteBatch,
            "30 PRINT \"READY TO CODE?\"",
            80, 168, Header);

        DrawText(spriteBatch,
            "NOW TYPE RUN.",
            48, 208, Ink);

        DrawText(spriteBatch,
            "CENTAURI BASIC RUNS LINES IN",
            48, 240, Ink);

        DrawText(spriteBatch,
            "LINE NUMBER ORDER.",
            48, 264, Ink);

        DrawText(spriteBatch,
            "TRY CHANGING THE WORDS AND RUN IT AGAIN.",
            48, 296, Muted);

        DrawManualFooter(
            spriteBatch,
            "LEFT PREVIOUS   RIGHT NEXT   ESC CONTENTS");
    }

    private void DrawGettingStarted3(SpriteBatch spriteBatch)
    {
        DrawManualHeader(
            spriteBatch,
            "1. GETTING STARTED",
            "MAKING A LOOP");

        DrawText(spriteBatch,
            "TRY THIS:",
            48, 88, Ink);

        DrawText(spriteBatch,
            "10 PRINT \"CENTAURI64\"",
            80, 120, Header);

        DrawText(spriteBatch,
            "20 GOTO 10",
            80, 144, Header);

        DrawText(spriteBatch,
            "GOTO TELLS THE COMPUTER TO CONTINUE",
            48, 184, Ink);

        DrawText(spriteBatch,
            "FROM ANOTHER LINE.",
            48, 208, Ink);

        DrawText(spriteBatch,
            "LINE 20 SENDS THE PROGRAM BACK TO LINE 10,",
            48, 240, Ink);

        DrawText(spriteBatch,
            "SO IT WILL KEEP RUNNING.",
            48, 264, Ink);

        DrawText(spriteBatch,
            "PRESS ESC TO BREAK A RUNNING PROGRAM.",
            48, 296, Header);

        DrawManualFooter(
            spriteBatch,
            "LEFT PREVIOUS              ESC CONTENTS");
    }

    private void DrawManualHeader(SpriteBatch spriteBatch,string title,string subtitle)
    {
        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        DrawText(
            spriteBatch,
            title,
            224,
            24,
            Color.White);

        DrawText(
            spriteBatch,
            subtitle,
            232,
            44,
            Cyan);
    }

    private void DrawManualFooter(SpriteBatch spriteBatch,string text)
    {
        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Header);

        DrawText(
            spriteBatch,
            text,
            48,
            432,
            Color.White);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

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
        }

        spriteBatch.End();
    }

    private void DrawText(
        SpriteBatch spriteBatch,
        string text,
        int x,
        int y,
        Color colour)
    {
        _font.Draw(
            spriteBatch,
            text,
            new Vector2(x, y),
            colour);
    }

    private void DrawBox(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color colour)
    {
        spriteBatch.Draw(
            _whitePixel,
            rectangle,
            colour);
    }

    private bool Pressed(
        KeyboardState keyboard,
        Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}