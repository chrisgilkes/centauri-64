using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Machine.Images;
using Centauri64.Session;
using Centauri64.Settings;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    private readonly ImageAssetStore _imageAssets = new();
    private readonly ImageLayers _imageLayers = new();
    private readonly ImageTextureCache _imageTextureCache = new();
    private readonly List<ImageBlit> _imageBlits = new();
    private ImageEditor _imageEditor = null!;

    private sealed class ImageBlit
    {
        public ImageAsset Image { get; }
        public int X { get; }
        public int Y { get; }
        public int Frame { get; }

        public ImageBlit(ImageAsset image, int x, int y, int frame)
        {
            Image = image;
            X = x;
            Y = y;
            Frame = frame;
        }
    }

    public ImageAssetStore ImageAssets => _imageAssets;

    public ImageEditor ImageEditor => _imageEditor;

    public ImageLayers ImageLayers => _imageLayers;

    private void CreateImageEditor()
    {
        _imageEditor = new ImageEditor(_imageAssets);
    }

    private void WireSpriteArtworkBridge()
    {
        _spriteEditor.RequestSpriteArtworkEditor = () =>
        {
            if (!FeatureGate.CanOpenSpriteArtworkEditor())
                return;

            _spriteEditor.Close();
            _imageEditor.OpenSpriteArtwork();
        };
    }

    public void OpenSpriteArtworkEditor()
    {
        if (!FeatureGate.CanOpenSpriteArtworkEditor())
            return;

        if (_spriteEditor.IsActive)
            _spriteEditor.Close();

        _imageEditor.OpenSpriteArtwork();
    }

    public void OpenFullImageEditor()
    {
        if (!FeatureGate.CanOpenFullImageEditor())
            return;

        if (_spriteEditor.IsActive)
            _spriteEditor.Close();

        _imageEditor.Open(ImageEditor.AccessMode.Full);
    }

    public void UpdateImageEditor(
        GameTime gameTime,
        MouseState mouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        CentauriSettings settings,
        Action? persistSettings = null)
    {
        _imageEditor.Update(
            gameTime,
            mouse,
            keyboard,
            previousKeyboard,
            settings,
            persistSettings);
    }

    public void DrawImageEditor(
        SpriteBatch spriteBatch,
        Graphics.BitmapFont font,
        Texture2D pixel)
    {
        _imageEditor.Draw(spriteBatch, font, pixel);
    }

    /// <summary>
    /// BASIC IMAGE blit — draws once into the graphics presentation list.
    /// Cleared by CLS. Does not persist over text.
    /// </summary>
    public void BlitImage(string name, int x, int y, int frame = 0)
    {
        var image = RequireImage(name);
        EnsureModeMatch(image);

        if (frame < 0 || frame >= image.FrameCount)
            throw new InvalidOperationException("FRAME NOT FOUND");

        _imageBlits.Add(new ImageBlit(image, x, y, frame));
    }

    public void ClearImageBlits()
    {
        _imageBlits.Clear();
    }

    public void SetBackground(int layer, string name)
    {
        if (layer is < 0 or > 1)
            throw new InvalidOperationException("BACKGROUND LAYER MUST BE 0 OR 1");

        var image = RequireImage(name);
        EnsureModeMatch(image);
        EnsureFullScreen(image, "BACKGROUND SIZE MISMATCH");

        if (layer == 0)
            _imageLayers.Background0 = image;
        else
            _imageLayers.Background1 = image;
    }

    public void HideBackground(int layer)
    {
        if (layer is < 0 or > 1)
            throw new InvalidOperationException("BACKGROUND LAYER MUST BE 0 OR 1");

        if (layer == 0)
            _imageLayers.Background0 = null;
        else
            _imageLayers.Background1 = null;
    }

    public void SetForeground(string name)
    {
        var image = RequireImage(name);
        EnsureModeMatch(image);
        EnsureFullScreen(image, "FOREGROUND SIZE MISMATCH");
        _imageLayers.Foreground = image;
    }

    public void HideForeground()
    {
        _imageLayers.Foreground = null;
    }

    public void ClearImageLayers()
    {
        _imageLayers.ClearAll();
        _imageBlits.Clear();
    }

    /// <summary>
    /// Draw order:
    /// paper → BG0 → BG1 → graphics (incl. IMAGE blits) → tiles → sprites → FG → text
    /// </summary>
    public void DrawBackgroundImages(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_imageLayers.Background0 != null)
            DrawImageAsset(spriteBatch, pixel, _imageLayers.Background0, 0, 0, 0);

        if (_imageLayers.Background1 != null)
            DrawImageAsset(spriteBatch, pixel, _imageLayers.Background1, 0, 0, 0);
    }

    public void DrawImageBlits(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var blit in _imageBlits)
            DrawImageAsset(spriteBatch, pixel, blit.Image, blit.X, blit.Y, blit.Frame);
    }

    public void DrawForegroundImage(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_imageLayers.Foreground != null)
            DrawImageAsset(spriteBatch, pixel, _imageLayers.Foreground, 0, 0, 0);
    }

    private void DrawImageAsset(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        ImageAsset image,
        int x,
        int y,
        int frame)
    {
        var texture = _imageTextureCache.GetTexture(pixel.GraphicsDevice, image, frame);
        spriteBatch.Draw(
            texture,
            new Rectangle(x, y, image.Width, image.Height),
            Color.White);
    }

    private ImageAsset RequireImage(string name)
    {
        var image = _imageAssets.Find(name);
        if (image == null)
            throw new InvalidOperationException("IMAGE NOT FOUND");

        return image;
    }

    private void EnsureModeMatch(ImageAsset image)
    {
        if (image.Mode != _displayMode)
            throw new InvalidOperationException("IMAGE MODE MISMATCH");
    }

    private void EnsureFullScreen(ImageAsset image, string error)
    {
        if (!image.IsFullScreen)
            throw new InvalidOperationException(error);
    }
}
