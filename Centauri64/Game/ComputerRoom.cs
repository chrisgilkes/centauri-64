using System;
using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class ComputerRoom
{
    private readonly BitmapFont _font;

    private KeyboardState _previousKeyboard;

    public event Action? ComputerSelected;

    public ComputerRoom(BitmapFont font)
    {
        _font = font;
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

        DrawText(
            spriteBatch,
            "CENTAURI64 COMPUTER ROOM",
            32,
            32,
            Color.Cyan);

        DrawText(
            spriteBatch,
            "1  CENTAURI64 COMPUTER",
            32,
            96,
            Color.White);

        DrawText(
            spriteBatch,
            "   SWITCHED OFF",
            32,
            112,
            Color.Gray);

        DrawText(
            spriteBatch,
            "2  PROGRAMMING MANUAL",
            32,
            160,
            Color.White);

        DrawText(
            spriteBatch,
            "3  MY SOFTWARE",
            32,
            192,
            Color.White);

        DrawText(
            spriteBatch,
            "4  MAGAZINES",
            32,
            224,
            Color.White);

        DrawText(
            spriteBatch,
            "5  NOTICE BOARD",
            32,
            256,
            Color.White);

        DrawText(
            spriteBatch,
            "SELECT 1-5",
            32,
            320,
            Color.Yellow);

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

    private bool Pressed(
        KeyboardState keyboard,
        Keys key)
    {
        return keyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }
}