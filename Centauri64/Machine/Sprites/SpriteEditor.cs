using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;

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

    private int _currentAssetIndex;

    private bool _enteringSpriteName;
    private string _newSpriteName = "";

    public SpriteEditor(SpriteAssetStore assets)
    {
        _assets = assets;
    }

    public void Open(string assetName)
    {
        var assets =
            _assets.Assets;

        _currentAssetIndex = -1;

        for (var i = 0; i < assets.Count; i++)
        {
            if (assets[i].Name == assetName)
            {
                _currentAssetIndex = i;
                break;
            }
        }

        if (_currentAssetIndex < 0)
        {
            throw new InvalidOperationException(
                $"Unknown sprite {assetName}.");
        }

        SelectAsset(
            _currentAssetIndex);

        IsActive = true;
    }

    private void SelectAsset(int index)
    {
        var assets =
            _assets.Assets;

        if (index < 0 ||
            index >= assets.Count)
        {
            return;
        }

        _currentAssetIndex = index;

        _asset =
            assets[_currentAssetIndex];

        _animation =
            _asset.GetAnimation("DEFAULT");

        if (_animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "Sprite has no frames.");
        }

        _frame =
            _animation.Frames[0];
    }

    public void Update( MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        if (!IsActive || _frame == null)
        return;

        if (_enteringSpriteName)
        {
            UpdateSpriteNameEntry(
                keyboard,
                previousKeyboard);

            return;
        }

        if (keyboard.IsKeyDown(Keys.N) &&
            previousKeyboard.IsKeyUp(Keys.N))
        {
            _enteringSpriteName = true;
            _newSpriteName = "";
            return;
        }

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

        if (keyboard.IsKeyDown(Keys.Left) &&
            previousKeyboard.IsKeyUp(Keys.Left))
        {
            SelectPreviousAsset();
            return;
        }

        if (keyboard.IsKeyDown(Keys.Right) &&
            previousKeyboard.IsKeyUp(Keys.Right))
        {
            SelectNextAsset();
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

    private void SelectPreviousAsset()
    {
        var assets =
            _assets.Assets;

        if (assets.Count == 0)
            return;

        var index =
            _currentAssetIndex - 1;

        if (index < 0)
            index = assets.Count - 1;

        SelectAsset(index);
    }

    private void SelectNextAsset()
    {
        var assets =
            _assets.Assets;

        if (assets.Count == 0)
            return;

        var index =
            _currentAssetIndex + 1;

        if (index >= assets.Count)
            index = 0;

        SelectAsset(index);
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

    public void Draw(SpriteBatch spriteBatch,BitmapFont font,Texture2D pixel)
    {
        if (!IsActive || _frame == null)
            return;

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, 640, 400),
            Color.Black);

        DrawGrid(
            spriteBatch,
            pixel);

        DrawPalette(
            spriteBatch,
            pixel);

        DrawEditorText(
            spriteBatch,
            font);
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

    private void DrawEditorText(
        SpriteBatch spriteBatch,
        BitmapFont font)
    {
        if (_asset == null ||
            _animation == null)
        {
            return;
        }

        if (_enteringSpriteName)
        {
            font.Draw(
                spriteBatch,
                "NEW SPRITE:",
                new Vector2(440, 250),
                Color.White);

            font.Draw(
                spriteBatch,
                _newSpriteName + "_",
                new Vector2(440, 270),
                Color.White);

            font.Draw(
                spriteBatch,
                "ENTER - CREATE",
                new Vector2(440, 300),
                Color.White);

            font.Draw(
                spriteBatch,
                "ESC - CANCEL",
                new Vector2(440, 320),
                Color.White);

            return;
        }

        font.Draw(
            spriteBatch,
            "SPRITE EDITOR",
            new Vector2(440, 20),
            Color.White);

        font.Draw(
            spriteBatch,
            $"SPRITE: {_asset.Name}",
            new Vector2(440, 180),
            Color.White);

        font.Draw(
            spriteBatch,
            $"ANIM: {_animation.Name}",
            new Vector2(440, 200),
            Color.White);

        font.Draw(
            spriteBatch,
            "FRAME: 1/1",
            new Vector2(440, 220),
            Color.White);

        font.Draw(
            spriteBatch,
            "N - NEW SPRITE",
            new Vector2(440, 250),
            Color.White);

        font.Draw(
            spriteBatch,
            "< > - CHANGE",
            new Vector2(440, 270),
            Color.White);

        font.Draw(
            spriteBatch,
            "C - CLEAR",
            new Vector2(440, 290),
            Color.White);

        font.Draw(
            spriteBatch,
            "ESC - EXIT",
            new Vector2(440, 310),
            Color.White);
    }

    private void UpdateSpriteNameEntry(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (keyboard.IsKeyDown(Keys.Escape) &&
            previousKeyboard.IsKeyUp(Keys.Escape))
        {
            _enteringSpriteName = false;
            _newSpriteName = "";
            return;
        }

        if (keyboard.IsKeyDown(Keys.Back) &&
            previousKeyboard.IsKeyUp(Keys.Back))
        {
            if (_newSpriteName.Length > 0)
            {
                _newSpriteName =
                    _newSpriteName[..^1];
            }

            return;
        }

        if (keyboard.IsKeyDown(Keys.Enter) &&
            previousKeyboard.IsKeyUp(Keys.Enter))
        {
            CreateSprite();
            return;
        }

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!previousKeyboard.IsKeyUp(key))
                continue;

            if (key >= Keys.A &&
                key <= Keys.Z)
            {
                var character =
                    (char)('A' + (key - Keys.A));

                if (_newSpriteName.Length < 12)
                {
                    _newSpriteName += character;
                }

                return;
            }

            if (key >= Keys.D0 &&
                key <= Keys.D9)
            {
                var character =
                    (char)('0' + (key - Keys.D0));

                if (_newSpriteName.Length < 12)
                {
                    _newSpriteName += character;
                }

                return;
            }
        }
    }

    private void CreateSprite()
    {
        if (string.IsNullOrWhiteSpace(
                _newSpriteName))
        {
            return;
        }

        if (_assets.Contains(
                _newSpriteName))
        {
            _enteringSpriteName = false;
            _newSpriteName = "";
            return;
        }

        var asset =
            new SpriteAsset(
                _newSpriteName);

        var animation =
            asset.AddAnimation(
                "DEFAULT");

        animation.AddFrame();

        _assets.Add(asset);

        _enteringSpriteName = false;

        var name =
            _newSpriteName;

        _newSpriteName = "";

        // Assets are sorted, so find the newly
        // created asset's actual index.
        var assets =
            _assets.Assets;

        for (var i = 0;
            i < assets.Count;
            i++)
        {
            if (assets[i].Name == name)
            {
                SelectAsset(i);
                break;
            }
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