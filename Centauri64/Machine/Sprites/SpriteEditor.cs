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

    private const int PaletteCellSize = 18;
    private const int PaletteColumns = 8;

    private int _currentAssetIndex;

    private bool _enteringSpriteName;
    private string _newSpriteName = "";

    private const int PreviewX = 425;
    private const int PreviewY = 400;
    private const int PreviewWidth = 190;
    private const int PreviewHeight = 64;

    private int _currentFrameIndex;

    private int _previewFrameIndex;
    private float _previewTimer;

    private const float PreviewFrameTime = 0.125f;

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

        _currentFrameIndex = 0;

        _frame = _animation.Frames[_currentFrameIndex];

        _previewFrameIndex = 0;
        _previewTimer = 0.0f;
    }

    public void Update(GameTime gameTime,MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        if (!IsActive || _frame == null)
            return;

        UpdatePreviewAnimation(gameTime);

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

        if (keyboard.IsKeyDown(Keys.OemOpenBrackets) && previousKeyboard.IsKeyUp(Keys.OemOpenBrackets))
        {
            SelectPreviousFrame();
            return;
        }

        if (keyboard.IsKeyDown(Keys.OemCloseBrackets) &&
            previousKeyboard.IsKeyUp(Keys.OemCloseBrackets))
        {
            SelectNextFrame();
            return;
        }

        if (keyboard.IsKeyDown(Keys.A) && previousKeyboard.IsKeyUp(Keys.A))
        {
            AddFrame();
            return;
        }

        if (keyboard.IsKeyDown(Keys.D) && previousKeyboard.IsKeyUp(Keys.D))
        {
            DeleteCurrentFrame();
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

    private void UpdatePreviewAnimation(GameTime gameTime)
    {
        if (_animation == null ||
            _animation.Frames.Count <= 1)
        {
            _previewFrameIndex = 0;
            _previewTimer = 0.0f;
            return;
        }

        _previewTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_previewTimer >= PreviewFrameTime)
        {
            _previewTimer -= PreviewFrameTime;

            _previewFrameIndex++;

            if (_previewFrameIndex >=
                _animation.Frames.Count)
            {
                _previewFrameIndex = 0;
            }
        }
    }

    private void AddFrame()
    {
        if (_animation == null ||
            _frame == null)
        {
            return;
        }

        var sourceFrame = _frame;

        var newFrame =
            _animation.AddFrame();

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                newFrame.Pixels[y, x] =
                    sourceFrame.Pixels[y, x];
            }
        }

        _currentFrameIndex =
            _animation.Frames.Count - 1;

        _frame =
            _animation.Frames[_currentFrameIndex];
    }
    
    private void DeleteCurrentFrame()
    {
        if (_animation == null ||
            _animation.Frames.Count <= 1)
        {
            return;
        }

        _animation.Frames.RemoveAt(_currentFrameIndex);

        // If we deleted the last frame, move back to the new last frame.
        if (_currentFrameIndex >= _animation.Frames.Count)
        {
            _currentFrameIndex =
                _animation.Frames.Count - 1;
        }

        _frame =
            _animation.Frames[_currentFrameIndex];

        // Keep the animated preview valid too.
        if (_previewFrameIndex >= _animation.Frames.Count)
        {
            _previewFrameIndex = 0;
        }

        _previewTimer = 0.0f;
    }

    private void SelectPreviousFrame()
    {
        if (_animation == null ||
            _animation.Frames.Count == 0)
        {
            return;
        }

        _currentFrameIndex--;

        if (_currentFrameIndex < 0)
        {
            _currentFrameIndex =
                _animation.Frames.Count - 1;
        }

        _frame =
            _animation.Frames[_currentFrameIndex];
    }

    private void SelectNextFrame()
    {
        if (_animation == null ||
            _animation.Frames.Count == 0)
        {
            return;
        }

        _currentFrameIndex++;

        if (_currentFrameIndex >=
            _animation.Frames.Count)
        {
            _currentFrameIndex = 0;
        }

        _frame =
            _animation.Frames[_currentFrameIndex];
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
        var paletteWidth = PaletteColumns * PaletteCellSize;

        var paletteRows  = CentauriPalette.MAX_COLORS / PaletteColumns;

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

        if (colourIndex >= CentauriPalette.MAX_COLORS)
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

        DrawGrid(spriteBatch,pixel);

        DrawPreview(spriteBatch,pixel);

        DrawPalette(spriteBatch,pixel);

        DrawEditorText(spriteBatch,font);

    }

    private void DrawPalette(SpriteBatch spriteBatch,Texture2D pixel)
    {
        for (var colourIndex = 0;colourIndex < CentauriPalette.MAX_COLORS;colourIndex++)
        {
            var column =
                colourIndex % PaletteColumns;

            var row =
                colourIndex / PaletteColumns;

            var x =
                PaletteX + column * PaletteCellSize;

            var y =
                PaletteY + row * PaletteCellSize;

            var rectangle = new Rectangle(
                x,
                y,
                PaletteCellSize,
                PaletteCellSize);

            // Draw colour with a 1px inset so the
            // palette grid remains visible.
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    rectangle.X + 1,
                    rectangle.Y + 1,
                    rectangle.Width - 2,
                    rectangle.Height - 2),
                CentauriPalette.Get(colourIndex));

            // Grid around every colour.
            DrawRectangle(
                spriteBatch,
                pixel,
                rectangle,
                Color.Gray);

            // Stronger highlight around selected colour.
            if (colourIndex == SelectedColour)
            {
                DrawRectangle(
                    spriteBatch,
                    pixel,
                    new Rectangle(
                        x - 2,
                        y - 2,
                        PaletteCellSize + 4,
                        PaletteCellSize + 4),
                    Color.White);
            }
        }
    }

    private void DrawPreview(SpriteBatch spriteBatch,Texture2D pixel)
    {
        if (_animation == null || _animation.Frames.Count == 0)
        {
            return;
        }

        var previewFrame =_animation.Frames[_previewFrameIndex];

        var previewRect = new Rectangle(
            PreviewX,
            PreviewY,
            PreviewWidth,
            PreviewHeight);

        DrawRectangle(
            spriteBatch,
            pixel,
            previewRect,
            Color.Gray);

        var spriteX =
            PreviewX +
            (PreviewWidth - CentauriSprite.WIDTH) / 2;

        var spriteY =
            PreviewY +
            (PreviewHeight - CentauriSprite.HEIGHT) / 2;

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                var colourIndex = previewFrame.Pixels[y, x];

                if (colourIndex ==
                    CentauriSprite.TRANSPARENT)
                {
                    continue;
                }

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        spriteX + x,
                        spriteY + y,
                        1,
                        1),
                    CentauriPalette.Get(
                        colourIndex));
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

    private void DrawEditorText(SpriteBatch spriteBatch,BitmapFont font)
    {
        if (_asset == null || _animation == null)
        {
            return;
        }

        // Title.
        font.Draw(spriteBatch,"SPRITE EDITOR",new Vector2(440, 20),Color.White);

        // Palette heading.
        font.Draw(spriteBatch,"PALETTE",new Vector2(440, 42),Color.White);

        // New sprite name entry.
        if (_enteringSpriteName)
        {
            font.Draw(
                spriteBatch,
                "NEW SPRITE:",
                new Vector2(440, 200),
                Color.White);

            font.Draw(
                spriteBatch,
                _newSpriteName + "_",
                new Vector2(440, 220),
                Color.White);

            font.Draw(
                spriteBatch,
                "ENTER - CREATE",
                new Vector2(440, 240),
                Color.White);

            font.Draw(
                spriteBatch,
                "ESC - CANCEL",
                new Vector2(440, 260),
                Color.White);

            return;
        }

        // Current sprite information.
        font.Draw(
            spriteBatch,
            $"SPRITE: {_asset.Name}",
            new Vector2(440, 200),
            Color.White);

        font.Draw(
            spriteBatch,
            $"ANIM: {_animation.Name}",
            new Vector2(440, 220),
            Color.White);

        font.Draw(
            spriteBatch,
            $"FRAME: {_currentFrameIndex + 1}/{_animation.Frames.Count}",
            new Vector2(440, 240),
            Color.White);

        // Controls.
        font.Draw(
            spriteBatch,
            "N - NEW SPRITE",
            new Vector2(440, 260),
            Color.White);

        font.Draw(
            spriteBatch,
            "< > - CHANGE",
            new Vector2(440, 280),
            Color.White);

         font.Draw(
            spriteBatch,
            "A - ADD FRAME",
            new Vector2(440, 300),
            Color.White);

        font.Draw(
            spriteBatch,
            "D - DELETE FRAME",
            new Vector2(440, 320),
            Color.White);

        font.Draw(
            spriteBatch,
            "C - CLEAR",
            new Vector2(440, 340),
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