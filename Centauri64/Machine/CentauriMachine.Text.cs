
using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using Centauri64.Graphics;


namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    private sealed class PositionedText
    {
        public int X { get; }
        public int Y { get; }
        public string Text { get; }
        public int Colour { get; }

        public PositionedText(int x,int y,string text,int colour)
        {
            X = x;
            Y = y;
            Text = text;
            Colour = colour;
        }
    }

    private readonly List<PositionedText> _positionedText = new();

    /// <summary>Retained PRINTAT count (cleared by CLS). For headless verification.</summary>
    public int RetainedTextCount => _positionedText.Count;

    public void Print(string text)
    {
        ActiveTextConsole.WriteLine(text);
    }

    public void BeginInput(string prompt)
    {
        ActiveTextConsole.Write(prompt);
        ActiveTextConsole.BeginInput();
    }

    public void CancelInput()
    {
        _console.CancelInput();
        _programConsole.CancelInput();
    }

    public void WriteText(int x, int y, string text)
    {
        var existing = _positionedText.FindIndex(
            item => item.X == x && item.Y == y);

        var positionedText = new PositionedText(x,y,text,ActiveTextConsole.Foreground);

        if (existing >= 0)
        {
            _positionedText[existing] = positionedText;
        }
        else
        {
            _positionedText.Add(positionedText);
        }
    }

    public void DrawText(SpriteBatch spriteBatch,BitmapFont font)
    {
        foreach (var item in _positionedText)
        {
            font.Draw(
                spriteBatch,
                item.Text,
                new Vector2(item.X - _cameraX, item.Y - _cameraY),
                CentauriPalette.Get(item.Colour));
        }
    }
}