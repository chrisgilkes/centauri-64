using System;
using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class ComputerRoom
{
    private readonly BitmapFont _font;

    private static readonly Color Background = new(22, 55, 72);
    private static readonly Color Header     = new(36, 72, 110);
    private static readonly Color Cyan       = new(91, 214, 205);
    private static readonly Color Cream      = new(238, 232, 190);
    private static readonly Color Yellow     = new(232, 205, 92);
    private static readonly Color Muted      = new(130, 165, 170);
    private static readonly Color Dark       = new(14, 28, 38);
    private readonly Texture2D _whitePixel;

    private KeyboardState _previousKeyboard;

    public event Action? ComputerSelected;

    private bool _computerPoweredOn;

    public bool ComputerPoweredOn => _computerPoweredOn;

    public ComputerRoom(BitmapFont font, Texture2D whitePixel)
    {
        _font       = font;
        _whitePixel = whitePixel;
    }

    public void SetComputerPoweredOn()
    {
        _computerPoweredOn = true;
    }

    public void Update()
    {
        var keyboard = Keyboard.GetState();

        if (Pressed(keyboard, Keys.D1) ||
            Pressed(keyboard, Keys.NumPad1))
        {
            ComputerSelected?.Invoke();
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        // Background.
        DrawBox(
            spriteBatch,
            new Rectangle(0, 0, 640, 400),
            Background);

        // ---------------------------------------------------------
        // Header
        // ---------------------------------------------------------

        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        DrawText(
            spriteBatch,
            "YOUR BEDROOM",
            272,
            24,
            Cream);

        DrawText(
            spriteBatch,
            "SATURDAY",
            64,
            44,
            Yellow);

        DrawText(
            spriteBatch,
            "CENTAURI64",
            280,
            44,
            Cyan);

        DrawText(
            spriteBatch,
            "10:42 AM",
            504,
            44,
            Yellow);

        DrawText(
            spriteBatch,
            "0101010101010101010101010101010101010101010101010101010101010101010101",
            24,
            72,
            Cyan);

        // ---------------------------------------------------------
        // Centauri64
        // ---------------------------------------------------------

        DrawText(spriteBatch,
            "+-----------------------------+",
            192, 96, Cyan);

        DrawText(spriteBatch,
            "|     **** CENTAURI64 ****    |",
            192, 104, Cyan);

        DrawText(spriteBatch,
            "|                             |",
            192, 112, Cyan);

       var computerStatus = _computerPoweredOn
            ? "|           READY.            |"
            : "|        SWITCHED OFF         |";

        DrawText(
            spriteBatch,
            computerStatus,
            192,
            120,
            _computerPoweredOn ? Cream : Muted);

        DrawText(spriteBatch,
            "|                             |",
            192, 128, Cyan);

        DrawText(spriteBatch,
            "|          TAPE DECK          |",
            192, 136, Cream);

        DrawText(spriteBatch,
            "+-----------------------------+",
            192, 144, Cyan);

        // ---------------------------------------------------------
        // Menu
        // ---------------------------------------------------------

        DrawText(spriteBatch, "[1]", 72, 184, Yellow);
        
        var computerOption = _computerPoweredOn? "USE CENTAURI64": "BOOT UP CENTAURI64";

        DrawText(
            spriteBatch,
            computerOption,
            112,
            184,
            Cream);

        DrawText(spriteBatch, "[2]", 72, 208, Yellow);
        DrawText(spriteBatch, "PROGRAMMING MANUAL", 112, 208, Cream);

        DrawText(spriteBatch, "[3]", 72, 232, Yellow);
        DrawText(spriteBatch, "MY SOFTWARE", 112, 232, Cream);

        DrawText(spriteBatch, "[4]", 72, 256, Yellow);
        DrawText(spriteBatch, "MAGAZINES", 112, 256, Cream);

        DrawText(spriteBatch, "[5]", 72, 280, Yellow);
        DrawText(spriteBatch, "NOTICE BOARD", 112, 280, Cream);

        // ---------------------------------------------------------
        // Footer
        // ---------------------------------------------------------

        DrawText(
            spriteBatch,
            "0101010101010101010101010101010101010101010101010101010101010101010101",
            24,
            328,
            Cyan);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 344, 608, 24),
            Cyan);

        DrawText(
            spriteBatch,
            "[1]-[5] SELECT",
            48,
            352,
            Dark);

        spriteBatch.End();
    }

    private void DrawText(SpriteBatch spriteBatch,string text,int x,int y,Color colour)
    {
        _font.Draw(spriteBatch,text,new Vector2(x, y),colour);
    }

    private void DrawBox(SpriteBatch spriteBatch,Rectangle rectangle,Color colour)
    {
        spriteBatch.Draw(_whitePixel,rectangle,colour);
    }

    private bool Pressed(KeyboardState keyboard,Keys key)
    {
        return keyboard.IsKeyDown(key) &&!_previousKeyboard.IsKeyDown(key);
    }
}