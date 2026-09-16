using Microsoft.Xna.Framework.Input;
using Centauri64.Console;

namespace Centauri64.Machine;

public sealed class CentauriMachine
{
    private readonly TextConsole _console;

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
}