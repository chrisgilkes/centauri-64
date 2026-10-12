using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.CreativeTools;
using Centauri64.Graphics;
using Centauri64.Machine.Images;

namespace Centauri64.Machine.Sprites;

public sealed partial class SpriteEditor
{
    private readonly SpriteAssetStore _assets;
    private ImageAssetStore? _images;

    private SpriteAsset? _asset;
    private SpriteAnimation? _animation;
    private SpriteFrame? _frame;

    private bool _selectingArtwork;
    private int _artworkIndex;
    private readonly List<ImageAsset> _eligibleArtwork = new();

    public bool IsActive { get; private set; }

    /// <summary>Opens restricted Sprite Artwork Image Editor (Issue #3).</summary>
    public Action? RequestSpriteArtworkEditor { get; set; }

    public Func<string?>? GetTapeName { get; set; }

    public Action<string>? PersistToTape { get; set; }

    public int SelectedColour { get; private set; } = 7;

    private const int ScreenW = 640;
    private const int ScreenH = 480;
    private const int BrowserWidth = 120;
    private const int PaletteWidth = 88;
    private const int PreviewStripHeight = 72;
    private const int PaletteCellSize = 16;
    private const int PaletteColumns = 4;

    // Layout — recomputed each draw/update from chrome.
    private int GridX = 200;
    private int GridY = 56;
    private int PixelSize = 20;
    private int PaletteX = 552;
    private int PaletteY = 56;
    private int PreviewX = 200;
    private int PreviewY = 400;
    private int PreviewWidth = 190;
    private int PreviewHeight = 64;

    private readonly MenuBar _menuBar = new();
    private readonly ToolBar _toolBar;
    private readonly StatusBar _statusBar;
    private MouseState _previousMouse;
    private string _statusTip = "";

    private int _currentAssetIndex;

    private bool _enteringSpriteName;
    private string _newSpriteName = "";

    private int _currentFrameIndex;
    private int _currentAnimationIndex;

    private int _previewFrameIndex;
    private float _previewTimer;


    private bool _enteringAnimationName;
    private string _newAnimationName = "";

    private bool _copyingAnimation;

    private bool _onionSkinEnabled;

    public SpriteEditor(SpriteAssetStore assets, ImageAssetStore? images = null)
    {
        _assets = assets;
        _images = images;
        _toolBar = new ToolBar(16, 24, 48);
        _statusBar = new StatusBar(ScreenH - 16, 16);
        BuildMenus();
        BuildToolbar();
        LayoutChrome();
    }

    public void SetImageStore(ImageAssetStore images)
    {
        _images = images;
    }

    public void Open()
    {
        var assets = _assets.Assets;

        IsActive = true;
        _showingHelp = false;
        _selectingArtwork = false;
        _confirm = ConfirmKind.None;
        _message = "";
        ClearUndo();

        if (assets.Count == 0)
        {
            _currentAssetIndex = 0;
            _asset = null;
            _animation = null;
            _frame = null;
            return;
        }

        if (_currentAssetIndex < 0 ||
            _currentAssetIndex >= assets.Count)
        {
            _currentAssetIndex = 0;
        }

        SelectAsset(_currentAssetIndex);
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

        ClearUndo();

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

        ClearUndo();

        _previewFrameIndex = 0;
        _previewTimer = 0.0f;
    }

    public void Update(GameTime gameTime,MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        if (!IsActive)
            return;

        LayoutChrome();
        UpdatePreviewAnimation(gameTime);

        if (_enteringSpriteName)
        {
            UpdateSpriteNameEntry(
                keyboard,
                previousKeyboard);
            _previousMouse = mouse;
            return;
        }

        if (_enteringAnimationName)
        {
            UpdateAnimationNameEntry(keyboard,previousKeyboard);
            _previousMouse = mouse;
            return;
        }

        if (_confirm != ConfirmKind.None)
        {
            UpdateConfirm(keyboard, previousKeyboard);
            _previousMouse = mouse;
            return;
        }

        if (_selectingArtwork)
        {
            UpdateArtworkSelection(keyboard, previousKeyboard);
            _previousMouse = mouse;
            return;
        }

        if (_menuBar.IsOpen)
        {
            _menuBar.HandleKeyboard(keyboard, previousKeyboard);
            _menuBar.HandleMouse(mouse, _previousMouse, ScreenW);
            _previousMouse = mouse;
            return;
        }

        if (_menuBar.HandleMouse(mouse, _previousMouse, ScreenW))
        {
            _previousMouse = mouse;
            return;
        }

        if (_toolBar.HandleMouse(mouse, _previousMouse))
        {
            if (!string.IsNullOrEmpty(_toolBar.HoverTip))
                _statusTip = _toolBar.HoverTip;
            _previousMouse = mouse;
            return;
        }

        if (HandleBrowserMouse(mouse))
        {
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.OemQuestion))
        {
            _showingHelp = !_showingHelp;
            return;
        }

        if (_showingHelp)
        {
            if (Pressed(keyboard, previousKeyboard, Keys.Escape))
                _showingHelp = false;

            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            Close();
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.N) &&
            !ShiftDown(keyboard))
        {
            BeginNewSpritePrompt();
            _previousMouse = mouse;
            return;
        }

        var shiftDown = ShiftDown(keyboard);

        if (shiftDown &&
            Pressed(keyboard, previousKeyboard, Keys.N))
        {
            _enteringSpriteName = true;
            _copyingSprite = true;
            _renamingSprite = false;
            _newSpriteName = "";
            _message = "";
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.R))
        {
            BeginRenameSpritePrompt();
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.X))
        {
            _confirm = ConfirmKind.DeleteSprite;
            return;
        }

        if (shiftDown && keyboard.IsKeyDown(Keys.M) && previousKeyboard.IsKeyUp(Keys.M))
        {
            BeginCopyAnimationPrompt();
            return;
        }

        if (!shiftDown && keyboard.IsKeyDown(Keys.M) && previousKeyboard.IsKeyUp(Keys.M))
        {
            BeginNewAnimationPrompt();
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.B))
        {
            BeginArtworkSelection();
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.I))
        {
            OpenArtStudio();
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

        if (shiftDown && keyboard.IsKeyDown(Keys.D) && previousKeyboard.IsKeyUp(Keys.D))
        {
            if (_animation != null &&
                _animation.Name == "DEFAULT")
            {
                ShowMessage("CANNOT DELETE DEFAULT");
                return;
            }

            _confirm = ConfirmKind.DeleteAnimation;
            return;
        }

        // Canvas is preview-only — paint in Art Studio, then BIND.
        if (mouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released)
        {
            var gridWidth = CentauriSprite.WIDTH * PixelSize;
            var gridHeight = CentauriSprite.HEIGHT * PixelSize;
            if (mouse.X >= GridX &&
                mouse.X < GridX + gridWidth &&
                mouse.Y >= GridY &&
                mouse.Y < GridY + gridHeight)
            {
                BeginArtworkSelection();
            }
        }

        _previousMouse = mouse;
    }

    private static bool ShiftDown(KeyboardState keyboard)
    {
        return keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift);
    }

    private static bool Pressed(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key)
    {
        return keyboard.IsKeyDown(key) &&
            previousKeyboard.IsKeyUp(key);
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
                shiftedPixels[y, x] =
                    CentauriSprite.TRANSPARENT;
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var newX = x + offsetX;
                var newY = y + offsetY;

                if (newX < 0 || newX >= width ||
                    newY < 0 || newY >= height)
                {
                    continue;
                }

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

        if (_animation.Name == "DEFAULT")
            return;

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
            _message = "";
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
            ShowMessage("NAME TAKEN");
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
        MarkDirty();
        ClearUndo();
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

        var current = _animation.Frames[
            Math.Clamp(_previewFrameIndex, 0, _animation.Frames.Count - 1)];
        var frameTime = current.DurationSeconds > 0f
            ? current.DurationSeconds
            : ImageFrame.DefaultDurationSeconds;

        _previewTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (_previewTimer >= frameTime)
        {
            _previewTimer -= frameTime;

            _previewFrameIndex++;

            if (_previewFrameIndex >=
                _animation.Frames.Count)
            {
                _previewFrameIndex = 0;
            }

            current = _animation.Frames[_previewFrameIndex];
            frameTime = current.DurationSeconds > 0f
                ? current.DurationSeconds
                : ImageFrame.DefaultDurationSeconds;
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
            _animation.InsertFrameAfter(_currentFrameIndex);

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

        _currentFrameIndex++;

        _frame =
            _animation.Frames[_currentFrameIndex];

        MarkDirty();
        ClearUndo();
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

        ClearUndo();
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

        ClearUndo();
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

    private bool TrySelectPaletteColour(int mouseX, int mouseY)
    {
        // Transparent swatch at top of palette column.
        var trans = new Rectangle(PaletteX, PaletteY, PaletteWidth - 16, 16);
        if (trans.Contains(mouseX, mouseY))
        {
            SelectedColour = CentauriSprite.TRANSPARENT;
            _statusTip = "COLOUR: TRANSPARENT (RIGHT-CLICK ALSO ERASES)";
            return true;
        }

        var swatchOriginY = PaletteY + 24;
        var paletteWidth = PaletteColumns * PaletteCellSize;
        var paletteRows = (CentauriPalette.MAX_COLORS + PaletteColumns - 1) / PaletteColumns;
        var paletteHeight = paletteRows * PaletteCellSize;

        if (mouseX < PaletteX ||
            mouseX >= PaletteX + paletteWidth ||
            mouseY < swatchOriginY ||
            mouseY >= swatchOriginY + paletteHeight)
        {
            return false;
        }

        var column = (mouseX - PaletteX) / PaletteCellSize;
        var row = (mouseY - swatchOriginY) / PaletteCellSize;
        var colourIndex = row * PaletteColumns + column;

        if (colourIndex >= CentauriPalette.MAX_COLORS)
            return false;

        SelectedColour = colourIndex;
        _statusTip = $"COLOUR: {colourIndex}";
        return true;
    }

    private void BeginArtworkSelection()
    {
        if (_animation == null)
        {
            ShowMessage("CREATE A SPRITE FIRST");
            return;
        }

        _eligibleArtwork.Clear();
        if (_images != null)
        {
            foreach (var image in _images.Images)
            {
                if (SpriteArtwork.IsEligible(image))
                    _eligibleArtwork.Add(image);
            }
        }

        if (_eligibleArtwork.Count == 0)
        {
            ShowMessage("NO 16X16 ART — PRESS I FOR ART STUDIO");
            return;
        }

        _artworkIndex = 0;
        if (!string.IsNullOrEmpty(_animation.SourceImageName))
        {
            for (var i = 0; i < _eligibleArtwork.Count; i++)
            {
                if (_eligibleArtwork[i].Name == _animation.SourceImageName)
                {
                    _artworkIndex = i;
                    break;
                }
            }
        }

        _selectingArtwork = true;
        _message = "";
    }

    private void UpdateArtworkSelection(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            _selectingArtwork = false;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Up) ||
            Pressed(keyboard, previousKeyboard, Keys.Left))
        {
            _artworkIndex--;
            if (_artworkIndex < 0)
                _artworkIndex = _eligibleArtwork.Count - 1;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Down) ||
            Pressed(keyboard, previousKeyboard, Keys.Right))
        {
            _artworkIndex++;
            if (_artworkIndex >= _eligibleArtwork.Count)
                _artworkIndex = 0;
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Enter))
        {
            ApplySelectedArtwork();
            _selectingArtwork = false;
        }
    }

    private void ApplySelectedArtwork()
    {
        if (_animation == null ||
            _artworkIndex < 0 ||
            _artworkIndex >= _eligibleArtwork.Count)
        {
            return;
        }

        var image = _eligibleArtwork[_artworkIndex];
        var wasBound = !string.IsNullOrWhiteSpace(_animation.SourceImageName);

        try
        {
            SpriteArtwork.BakeIntoAnimation(_animation, image);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message.ToUpperInvariant());
            return;
        }

        _currentFrameIndex = 0;
        _frame = _animation.Frames[0];
        _previewFrameIndex = 0;
        _previewTimer = 0f;
        ClearUndo();
        MarkDirty();

        // Explicit bake — later library edits do not change this copy until rebound.
        ShowMessage(
            wasBound
                ? $"REBOUND {image.Name} ({_animation.Frames.Count} FR) — BAKED COPY UPDATED"
                : $"BOUND {image.Name} ({_animation.Frames.Count} FR) — BAKED COPY");
    }

    private void DrawArtworkSelection(
        SpriteBatch spriteBatch,
        BitmapFont font,
        int x)
    {
        font.Draw(
            spriteBatch,
            "BIND 16X16 ARTWORK",
            new Vector2(x, 180),
            Color.Yellow);

        font.Draw(
            spriteBatch,
            $"TO ANIM: {_animation?.Name ?? "?"}",
            new Vector2(x, 195),
            Color.White);

        var start = Math.Max(0, _artworkIndex - 4);
        var y = 220;
        for (var i = start;
             i < _eligibleArtwork.Count && y < 360;
             i++)
        {
            var image = _eligibleArtwork[i];
            var selected = i == _artworkIndex;
            var label =
                $"{image.Name}  {image.FrameCount}FR";

            font.Draw(
                spriteBatch,
                selected ? "> " + label : "  " + label,
                new Vector2(x, y),
                selected ? Color.Yellow : Color.White);
            y += 15;
        }

        var already =
            !string.IsNullOrWhiteSpace(_animation?.SourceImageName);
        font.Draw(
            spriteBatch,
            already
                ? "UP/DOWN  ENTER REBIND (BAKED)  ESC"
                : "UP/DOWN  ENTER BIND (BAKED COPY)  ESC",
            new Vector2(x, 380),
            Color.White);
    }

    private void DrawPrompt(
        SpriteBatch spriteBatch,
        BitmapFont font,
        int x,
        string title,
        string value)
    {
        font.Draw(
            spriteBatch,
            title,
            new Vector2(x, 185),
            Color.White);

        font.Draw(
            spriteBatch,
            value + "_",
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

        if (!string.IsNullOrEmpty(_message))
        {
            font.Draw(
                spriteBatch,
                _message,
                new Vector2(x, 265),
                Color.Yellow);
        }
    }

    private string ConfirmText()
    {
        return _confirm switch
        {
            ConfirmKind.DeleteFrame => "DELETE FRAME?",
            ConfirmKind.DeleteAnimation => "DELETE ANIM?",
            ConfirmKind.DeleteSprite => "DELETE SPRITE?",
            _ => ""
        };
    }

    private static void DrawHelp(
        SpriteBatch spriteBatch,
        BitmapFont font,
        int x)
    {
        var lines = new[]
        {
            "SPRITE BINDER — NO PAINTING HERE",
            "I  OPEN ART STUDIO",
            "B  BIND 16X16 ART INTO ANIM",
            "N  NEW SPRITE",
            "M  NEW ANIMATION",
            "< > SPRITES   UP/DOWN ANIMS",
            "[ ] REVIEW BAKED FRAMES",
            "R RENAME  X DELETE SPRITE",
            "SHIFT+D DELETE ANIM",
            "CLICK GRID TO BIND",
            "? OR ESC CLOSE"
        };

        var y = 180;

        foreach (var line in lines)
        {
            font.Draw(
                spriteBatch,
                line,
                new Vector2(x, y),
                Color.White);

            y += 14;
        }
    }

    private void UpdateSpriteNameEntry(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (keyboard.IsKeyDown(Keys.Escape) &&
            previousKeyboard.IsKeyUp(Keys.Escape))
        {
            _enteringSpriteName = false;
            _copyingSprite = false;
            _renamingSprite = false;
            _newSpriteName = "";
            _message = "";
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

        var name = _newSpriteName;

        if (_renamingSprite)
        {
            if (_asset != null && _asset.Name == name)
            {
                _enteringSpriteName = false;
                _renamingSprite = false;
                _newSpriteName = "";
                _message = "";
                return;
            }
            if (_assets.Contains(name) &&
                _asset != null &&
                _asset.Name != name)
            {
                ShowMessage("NAME TAKEN");
                return;
            }

            RenameCurrentSprite(name);
            _enteringSpriteName = false;
            _renamingSprite = false;
            _newSpriteName = "";
            _message = "";
            return;
        }

        if (_assets.Contains(name))
        {
            ShowMessage("NAME TAKEN");
            return;
        }

        if (_copyingSprite)
        {
            CopyCurrentSprite(name);
        }
        else
        {
            var asset =
                new SpriteAsset(name);

            var animation =
                asset.AddAnimation("DEFAULT");

            animation.AddFrame();

            _assets.Add(asset);
            MarkDirty();
            SelectAssetByName(name);
        }

        _enteringSpriteName = false;
        _copyingSprite = false;
        _renamingSprite = false;
        _newSpriteName = "";
        _message = "";
    }

    public void Close()
    {
        _selectingArtwork = false;

        if (_dirty)
        {
            var tape = GetTapeName?.Invoke();
            if (!string.IsNullOrWhiteSpace(tape) && PersistToTape != null)
            {
                PersistToTape(tape);
                _dirty = false;
                Notice?.Invoke($"SAVED SPRITES TO \"{tape.ToUpperInvariant()}\"");
            }
            else
            {
                Notice?.Invoke("SAVE \"NAME\" TO KEEP SPRITES");
            }
        }

        IsActive = false;
        _showingHelp = false;
        _confirm = ConfirmKind.None;
        _painting = false;

        _asset = null;
        _animation = null;
        _frame = null;
    }
}