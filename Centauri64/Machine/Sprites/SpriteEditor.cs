using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Sprites;

public sealed class SpriteEditor
{
    private readonly SpriteAssetStore _assets;

    private SpriteAsset? _asset;
    private SpriteAnimation? _animation;
    private SpriteFrame? _frame;

    public bool IsActive { get; private set; }

    public int SelectedColour { get; private set; } = 7;

    private const int GridX = 80;
    private const int GridY = 40;

    private const int PixelSize = 20;

    private const int PaletteX = 440;
    private const int PaletteY = 60;

    private const int PaletteCellSize = 24;
    private const int PaletteColumns = 4;

    public SpriteEditor(SpriteAssetStore assets)
    {
        _assets = assets;
    }

    public void Open(string assetName)
    {
        _asset =
            _assets.Get(assetName);

        _animation =
            _asset.GetAnimation("DEFAULT");

        if (_animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "Sprite has no frames.");
        }

        _frame =
            _animation.Frames[0];

        IsActive = true;
    }

    public void Update( MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        if (!IsActive || _frame == null)
        return;

        if (keyboard.IsKeyDown(Keys.Escape) &&
            previousKeyboard.IsKeyUp(Keys.Escape))
        {
            Close();
            return;
        }

        if (keyboard.IsKeyDown(Keys.C) &&
            previousKeyboard.IsKeyUp(Keys.C))
        {
            _frame.Clear();
            return;
        }

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            if (TrySelectPaletteColour(
                    mouse.X,
                    mouse.Y))
            {
                return;
            }
        }

        var gridWidth =
            CentauriSprite.WIDTH * PixelSize;

        var gridHeight =
            CentauriSprite.HEIGHT * PixelSize;

        if (mouse.X < GridX ||
            mouse.X >= GridX + gridWidth ||
            mouse.Y < GridY ||
            mouse.Y >= GridY + gridHeight)
        {
            return;
        }

        var spriteX =
            (mouse.X - GridX) / PixelSize;

        var spriteY =
            (mouse.Y - GridY) / PixelSize;

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            _frame.Pixels[spriteY, spriteX] =
                SelectedColour;
        }

        if (mouse.RightButton == ButtonState.Pressed)
        {
            _frame.Pixels[spriteY, spriteX] =
                CentauriSprite.TRANSPARENT;
        }
    }

    private bool TrySelectPaletteColour(int mouseX,int mouseY)
    {
        var paletteWidth =
            PaletteColumns * PaletteCellSize;

        var paletteRows =
            16 / PaletteColumns;

        var paletteHeight =
            paletteRows * PaletteCellSize;

        if (mouseX < PaletteX ||
            mouseX >= PaletteX + paletteWidth ||
            mouseY < PaletteY ||
            mouseY >= PaletteY + paletteHeight)
        {
            return false;
        }

        var column =
            (mouseX - PaletteX) / PaletteCellSize;

        var row =
            (mouseY - PaletteY) / PaletteCellSize;

        var colourIndex =
            row * PaletteColumns + column;

        if (colourIndex >= 16)
            return false;

        SelectedColour = colourIndex;

        return true;
    }

    public void Draw(SpriteBatch spriteBatch,Texture2D pixel)
    {
        if (!IsActive || _frame == null)
            return;

        // Editor background.
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, 640, 400),
            Color.Black);

        DrawGrid(spriteBatch,pixel);

        DrawPalette(spriteBatch,pixel);
    }

    private void DrawPalette(SpriteBatch spriteBatch,Texture2D pixel)
    {
        for (var colourIndex = 0;
            colourIndex < 16;
            colourIndex++)
        {
            var column =
                colourIndex % PaletteColumns;

            var row =
                colourIndex / PaletteColumns;

            var x =
                PaletteX + column * PaletteCellSize;

            var y =
                PaletteY + row * PaletteCellSize;

            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    x,
                    y,
                    PaletteCellSize - 2,
                    PaletteCellSize - 2),
                CentauriPalette.Get(colourIndex));

            // Highlight currently selected colour.
            if (colourIndex == SelectedColour)
            {
                DrawRectangle(
                    spriteBatch,
                    pixel,
                    new Rectangle(
                        x - 2,
                        y - 2,
                        PaletteCellSize + 2,
                        PaletteCellSize + 2),
                    Color.White);
            }
        }
    }

    private static void DrawRectangle(SpriteBatch spriteBatch,Texture2D pixel,Rectangle rectangle,Color colour)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                1),
            colour);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Bottom - 1,
                rectangle.Width,
                1),
            colour);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X,
                rectangle.Y,
                1,
                rectangle.Height),
            colour);

        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.Right - 1,
                rectangle.Y,
                1,
                rectangle.Height),
            colour);
    }

    private void DrawGrid(SpriteBatch spriteBatch,Texture2D pixel)
    {
        if (_frame == null)
            return;

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                var colourIndex =
                    _frame.Pixels[y, x];

                var colour =
                    colourIndex == CentauriSprite.TRANSPARENT
                        ? new Color(32, 32, 32)
                        : CentauriPalette.Get(colourIndex);

                var rectangle = new Rectangle(
                    GridX + x * PixelSize,
                    GridY + y * PixelSize,
                    PixelSize,
                    PixelSize);

                spriteBatch.Draw(
                    pixel,
                    rectangle,
                    colour);
            }
        }

        var gridWidth =
            CentauriSprite.WIDTH * PixelSize;

        var gridHeight =
            CentauriSprite.HEIGHT * PixelSize;

        for (var x = 0;
            x <= CentauriSprite.WIDTH;
            x++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    GridX + x * PixelSize,
                    GridY,
                    1,
                    gridHeight),
                Color.Gray);
        }

        for (var y = 0;
            y <= CentauriSprite.HEIGHT;
            y++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    GridX,
                    GridY + y * PixelSize,
                    gridWidth,
                    1),
                Color.Gray);
        }
    }

    public void Close()
    {
        IsActive = false;

        _asset = null;
        _animation = null;
        _frame = null;
    }
}