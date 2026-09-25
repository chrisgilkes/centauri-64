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

    private const int PreviewX = 150;
    private const int PreviewY = 400;
    private const int PreviewWidth = 190;
    private const int PreviewHeight = 64;

    private int _currentFrameIndex;
    private int _currentAnimationIndex;

    private int _previewFrameIndex;
    private float _previewTimer;

    private const float PreviewFrameTime = 0.125f;

    private bool _enteringAnimationName;
    private string _newAnimationName = "";

    private bool _copyingAnimation;

    private bool _onionSkinEnabled;

    public SpriteEditor(SpriteAssetStore assets)
    {
        _assets = assets;
    }

    public void Open()
    {
        var assets = _assets.Assets;

        if (assets.Count == 0)
        {
            return;
        }

        if (_currentAssetIndex < 0 ||
            _currentAssetIndex >= assets.Count)
        {
            _currentAssetIndex = 0;
        }

        SelectAsset(_currentAssetIndex);

        IsActive = true;
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

        var animations = _asset.AnimationList;

        if (animations.Count == 0)
        {
            throw new InvalidOperationException(
                "Sprite has no animations.");
        }

        _currentAnimationIndex = 0;

        SelectAnimation(_currentAnimationIndex);
    }

    private void SelectAnimation(int index)
    {
        if (_asset == null)
            return;

        var animations =
            _asset.AnimationList;

        if (index < 0 ||
            index >= animations.Count)
        {
            return;
        }

        _currentAnimationIndex = index;

        _animation =
            animations[_currentAnimationIndex];

        if (_animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "Animation has no frames.");
        }

        _currentFrameIndex = 0;

        _frame =
            _animation.Frames[_currentFrameIndex];

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

        if (_enteringAnimationName)
        {
            UpdateAnimationNameEntry(keyboard,previousKeyboard);

            return;
        }

        if (keyboard.IsKeyDown(Keys.N) && previousKeyboard.IsKeyUp(Keys.N))
        {
            _enteringSpriteName = true;
            _newSpriteName = "";
            return;
        }

        if (keyboard.IsKeyDown(Keys.O) &&
            previousKeyboard.IsKeyUp(Keys.O))
        {
            _onionSkinEnabled =
                !_onionSkinEnabled;

            return;
        }

        var shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (shiftDown && keyboard.IsKeyDown(Keys.M) && previousKeyboard.IsKeyUp(Keys.M))
        {
            _copyingAnimation = true;
            _enteringAnimationName = true;
            _newAnimationName = "";
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.M) && previousKeyboard.IsKeyUp(Keys.M))
        {
            _copyingAnimation = false;
            _enteringAnimationName = true;
            _newAnimationName = "";
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

        if (!shiftDown && keyboard.IsKeyDown(Keys.Left) &&
            previousKeyboard.IsKeyUp(Keys.Left))
        {
            SelectPreviousAsset();
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.Right) &&
            previousKeyboard.IsKeyUp(Keys.Right))
        {
            SelectNextAsset();
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.Up) && previousKeyboard.IsKeyUp(Keys.Up))
        {
            SelectPreviousAnimation();
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.Down) &&
            previousKeyboard.IsKeyUp(Keys.Down))
        {
            SelectNextAnimation();
            return;
        }

        if (shiftDown &&keyboard.IsKeyDown(Keys.Left) && previousKeyboard.IsKeyUp(Keys.Left))
        {
            ShiftFrame(-1, 0);
            return;
        }

        if (shiftDown &&
            keyboard.IsKeyDown(Keys.Right) &&
            previousKeyboard.IsKeyUp(Keys.Right))
        {
            ShiftFrame(1, 0);
            return;
        }

        if (shiftDown &&
            keyboard.IsKeyDown(Keys.Up) &&
            previousKeyboard.IsKeyUp(Keys.Up))
        {
            ShiftFrame(0, -1);
            return;
        }

        if (shiftDown &&
            keyboard.IsKeyDown(Keys.Down) &&
            previousKeyboard.IsKeyUp(Keys.Down))
        {
            ShiftFrame(0, 1);
            return;
        }

        if (keyboard.IsKeyDown(Keys.H) &&
            previousKeyboard.IsKeyUp(Keys.H))
        {
            FlipFrameHorizontal();
            return;
        }

        if (keyboard.IsKeyDown(Keys.V) &&
            previousKeyboard.IsKeyUp(Keys.V))
        {
            FlipFrameVertical();
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

        if (shiftDown && keyboard.IsKeyDown(Keys.D) && previousKeyboard.IsKeyUp(Keys.D))
        {
            DeleteCurrentAnimation();
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.D) && previousKeyboard.IsKeyUp(Keys.D))
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

    private void FlipFrameHorizontal()
    {
        if (_frame == null)
            return;

        var width =
            CentauriSprite.WIDTH;

        var height =
            CentauriSprite.HEIGHT;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width / 2; x++)
            {
                var oppositeX =
                    width - 1 - x;

                var temp =
                    _frame.Pixels[y, x];

                _frame.Pixels[y, x] =
                    _frame.Pixels[y, oppositeX];

                _frame.Pixels[y, oppositeX] =
                    temp;
            }
        }
    }

    private void FlipFrameVertical()
    {
        if (_frame == null)
            return;

        var width =
            CentauriSprite.WIDTH;

        var height =
            CentauriSprite.HEIGHT;

        for (var y = 0; y < height / 2; y++)
        {
            var oppositeY =
                height - 1 - y;

            for (var x = 0; x < width; x++)
            {
                var temp =
                    _frame.Pixels[y, x];

                _frame.Pixels[y, x] =
                    _frame.Pixels[oppositeY, x];

                _frame.Pixels[oppositeY, x] =
                    temp;
            }
        }
    }

    private void ShiftFrame(int offsetX, int offsetY)
    {
        if (_frame == null)
            return;

        var width =
            CentauriSprite.WIDTH;

        var height =
            CentauriSprite.HEIGHT;

        var shiftedPixels =
            new int[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var newX =
                    (x + offsetX + width) % width;

                var newY =
                    (y + offsetY + height) % height;

                shiftedPixels[newY, newX] =
                    _frame.Pixels[y, x];
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                _frame.Pixels[y, x] =
                    shiftedPixels[y, x];
            }
        }
    }

    private void DeleteCurrentAnimation()
    {
        if (_asset == null ||
            _animation == null)
        {
            return;
        }

        var animationName =
            _animation.Name;

        if (!_asset.RemoveAnimation(animationName))
        {
            return;
        }

        var animations =
            _asset.AnimationList;

        // If we deleted the last animation in the list,
        // move back to the new final animation.
        if (_currentAnimationIndex >= animations.Count)
        {
            _currentAnimationIndex =
                animations.Count - 1;
        }

        SelectAnimation(
            _currentAnimationIndex);
    }

    private void UpdateAnimationNameEntry(KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        if (keyboard.IsKeyDown(Keys.Escape) &&
            previousKeyboard.IsKeyUp(Keys.Escape))
        {
            _enteringAnimationName = false;
            _copyingAnimation = false;
            _newAnimationName = "";
            return;
        }

        if (keyboard.IsKeyDown(Keys.Back) &&
            previousKeyboard.IsKeyUp(Keys.Back))
        {
            if (_newAnimationName.Length > 0)
            {
                _newAnimationName =
                    _newAnimationName[..^1];
            }

            return;
        }

        if (keyboard.IsKeyDown(Keys.Enter) &&
            previousKeyboard.IsKeyUp(Keys.Enter))
        {
            CreateAnimation();
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

                if (_newAnimationName.Length < 12)
                {
                    _newAnimationName += character;
                }

                return;
            }

            if (key >= Keys.D0 &&
                key <= Keys.D9)
            {
                var character =
                    (char)('0' + (key - Keys.D0));

                if (_newAnimationName.Length < 12)
                {
                    _newAnimationName += character;
                }

                return;
            }
        }
    }

    private void CreateAnimation()
    {
        if (_asset == null)
            return;

        if (string.IsNullOrWhiteSpace(
                _newAnimationName))
        {
            return;
        }

        if (_asset.ContainsAnimation(_newAnimationName))
        {
            _enteringAnimationName = false;
            _copyingAnimation = false;
            _newAnimationName = "";
            return;
        }

        SpriteAnimation animation;

        if (_copyingAnimation &&
            _animation != null)
        {
            var duplicate =
                _asset.DuplicateAnimation(
                    _animation.Name,
                    _newAnimationName);

            if (duplicate == null)
                return;

            animation = duplicate;
        }
        else
        {
            animation =
                _asset.AddAnimation(
                    _newAnimationName);

            animation.AddFrame();
        }

        var name =
            _newAnimationName;

        _enteringAnimationName = false;
        _newAnimationName = "";

        var animations =
            _asset.AnimationList;

        for (var i = 0;
            i < animations.Count;
            i++)
        {
            if (animations[i].Name == name)
            {
                SelectAnimation(i);
                break;
            }
        }

        _copyingAnimation = false;
    }

    private void SelectPreviousAnimation()
    {
        if (_asset == null)
            return;

        var animations =
            _asset.AnimationList;

        if (animations.Count == 0)
            return;

        var index =
            _currentAnimationIndex - 1;

        if (index < 0)
        {
            index = animations.Count - 1;
        }

        SelectAnimation(index);
    }

    private void SelectNextAnimation()
    {
        if (_asset == null)
            return;

        var animations =
            _asset.AnimationList;

        if (animations.Count == 0)
            return;

        var index =
            _currentAnimationIndex + 1;

        if (index >= animations.Count)
        {
            index = 0;
        }

        SelectAnimation(index);
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
        if (_animation == null)
            return;

        if (!_animation.RemoveFrame(_currentFrameIndex))
            return;

        if (_currentFrameIndex >= _animation.Frames.Count)
        {
            _currentFrameIndex =
                _animation.Frames.Count - 1;
        }

        _frame =
            _animation.Frames[_currentFrameIndex];

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


        SpriteFrame? onionFrame = null;

        if (_onionSkinEnabled &&
            _animation != null &&
            _animation.Frames.Count > 1)
        {
            var onionFrameIndex =
                _currentFrameIndex - 1;

            if (onionFrameIndex < 0)
            {
                onionFrameIndex =
                    _animation.Frames.Count - 1;
            }

            onionFrame =
                _animation.Frames[onionFrameIndex];
        }


        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                var colourIndex = _frame.Pixels[y, x];

                Color colour;

                if (colourIndex != CentauriSprite.TRANSPARENT)
                {
                    colour =
                        CentauriPalette.Get(colourIndex);
                }
                else if (onionFrame != null)
                {
                    var onionColourIndex =
                        onionFrame.Pixels[y, x];

                    if (onionColourIndex !=
                        CentauriSprite.TRANSPARENT)
                    {
                        colour =
                            CentauriPalette
                                .Get(onionColourIndex) * 0.3f;
                    }
                    else
                    {
                        colour =
                            new Color(32, 32, 32);
                    }
                }
                else
                {
                    colour = new Color(32, 32, 32);
                }

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

        const int x = 440;

        // Title.
        font.Draw(
            spriteBatch,
            "SPRITE EDITOR",
            new Vector2(x, 20),
            Color.White);

        // Palette heading.
        font.Draw(
            spriteBatch,
            "PALETTE",
            new Vector2(x, 42),
            Color.White);

        // New sprite name entry.
        if (_enteringSpriteName)
        {
            font.Draw(
                spriteBatch,
                "NEW SPRITE:",
                new Vector2(x, 185),
                Color.White);

            font.Draw(
                spriteBatch,
                _newSpriteName + "_",
                new Vector2(x, 205),
                Color.White);

            font.Draw(
                spriteBatch,
                "ENTER - CREATE",
                new Vector2(x, 225),
                Color.White);

            font.Draw(
                spriteBatch,
                "ESC - CANCEL",
                new Vector2(x, 245),
                Color.White);

            return;
        }

        // New/copy animation name entry.
        if (_enteringAnimationName)
        {
            font.Draw(
                spriteBatch,
                _copyingAnimation
                    ? "COPY ANIMATION:"
                    : "NEW ANIMATION:",
                new Vector2(x, 185),
                Color.White);

            font.Draw(
                spriteBatch,
                _newAnimationName + "_",
                new Vector2(x, 205),
                Color.White);

            font.Draw(
                spriteBatch,
                "ENTER - CREATE",
                new Vector2(x, 225),
                Color.White);

            font.Draw(
                spriteBatch,
                "ESC - CANCEL",
                new Vector2(x, 245),
                Color.White);

            return;
        }

        // Current sprite information.
        font.Draw(
            spriteBatch,
            $"SPRITE: {_asset.Name}",
            new Vector2(x, 180),
            Color.White);

        font.Draw(
            spriteBatch,
            $"ANIM: {_animation.Name}",
            new Vector2(x, 195),
            Color.White);

        font.Draw(
            spriteBatch,
            $"FRAME: {_currentFrameIndex + 1}/{_animation.Frames.Count}",
            new Vector2(x, 210),
            Color.White);

        font.Draw(
            spriteBatch,
            $"ONION: {(_onionSkinEnabled ? "ON" : "OFF")}",
            new Vector2(x, 225),
            _onionSkinEnabled
                ? Color.Yellow
                : Color.Gray);

        // Sprite controls.
        font.Draw(
            spriteBatch,
            "N - NEW SPRITE",
            new Vector2(x, 245),
            Color.White);

        font.Draw(
            spriteBatch,
            "< > - SPRITE",
            new Vector2(x, 260),
            Color.White);

        // Animation controls.
        font.Draw(
            spriteBatch,
            "M - NEW ANIM",
            new Vector2(x, 280),
            Color.White);

        font.Draw(
            spriteBatch,
            "SHIFT+M - COPY ANIM",
            new Vector2(x, 295),
            Color.White);

        font.Draw(
            spriteBatch,
            "UP/DOWN - ANIM",
            new Vector2(x, 310),
            Color.White);

        font.Draw(
            spriteBatch,
            "SHIFT+D - DELETE ANIM",
            new Vector2(x, 325),
            Color.White);

        // Frame controls.
        font.Draw(
            spriteBatch,
            "[ ] - FRAME",
            new Vector2(x, 345),
            Color.White);

        font.Draw(
            spriteBatch,
            "A - DUP FRAME",
            new Vector2(x, 360),
            Color.White);

        font.Draw(
            spriteBatch,
            "D - DELETE FRAME",
            new Vector2(x, 375),
            Color.White);

        // Editing controls.
        font.Draw(
            spriteBatch,
            "O - ONION",
            new Vector2(x, 395),
            Color.White);

        font.Draw(
            spriteBatch,
            "SHIFT+ARROWS - MOVE",
            new Vector2(x, 410),
            Color.White);

        font.Draw(
            spriteBatch,
            "H/V - FLIP",
            new Vector2(x, 425),
            Color.White);

        font.Draw(
            spriteBatch,
            "C - CLEAR",
            new Vector2(x, 440),
            Color.White);

        font.Draw(
            spriteBatch,
            "ESC - EXIT",
            new Vector2(x, 455),
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