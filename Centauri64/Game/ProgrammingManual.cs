using System;
using Centauri64.Graphics;
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
    private static readonly Color Header     = new(36, 72, 110);
    private static readonly Color Cyan       = new(91, 214, 205);
    private static readonly Color Yellow     = new(232, 205, 92);
    private static readonly Color Ink        = new(32, 38, 42);
    private static readonly Color Muted      = new(100, 110, 105);

    public event Action? ExitSelected;

    private enum ManualPage
    {
        Contents,
        GettingStarted
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

        if (Pressed(keyboard, Keys.Escape))
        {
            ExitSelected?.Invoke();
        }

        if (_currentPage == ManualPage.Contents)
        {
            if (Pressed(keyboard, Keys.D1) ||
                Pressed(keyboard, Keys.NumPad1))
            {
                _currentPage = ManualPage.GettingStarted;
            }

            if (Pressed(keyboard, Keys.Escape))
            {
                ExitSelected?.Invoke();
            }
        }
        else
        {
            if (Pressed(keyboard, Keys.Escape))
            {
                _currentPage = ManualPage.Contents;
            }
        }

        _previousKeyboard = keyboard;
    }


    private void DrawContents(SpriteBatch spriteBatch)
    {
         DrawBox(
            spriteBatch,
            new Rectangle(0, 0, 640, 400),
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
            new Rectangle(16, 344, 608, 24),
            Header);

        DrawText(
            spriteBatch,
            "[1]-[8] SELECT       ESC BACK",
            48,
            352,
            Color.White);
    }

    private void DrawGettingStarted(SpriteBatch spriteBatch)
    {
        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        DrawText(
            spriteBatch,
            "1. GETTING STARTED",
            224,
            24,
            Color.White);

        DrawText(
            spriteBatch,
            "YOUR FIRST PROGRAM",
            232,
            44,
            Cyan);

        DrawText(spriteBatch,
            "WHEN THE CENTAURI64 IS READY, TYPE:",
            48, 88, Ink);

        DrawText(spriteBatch,
            "10 PRINT \"HELLO WORLD\"",
            80, 120, Header);

        DrawText(spriteBatch,
            "PRESS ENTER, THEN TYPE:",
            48, 152, Ink);

        DrawText(spriteBatch,
            "RUN",
            80, 176, Header);

        DrawText(spriteBatch,
            "THE COMPUTER WILL PRINT:",
            48, 208, Ink);

        DrawText(spriteBatch,
            "HELLO WORLD",
            80, 232, Header);

        DrawText(spriteBatch,
            "TRY CHANGING THE MESSAGE AND RUN IT AGAIN.",
            48, 272, Ink);

        DrawText(spriteBatch,
            "YOU HAVE WRITTEN YOUR FIRST CENTAURI BASIC PROGRAM!",
            48, 296, Muted);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 344, 608, 24),
            Header);

        DrawText(
            spriteBatch,
            "ESC CONTENTS",
            48,
            352,
            Color.White);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        DrawBox(
            spriteBatch,
            new Rectangle(0, 0, 640, 400),
            Background);

        switch (_currentPage)
        {
            case ManualPage.Contents:
                DrawContents(spriteBatch);
                break;

            case ManualPage.GettingStarted:
                DrawGettingStarted(spriteBatch);
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