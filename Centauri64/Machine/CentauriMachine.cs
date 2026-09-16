using System;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;

namespace Centauri64.Machine;

public sealed class CentauriMachine
{
    private readonly TextConsole _console;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 400;

    public const int BORDER_SIZE = 8;

    public const int DISPLAY_WIDTH =
        SCREEN_WIDTH + BORDER_SIZE * 2;

    public const int DISPLAY_HEIGHT =
        SCREEN_HEIGHT + BORDER_SIZE * 2;

    public int BorderColour { get; private set; } = 6;

    public CentauriMachine(TextConsole console)
    {
        _console = console;
    }

    public bool IsKeyDown(string keyName)
    {
        var keyboard = Keyboard.GetState();

        return keyName switch
        {
            "LEFT" => keyboard.IsKeyDown(Keys.Left),
            "RIGHT" => keyboard.IsKeyDown(Keys.Right),
            "UP" => keyboard.IsKeyDown(Keys.Up),
            "DOWN" => keyboard.IsKeyDown(Keys.Down),
            "SPACE" => keyboard.IsKeyDown(Keys.Space),

            _ => false
        };
    }

    public void WriteText(int x,int y,string text)
    {
        _console.WriteAt(x, y, text);
    }

    public void ClearScreen()
    {
        _console.Clear();
    }

    public void SetInk(int colour)
    {
        ValidateColour(colour);

        _console.Foreground = colour;
    }

    public void SetBorder(int colour)
    {
        ValidateColour(colour);

        BorderColour = colour;
    }

    public void SetPaper(int colour)
    {
        ValidateColour(colour);

        _console.Background = colour;
    }

    private static void ValidateColour(int colour)
    {
        if (colour < 0 || colour > 15)
        {
            throw new InvalidOperationException(
                "Colour must be between 0 and 15.");
        }
    }
}