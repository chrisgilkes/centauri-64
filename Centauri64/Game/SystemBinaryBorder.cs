using System.Collections.Generic;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Game;

/// <summary>
/// Shared clockwise 0/1 bezel animation used by Centauri64 system screens.
/// </summary>
internal sealed class SystemBinaryBorder
{
    public const float CellsPerSecond = 10f;
    private const int Margin = 2;
    private const int Step = 8;

    private readonly Point[] _perimeter;
    private float _scroll;

    public SystemBinaryBorder()
    {
        _perimeter = BuildPerimeter();
    }

    public void Update(GameTime gameTime)
    {
        _scroll += (float)gameTime.ElapsedGameTime.TotalSeconds * CellsPerSecond;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        BitmapFont font,
        Color lit,
        Color dim)
    {
        var offset = (int)_scroll;

        for (var i = 0; i < _perimeter.Length; i++)
        {
            var cell = _perimeter[i];
            var bit = (i - offset) & 1;
            BootUi.DrawText(
                font,
                spriteBatch,
                bit == 0 ? "0" : "1",
                cell.X,
                cell.Y,
                bit == 0 ? lit : dim);
        }
    }

    private static Point[] BuildPerimeter()
    {
        const int width = CentauriMachine.DEVELOPMENT_WIDTH;
        const int height = CentauriMachine.DEVELOPMENT_HEIGHT;
        var cells = new List<Point>((width + height) * 2 / Step);

        for (var x = Margin; x <= width - Margin - Step; x += Step)
            cells.Add(new Point(x, Margin));

        for (var y = Margin + Step; y <= height - Margin - Step; y += Step)
            cells.Add(new Point(width - Margin - Step, y));

        for (var x = width - Margin - Step; x >= Margin; x -= Step)
            cells.Add(new Point(x, height - Margin - Step));

        for (var y = height - Margin - Step - Step; y >= Margin + Step; y -= Step)
            cells.Add(new Point(Margin, y));

        return cells.ToArray();
    }
}
