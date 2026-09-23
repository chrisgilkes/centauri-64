
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

    public void Print(string text)
    {
        _programConsole.WriteLine(text);
    }

    public void WriteText(int x, int y, string text)
    {
        var existing = _positionedText.FindIndex(
            item => item.X == x && item.Y == y);

        var positionedText = new PositionedText(x,y,text,_programConsole.Foreground);

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
            font.Draw(spriteBatch,item.Text,new Vector2(item.X, item.Y),CentauriPalette.Get(item.Colour));
        }
    }
}