using System;
using System.Collections.Generic;

using Centauri64.Basic;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed partial class SoftwareShelf
{
    private const int CoverGridX = 16;
    private const int CoverGridY = 72;
    private const int CoverPixelSize = 3;
    private const int CoverPaletteX = 280;
    private const int CoverPaletteY = 72;
    private const int CoverPaletteCell = 16;
    private const int CoverPaletteColumns = 8;
    private const int CoverPreviewX = 280;
    private const int CoverPreviewY = 152;
    private const int CoverPreviewWidth = 48;
    private const int CoverPreviewHeight = 72;
    private const int CoverUndoLimit = 32;
    private const int InlayCoverX = 448;
    private const int InlayCoverY = 96;
    private const int InlayCoverScale = 2;

    private bool _paintingCover;
    private TapeCover? _cover;
    private int _coverColour = 7;
    private bool _coverStroke;
    private string _coverMessage = "";
    private readonly List<int[,]> _coverUndo = new();

    private void OpenCover()
    {
        _paintingCover = true;
        _cover = _covers[_selected].Clone();
        _coverColour = 7;
        _coverStroke = false;
        _coverMessage = "";
        _coverUndo.Clear();
    }

    private void SaveCover()
    {
        if (_cover == null)
            return;

        var name = _tapes[_selected];

        TapeCover.CopyPixels(_cover.Pixels, _covers[_selected].Pixels);
        _machine.SaveTapeCover(name, _cover);

        _paintingCover = false;
        _cover = null;
        _coverStroke = false;
        _coverUndo.Clear();
    }

    private void UpdateCover(KeyboardState keyboard, MouseState mouse)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            SaveCover();
            return;
        }

        var controlDown =
            keyboard.IsKeyDown(Keys.LeftControl) ||
            keyboard.IsKeyDown(Keys.RightControl);
        var shiftDown =
            keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift);

        if ((controlDown && Pressed(keyboard, Keys.Z)) ||
            (!controlDown && Pressed(keyboard, Keys.U)))
        {
            UndoCover();
            return;
        }

        if (shiftDown && Pressed(keyboard, Keys.C))
        {
            PushCoverUndo();
            _cover?.Clear();
            _coverMessage = "";
            return;
        }

        if (!shiftDown && Pressed(keyboard, Keys.C))
        {
            _coverMessage = "SHIFT+C CLEARS";
            return;
        }

        if (Pressed(keyboard, Keys.H))
        {
            PushCoverUndo();
            FlipCoverHorizontal();
            return;
        }

        if (Pressed(keyboard, Keys.V))
        {
            PushCoverUndo();
            FlipCoverVertical();
            return;
        }

        if (shiftDown && Pressed(keyboard, Keys.Left))
        {
            PushCoverUndo();
            ShiftCover(-1, 0);
            return;
        }

        if (shiftDown && Pressed(keyboard, Keys.Right))
        {
            PushCoverUndo();
            ShiftCover(1, 0);
            return;
        }

        if (shiftDown && Pressed(keyboard, Keys.Up))
        {
            PushCoverUndo();
            ShiftCover(0, -1);
            return;
        }

        if (shiftDown && Pressed(keyboard, Keys.Down))
        {
            PushCoverUndo();
            ShiftCover(0, 1);
            return;
        }

        if (mouse.LeftButton != ButtonState.Pressed &&
            mouse.RightButton != ButtonState.Pressed)
        {
            _coverStroke = false;
        }

        if (mouse.LeftButton == ButtonState.Pressed &&
            TrySelectCoverPalette(mouse.X, mouse.Y))
        {
            return;
        }

        var gridWidth = TapeCover.Width * CoverPixelSize;
        var gridHeight = TapeCover.Height * CoverPixelSize;

        if (mouse.X < CoverGridX ||
            mouse.X >= CoverGridX + gridWidth ||
            mouse.Y < CoverGridY ||
            mouse.Y >= CoverGridY + gridHeight)
        {
            return;
        }

        var pixelX = (mouse.X - CoverGridX) / CoverPixelSize;
        var pixelY = (mouse.Y - CoverGridY) / CoverPixelSize;
        var altDown =
            keyboard.IsKeyDown(Keys.LeftAlt) ||
            keyboard.IsKeyDown(Keys.RightAlt);

        if (mouse.MiddleButton == ButtonState.Pressed ||
            (altDown && mouse.LeftButton == ButtonState.Pressed))
        {
            PickCoverColour(pixelX, pixelY);
            return;
        }

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            BeginCoverStroke();
            PaintCover(pixelX, pixelY, _coverColour);
        }

        if (mouse.RightButton == ButtonState.Pressed)
        {
            BeginCoverStroke();
            PaintCover(pixelX, pixelY, 0);
        }
    }

    private void DrawInlayCover(SpriteBatch spriteBatch)
    {
        var cover = _covers[_selected];
        var width = TapeCover.Width * InlayCoverScale;
        var height = TapeCover.Height * InlayCoverScale;

        DrawText(spriteBatch, "COVER", InlayCoverX, InlayCoverY - 20, Yellow);

        DrawCoverPixels(
            spriteBatch,
            cover,
            InlayCoverX,
            InlayCoverY,
            InlayCoverScale,
            InlayCoverScale);

        DrawCoverFrame(
            spriteBatch,
            InlayCoverX - 2,
            InlayCoverY - 2,
            width + 4,
            height + 4,
            Cream);

        if (!cover.HasArt)
        {
            DrawText(
                spriteBatch,
                "C TO PAINT",
                InlayCoverX,
                InlayCoverY + height + 12,
                Muted);
        }
    }

    private void DrawCoverEditor(SpriteBatch spriteBatch)
    {
        if (_cover == null)
            return;

        DrawText(spriteBatch, "TAPE COVER", 272, 24, Cream);
        DrawText(spriteBatch, _tapes[_selected], 48, 44, Cyan);

        DrawCoverPixels(
            spriteBatch,
            _cover,
            CoverGridX,
            CoverGridY,
            CoverPixelSize,
            CoverPixelSize);

        DrawCoverFrame(
            spriteBatch,
            CoverGridX - 2,
            CoverGridY - 2,
            TapeCover.Width * CoverPixelSize + 4,
            TapeCover.Height * CoverPixelSize + 4,
            Yellow);

        DrawCoverPalette(spriteBatch);

        DrawText(spriteBatch, "PRINT", CoverPreviewX, CoverPreviewY - 16, Yellow);
        DrawCoverPixels(
            spriteBatch,
            _cover,
            CoverPreviewX,
            CoverPreviewY,
            1,
            1);

        var hintY = CoverPreviewY + TapeCover.Height + 16;
        DrawText(spriteBatch, "SHIFT+C CLEAR", CoverPaletteX, hintY, Muted);
        DrawText(spriteBatch, "H/V FLIP", CoverPaletteX, hintY + 16, Muted);
        DrawText(spriteBatch, "SHIFT+ARROWS MOVE", CoverPaletteX, hintY + 32, Muted);

        if (!string.IsNullOrEmpty(_coverMessage))
        {
            DrawText(
                spriteBatch,
                _coverMessage,
                CoverPaletteX,
                hintY + 56,
                Yellow);
        }

        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Cyan);

        DrawText(
            spriteBatch,
            "LMB PAINT  RMB PAPER  ALT PICK  U UNDO  ESC SAVE",
            24,
            432,
            Dark);
    }

    private void DrawCoverFrame(
        SpriteBatch spriteBatch,
        int x,
        int y,
        int width,
        int height,
        Color colour)
    {
        DrawBox(spriteBatch, new Rectangle(x, y, width, 2), colour);
        DrawBox(spriteBatch, new Rectangle(x, y + height - 2, width, 2), colour);
        DrawBox(spriteBatch, new Rectangle(x, y, 2, height), colour);
        DrawBox(spriteBatch, new Rectangle(x + width - 2, y, 2, height), colour);
    }

    private void DrawCoverPalette(SpriteBatch spriteBatch)
    {
        for (var colourIndex = 0;
            colourIndex < CentauriPalette.MAX_COLORS;
            colourIndex++)
        {
            var column = colourIndex % CoverPaletteColumns;
            var row = colourIndex / CoverPaletteColumns;
            var x = CoverPaletteX + (column * CoverPaletteCell);
            var y = CoverPaletteY + (row * CoverPaletteCell);

            DrawBox(
                spriteBatch,
                new Rectangle(
                    x + 1,
                    y + 1,
                    CoverPaletteCell - 2,
                    CoverPaletteCell - 2),
                CentauriPalette.Get(colourIndex));

            if (colourIndex == _coverColour)
            {
                DrawBox(
                    spriteBatch,
                    new Rectangle(
                        x - 2,
                        y - 2,
                        CoverPaletteCell + 4,
                        2),
                    Cream);
                DrawBox(
                    spriteBatch,
                    new Rectangle(
                        x - 2,
                        y + CoverPaletteCell,
                        CoverPaletteCell + 4,
                        2),
                    Cream);
                DrawBox(
                    spriteBatch,
                    new Rectangle(
                        x - 2,
                        y,
                        2,
                        CoverPaletteCell),
                    Cream);
                DrawBox(
                    spriteBatch,
                    new Rectangle(
                        x + CoverPaletteCell,
                        y,
                        2,
                        CoverPaletteCell),
                    Cream);
            }
        }
    }

    private void DrawCoverPreview(
        SpriteBatch spriteBatch,
        TapeCover cover,
        int x,
        int y,
        int width,
        int height)
    {
        for (var dy = 0; dy < height; dy++)
        {
            for (var dx = 0; dx < width; dx++)
            {
                var sx = dx * TapeCover.Width / width;
                var sy = dy * TapeCover.Height / height;
                var colour = cover.Pixels[sy, sx];

                DrawBox(
                    spriteBatch,
                    new Rectangle(x + dx, y + dy, 1, 1),
                    CentauriPalette.Get(colour));
            }
        }
    }

    private void DrawCoverPixels(
        SpriteBatch spriteBatch,
        TapeCover cover,
        int x,
        int y,
        int pixelWidth,
        int pixelHeight)
    {
        for (var py = 0; py < TapeCover.Height; py++)
        {
            for (var px = 0; px < TapeCover.Width; px++)
            {
                DrawBox(
                    spriteBatch,
                    new Rectangle(
                        x + (px * pixelWidth),
                        y + (py * pixelHeight),
                        pixelWidth,
                        pixelHeight),
                    CentauriPalette.Get(cover.Pixels[py, px]));
            }
        }
    }

    private bool TrySelectCoverPalette(int mouseX, int mouseY)
    {
        var paletteWidth = CoverPaletteColumns * CoverPaletteCell;
        var paletteRows = CentauriPalette.MAX_COLORS / CoverPaletteColumns;
        var paletteHeight = paletteRows * CoverPaletteCell;

        if (mouseX < CoverPaletteX ||
            mouseX >= CoverPaletteX + paletteWidth ||
            mouseY < CoverPaletteY ||
            mouseY >= CoverPaletteY + paletteHeight)
        {
            return false;
        }

        var column = (mouseX - CoverPaletteX) / CoverPaletteCell;
        var row = (mouseY - CoverPaletteY) / CoverPaletteCell;
        var colourIndex = (row * CoverPaletteColumns) + column;

        if (colourIndex >= CentauriPalette.MAX_COLORS)
            return false;

        _coverColour = colourIndex;
        return true;
    }

    private void PickCoverColour(int pixelX, int pixelY)
    {
        if (_cover == null)
            return;

        _coverColour = _cover.Pixels[pixelY, pixelX];
    }

    private void PaintCover(int pixelX, int pixelY, int colour)
    {
        if (_cover == null)
            return;

        _cover.Pixels[pixelY, pixelX] = colour;
    }

    private void BeginCoverStroke()
    {
        if (_coverStroke)
            return;

        PushCoverUndo();
        _coverStroke = true;
        _coverMessage = "";
    }

    private void PushCoverUndo()
    {
        if (_cover == null)
            return;

        _coverUndo.Add(CloneCoverPixels(_cover.Pixels));

        if (_coverUndo.Count > CoverUndoLimit)
            _coverUndo.RemoveAt(0);
    }

    private void UndoCover()
    {
        if (_cover == null || _coverUndo.Count == 0)
        {
            _coverMessage = "NOTHING TO UNDO";
            return;
        }

        var pixels = _coverUndo[^1];
        _coverUndo.RemoveAt(_coverUndo.Count - 1);
        TapeCover.CopyPixels(pixels, _cover.Pixels);
        _coverMessage = "";
    }

    private static int[,] CloneCoverPixels(int[,] source)
    {
        var copy = new int[TapeCover.Height, TapeCover.Width];
        TapeCover.CopyPixels(source, copy);
        return copy;
    }

    private void FlipCoverHorizontal()
    {
        if (_cover == null)
            return;

        for (var y = 0; y < TapeCover.Height; y++)
        {
            for (var x = 0; x < TapeCover.Width / 2; x++)
            {
                var opposite = TapeCover.Width - 1 - x;
                var temp = _cover.Pixels[y, x];
                _cover.Pixels[y, x] = _cover.Pixels[y, opposite];
                _cover.Pixels[y, opposite] = temp;
            }
        }
    }

    private void FlipCoverVertical()
    {
        if (_cover == null)
            return;

        for (var y = 0; y < TapeCover.Height / 2; y++)
        {
            var opposite = TapeCover.Height - 1 - y;

            for (var x = 0; x < TapeCover.Width; x++)
            {
                var temp = _cover.Pixels[y, x];
                _cover.Pixels[y, x] = _cover.Pixels[opposite, x];
                _cover.Pixels[opposite, x] = temp;
            }
        }
    }

    private void ShiftCover(int offsetX, int offsetY)
    {
        if (_cover == null)
            return;

        var shifted = new int[TapeCover.Height, TapeCover.Width];

        for (var y = 0; y < TapeCover.Height; y++)
        {
            for (var x = 0; x < TapeCover.Width; x++)
            {
                var nx = x + offsetX;
                var ny = y + offsetY;

                if (nx < 0 || nx >= TapeCover.Width ||
                    ny < 0 || ny >= TapeCover.Height)
                {
                    continue;
                }

                shifted[ny, nx] = _cover.Pixels[y, x];
            }
        }

        TapeCover.CopyPixels(shifted, _cover.Pixels);
    }
}
