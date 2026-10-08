using System;
using System.Collections.Generic;

using Centauri64.CreativeTools;
using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Images;

public sealed partial class ImageEditor
{
    private void HandleShortcuts(KeyboardState keyboard, KeyboardState previous)
    {
        var control =
            keyboard.IsKeyDown(Keys.LeftControl) ||
            keyboard.IsKeyDown(Keys.RightControl);

        if (control && WasPressed(keyboard, previous, Keys.Z))
        {
            Undo();
            return;
        }

        if (control && WasPressed(keyboard, previous, Keys.Y))
        {
            Redo();
            return;
        }

        if (WasPressed(keyboard, previous, Keys.G))
            ToggleGrid();
        else if (WasPressed(keyboard, previous, Keys.S) && !control)
            ToggleShapeFilled();
        else if (WasPressed(keyboard, previous, Keys.OemPlus) ||
                 WasPressed(keyboard, previous, Keys.Add))
            ZoomIn();
        else if (WasPressed(keyboard, previous, Keys.OemMinus) ||
                 WasPressed(keyboard, previous, Keys.Subtract))
            ZoomOut();
        else if (WasPressed(keyboard, previous, Keys.D0) ||
                 WasPressed(keyboard, previous, Keys.NumPad0))
            FitZoom();
        else if (WasPressed(keyboard, previous, Keys.P))
            _tool = DrawTool.Pencil;
        else if (WasPressed(keyboard, previous, Keys.E))
            _tool = DrawTool.Eraser;
        else if (WasPressed(keyboard, previous, Keys.F))
            _tool = DrawTool.Fill;
        else if (WasPressed(keyboard, previous, Keys.L))
            _tool = DrawTool.Line;
        else if (WasPressed(keyboard, previous, Keys.R) && !control)
            _tool = DrawTool.Rectangle;
        else if (WasPressed(keyboard, previous, Keys.C))
            _tool = DrawTool.Circle;
        else if (WasPressed(keyboard, previous, Keys.I))
            _tool = DrawTool.Pick;
        else if (WasPressed(keyboard, previous, Keys.OemOpenBrackets))
            PreviousFrame();
        else if (WasPressed(keyboard, previous, Keys.OemCloseBrackets))
            NextFrame();
    }

    private void HandleZoomAndPan(MouseState mouse, KeyboardState keyboard)
    {
        if (_image == null)
        {
            _previousScroll = mouse.ScrollWheelValue;
            return;
        }

        EnsureWorkspace();
        var overWorkspace =
            mouse.X >= _workspaceX &&
            mouse.X < _workspaceX + _workspaceW &&
            mouse.Y >= _workspaceY &&
            mouse.Y < _workspaceY + _workspaceH;

        var scrollDelta = mouse.ScrollWheelValue - _previousScroll;
        _previousScroll = mouse.ScrollWheelValue;
        if (scrollDelta != 0 && overWorkspace)
        {
            var next = NextZoomStep(_zoom, scrollDelta > 0);
            ZoomTo(next, mouse.X, mouse.Y);
            _statusTip = $"ZOOM {FormatZoom(_zoom)}X";
        }

        var alt =
            keyboard.IsKeyDown(Keys.LeftAlt) ||
            keyboard.IsKeyDown(Keys.RightAlt);
        var middleDown = mouse.MiddleButton == ButtonState.Pressed;
        var altLeft =
            alt && mouse.LeftButton == ButtonState.Pressed;

        if ((middleDown || altLeft) && overWorkspace)
        {
            if (!_panning)
            {
                _panning = true;
                _panGrabX = mouse.X;
                _panGrabY = mouse.Y;
                _panGrabPanX = _panX;
                _panGrabPanY = _panY;
                _fitZoom = false;
                EndStrokeIfNeeded();
                _shapeDragging = false;
            }
            else
            {
                _panX = _panGrabPanX + (mouse.X - _panGrabX);
                _panY = _panGrabPanY + (mouse.Y - _panGrabY);
                ClampPan();
            }

            return;
        }

        _panning = false;
    }

    private bool HandleBrowserMouse(MouseState mouse)
    {
        var top = _toolBar.Bottom;
        var bottom = ScreenH - 16 - FrameStripHeight;
        var rect = new Rectangle(0, top, BrowserWidth, bottom - top);
        if (!rect.Contains(mouse.X, mouse.Y))
            return false;

        _statusTip = "IMAGES — FILTER + CLICK TO SELECT";

        if (mouse.LeftButton != ButtonState.Pressed ||
            _previousMouse.LeftButton != ButtonState.Released)
            return true;

        // Filter chips (full editor only)
        if (!IsSpriteArtworkMode)
        {
            var filterY = top + 18;
            if (mouse.Y >= filterY && mouse.Y < filterY + 14)
            {
                var filters = new[]
                {
                    BrowserFilter.All,
                    BrowserFilter.General,
                    BrowserFilter.Sprite,
                    BrowserFilter.Tileset,
                    BrowserFilter.Background
                };
                var chip = (mouse.X - 4) / 22;
                if (chip >= 0 && chip < filters.Length)
                    _browserFilter = filters[chip];
                return true;
            }
        }

        var visible = GetFilteredImages();
        var listTop = IsSpriteArtworkMode ? top + 20 : top + 36;
        var index = (mouse.Y - listTop) / 56;
        if (index >= 0 && index < visible.Count)
        {
            var asset = visible[index];
            for (var i = 0; i < _assets.Count; i++)
            {
                if (ReferenceEquals(_assets.Images[i], asset))
                {
                    SelectIndex(i);
                    break;
                }
            }
        }

        return true;
    }

    private bool HandleFrameStripMouse(MouseState mouse)
    {
        var strip = new Rectangle(
            BrowserWidth,
            ScreenH - 16 - FrameStripHeight,
            ScreenW - BrowserWidth - PaletteWidth,
            FrameStripHeight);
        if (!strip.Contains(mouse.X, mouse.Y))
            return false;

        if (_image == null)
            return true;

        _statusTip = "FRAMES — CLICK TO SELECT, + TO ADD";

        if (mouse.LeftButton != ButtonState.Pressed ||
            _previousMouse.LeftButton != ButtonState.Released)
            return true;

        var x = strip.X + 8;
        for (var i = 0; i < _image.FrameCount; i++)
        {
            var cell = new Rectangle(x, strip.Y + 8, 36, 36);
            if (cell.Contains(mouse.X, mouse.Y))
            {
                SelectFrame(i);
                return true;
            }

            x += 40;
        }

        var add = new Rectangle(x, strip.Y + 8, 36, 36);
        if (add.Contains(mouse.X, mouse.Y))
            NewFrame();

        return true;
    }

    private List<ImageAsset> GetFilteredImages()
    {
        var list = new List<ImageAsset>();
        foreach (var image in _assets.Images)
        {
            if (IsSpriteArtworkMode)
            {
                // Issue #3: only 16×16 Sprite-category artwork.
                if (image.Category == ImageCategory.Sprite &&
                    image.Width == CentauriSprite.WIDTH &&
                    image.Height == CentauriSprite.HEIGHT)
                {
                    list.Add(image);
                }

                continue;
            }

            var match = _browserFilter switch
            {
                BrowserFilter.All => true,
                BrowserFilter.General => image.Category == ImageCategory.General,
                BrowserFilter.Sprite => image.Category == ImageCategory.Sprite,
                BrowserFilter.Tileset => image.Category == ImageCategory.Tileset,
                BrowserFilter.Background => image.Category == ImageCategory.Background,
                _ => true
            };
            if (match)
                list.Add(image);
        }

        return list;
    }

    private bool HandlePaletteMouse(MouseState mouse)
    {
        var top = _toolBar.Bottom;
        var bottom = ScreenH - 16;
        var rect = new Rectangle(ScreenW - PaletteWidth, top, PaletteWidth, bottom - top);
        if (!rect.Contains(mouse.X, mouse.Y))
            return false;

        if (mouse.LeftButton != ButtonState.Pressed ||
            _previousMouse.LeftButton != ButtonState.Released)
            return true;

        var localX = mouse.X - rect.X - 8;
        var localY = mouse.Y - rect.Y - 24;
        if (localY < 0)
            return true;

        if (localY < 18)
        {
            _colour = ImageAsset.Transparent;
            _statusTip = "COLOUR: TRANSPARENT";
            return true;
        }

        localY -= 22;
        if (localY < 0)
            return true;

        var col = localX / 18;
        var row = localY / 18;
        if (col is < 0 or > 3)
            return true;

        var index = row * 4 + col;
        if (index is >= 0 and < CentauriPalette.MAX_COLORS)
        {
            _colour = index;
            _statusTip = $"COLOUR: {index}";
        }

        return true;
    }

    private void HandleCanvasMouse(MouseState mouse)
    {
        if (_image == null || _panning)
            return;

        LayoutCanvas();

        var leftDown = mouse.LeftButton == ButtonState.Pressed;
        var leftPressed = leftDown && _previousMouse.LeftButton == ButtonState.Released;
        var leftReleased = !leftDown && _previousMouse.LeftButton == ButtonState.Pressed;

        var overCanvas = TryMapPixel(mouse.X, mouse.Y, out var px, out var py);
        if (!overCanvas)
        {
            if (_strokeActive || _shapeDragging)
            {
                px = ClampPixelX(mouse.X);
                py = ClampPixelY(mouse.Y);
            }
            else
            {
                EndStrokeIfNeeded();
                return;
            }
        }

        _previewX = px;
        _previewY = py;

        if (mouse.RightButton == ButtonState.Pressed &&
            _previousMouse.RightButton == ButtonState.Released &&
            overCanvas)
        {
            _colour = _image.GetPixel(px, py);
            _statusTip = _colour < 0 ? "PICKED TRANSPARENT" : $"PICKED COLOUR {_colour}";
            return;
        }

        switch (_tool)
        {
            case DrawTool.Pencil:
            case DrawTool.Eraser:
                if (leftPressed && overCanvas)
                {
                    PushUndo();
                    _strokeActive = true;
                    _lastPixelX = px;
                    _lastPixelY = py;
                    Plot(_image, px, py, DrawColour());
                    _image.MarkChanged();
                    _dirty = true;
                }
                else if (leftDown && _strokeActive)
                {
                    DrawLinePixels(_image, _lastPixelX, _lastPixelY, px, py, DrawColour());
                    _lastPixelX = px;
                    _lastPixelY = py;
                    _image.MarkChanged();
                    _dirty = true;
                }
                else if (leftReleased)
                {
                    EndStrokeIfNeeded();
                }
                break;

            case DrawTool.Fill:
                if (leftPressed && overCanvas)
                {
                    PushUndo();
                    FloodFill(_image, px, py, DrawColour());
                    _image.MarkChanged();
                    _dirty = true;
                }
                break;

            case DrawTool.Pick:
                if (leftPressed && overCanvas)
                {
                    _colour = _image.GetPixel(px, py);
                    _statusTip = _colour < 0 ? "PICKED TRANSPARENT" : $"PICKED COLOUR {_colour}";
                }
                break;

            case DrawTool.Line:
            case DrawTool.Rectangle:
            case DrawTool.Circle:
                if (leftPressed && overCanvas)
                {
                    _shapeDragging = true;
                    _shapeStartX = px;
                    _shapeStartY = py;
                }
                else if (leftReleased && _shapeDragging)
                {
                    PushUndo();
                    CommitShape(_image, _shapeStartX, _shapeStartY, px, py);
                    _image.MarkChanged();
                    _dirty = true;
                    _shapeDragging = false;
                }
                break;
        }
    }

    private int ClampPixelX(int mouseX)
    {
        if (_image == null)
            return 0;

        var px = (int)((mouseX - _canvasX) / _zoom);
        return Math.Clamp(px, 0, _image.Width - 1);
    }

    private int ClampPixelY(int mouseY)
    {
        if (_image == null)
            return 0;

        var py = (int)((mouseY - _canvasY) / _zoom);
        return Math.Clamp(py, 0, _image.Height - 1);
    }

    private int DrawColour() =>
        _tool == DrawTool.Eraser ? ImageAsset.Transparent : _colour;

    private void LayoutCanvas()
    {
        EnsureWorkspace();

        if (_image == null)
        {
            _canvasX = _workspaceX;
            _canvasY = _workspaceY;
            _canvasW = _workspaceW;
            _canvasH = _workspaceH;
            return;
        }

        if (_fitZoom)
        {
            _zoom = Math.Min(
                _workspaceW / (float)_image.Width,
                _workspaceH / (float)_image.Height);
            _zoom = Math.Max(0.1f, _zoom);
            _panX = (_workspaceW - _image.Width * _zoom) / 2f;
            _panY = (_workspaceH - _image.Height * _zoom) / 2f;
        }
        else
        {
            ClampPan();
        }

        _canvasX = _workspaceX + (int)MathF.Round(_panX);
        _canvasY = _workspaceY + (int)MathF.Round(_panY);
        _canvasW = Math.Max(1, (int)MathF.Round(_image.Width * _zoom));
        _canvasH = Math.Max(1, (int)MathF.Round(_image.Height * _zoom));
    }

    private bool TryMapPixel(int mouseX, int mouseY, out int px, out int py)
    {
        px = 0;
        py = 0;
        if (_image == null)
            return false;

        if (mouseX < _workspaceX || mouseY < _workspaceY ||
            mouseX >= _workspaceX + _workspaceW ||
            mouseY >= _workspaceY + _workspaceH)
            return false;

        if (mouseX < _canvasX || mouseY < _canvasY ||
            mouseX >= _canvasX + _canvasW ||
            mouseY >= _canvasY + _canvasH)
            return false;

        px = (int)((mouseX - _canvasX) / _zoom);
        py = (int)((mouseY - _canvasY) / _zoom);
        return px >= 0 && py >= 0 && px < _image.Width && py < _image.Height;
    }

    private void DrawBrowser(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var top = _toolBar.Bottom;
        var height = ScreenH - 16 - FrameStripHeight - top;
        spriteBatch.Draw(pixel, new Rectangle(0, top, BrowserWidth, height), CreativeUiTheme.Panel);
        font.Draw(
            spriteBatch,
            IsSpriteArtworkMode ? "ARTWORK" : "IMAGES",
            new Vector2(8, top + 4),
            CreativeUiTheme.Accent);

        var listTop = top + 20;
        if (!IsSpriteArtworkMode)
        {
            var filters = new[] { "A", "G", "S", "T", "B" };
            var filterEnums = new[]
            {
                BrowserFilter.All,
                BrowserFilter.General,
                BrowserFilter.Sprite,
                BrowserFilter.Tileset,
                BrowserFilter.Background
            };
            for (var i = 0; i < filters.Length; i++)
            {
                var x = 4 + i * 22;
                var selected = _browserFilter == filterEnums[i];
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x, top + 18, 20, 12),
                    selected ? CreativeUiTheme.MenuHot : CreativeUiTheme.PanelLight);
                font.Draw(
                    spriteBatch,
                    filters[i],
                    new Vector2(x + 6, top + 18),
                    selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
            }

            listTop = top + 36;
        }

        var device = pixel.GraphicsDevice;
        var visible = GetFilteredImages();
        for (var i = 0; i < visible.Count; i++)
        {
            var image = visible[i];
            var y = listTop + i * 56;
            if (y + 52 > ScreenH - 16 - FrameStripHeight)
                break;

            var selected = ReferenceEquals(image, _image);
            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(2, y, BrowserWidth - 4, 52),
                    CreativeUiTheme.MenuHot);
            }

            var thumb = GetThumbnail(device, image, 0);
            if (thumb != null)
            {
                spriteBatch.Draw(
                    thumb,
                    new Rectangle(8, y + 4, 40, 30),
                    Color.White);
            }

            font.Draw(
                spriteBatch,
                Truncate(image.Name, 10),
                new Vector2(52, y + 8),
                selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
            font.Draw(
                spriteBatch,
                Truncate(image.Category.ToString().ToUpperInvariant(), 8),
                new Vector2(52, y + 22),
                CreativeUiTheme.Muted);
        }
    }

    private void DrawFrameStrip(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var strip = new Rectangle(
            BrowserWidth,
            ScreenH - 16 - FrameStripHeight,
            ScreenW - BrowserWidth - PaletteWidth,
            FrameStripHeight);
        spriteBatch.Draw(pixel, strip, CreativeUiTheme.Panel);
        font.Draw(spriteBatch, "FRAMES", new Vector2(strip.X + 8, strip.Y + 2), CreativeUiTheme.Accent);

        if (_image == null)
            return;

        var device = pixel.GraphicsDevice;
        var x = strip.X + 8;
        for (var i = 0; i < _image.FrameCount; i++)
        {
            var cell = new Rectangle(x, strip.Y + 14, 36, 36);
            var selected = i == _image.CurrentFrameIndex;
            spriteBatch.Draw(
                pixel,
                cell,
                selected ? CreativeUiTheme.MenuHot : CreativeUiTheme.PanelLight);

            var thumb = GetThumbnail(device, _image, i);
            if (thumb != null)
                spriteBatch.Draw(thumb, new Rectangle(cell.X + 2, cell.Y + 2, 32, 24), Color.White);

            font.Draw(
                spriteBatch,
                (i + 1).ToString(),
                new Vector2(cell.X + 14, cell.Y + 26),
                selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Muted);

            x += 40;
        }

        var add = new Rectangle(x, strip.Y + 14, 36, 36);
        spriteBatch.Draw(pixel, add, CreativeUiTheme.PanelLight);
        font.Draw(spriteBatch, "+", new Vector2(add.X + 14, add.Y + 12), CreativeUiTheme.Accent);
    }

    private void DrawPalette(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var top = _toolBar.Bottom;
        var height = ScreenH - 16 - FrameStripHeight - top;
        var x = ScreenW - PaletteWidth;
        spriteBatch.Draw(pixel, new Rectangle(x, top, PaletteWidth, height), CreativeUiTheme.Panel);
        font.Draw(spriteBatch, "COLOUR", new Vector2(x + 8, top + 4), CreativeUiTheme.Accent);

        var swatch = new Rectangle(x + 8, top + 24, PaletteWidth - 16, 16);
        spriteBatch.Draw(pixel, swatch, Color.Black);
        DrawChecker(spriteBatch, pixel, swatch);
        if (_colour < 0)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(swatch.X - 1, swatch.Y - 1, swatch.Width + 2, swatch.Height + 2),
                CreativeUiTheme.Highlight);
        }

        font.Draw(spriteBatch, "TRANS", new Vector2(x + 20, top + 28), CreativeUiTheme.Text);

        for (var i = 0; i < CentauriPalette.MAX_COLORS; i++)
        {
            var col = i % 4;
            var row = i / 4;
            var rx = x + 8 + col * 18;
            var ry = top + 48 + row * 18;
            var cell = new Rectangle(rx, ry, 16, 16);
            spriteBatch.Draw(pixel, cell, CentauriPalette.Get(i));
            if (_colour == i)
            {
                spriteBatch.Draw(pixel, new Rectangle(rx - 1, ry - 1, 18, 1), CreativeUiTheme.Highlight);
                spriteBatch.Draw(pixel, new Rectangle(rx - 1, ry + 16, 18, 1), CreativeUiTheme.Highlight);
                spriteBatch.Draw(pixel, new Rectangle(rx - 1, ry, 1, 16), CreativeUiTheme.Highlight);
                spriteBatch.Draw(pixel, new Rectangle(rx + 16, ry, 1, 16), CreativeUiTheme.Highlight);
            }
        }
    }

    private void DrawCanvas(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        LayoutCanvas();
        var area = new Rectangle(_workspaceX, _workspaceY, _workspaceW, _workspaceH);
        spriteBatch.Draw(pixel, area, CreativeUiTheme.CanvasChrome);

        if (_image == null)
        {
            font.Draw(
                spriteBatch,
                "IMAGE > NEW IMAGE TO CREATE",
                new Vector2(area.X + 40, area.Y + area.Height / 2),
                CreativeUiTheme.Muted);
            return;
        }

        var imageRect = new Rectangle(_canvasX, _canvasY, _canvasW, _canvasH);
        var visible = Rectangle.Intersect(area, imageRect);
        if (visible.Width <= 0 || visible.Height <= 0)
            return;

        DrawChecker(spriteBatch, pixel, visible);

        var srcX = (visible.X - _canvasX) / _zoom;
        var srcY = (visible.Y - _canvasY) / _zoom;
        var srcW = visible.Width / _zoom;
        var srcH = visible.Height / _zoom;
        var source = new Rectangle(
            (int)MathF.Floor(srcX),
            (int)MathF.Floor(srcY),
            Math.Max(1, (int)MathF.Ceiling(srcW)),
            Math.Max(1, (int)MathF.Ceiling(srcH)));

        source.Width = Math.Min(source.Width, _image.Width - source.X);
        source.Height = Math.Min(source.Height, _image.Height - source.Y);

        var texture = _canvasCache.GetTexture(
            pixel.GraphicsDevice,
            _image,
            _image.CurrentFrameIndex);
        if (source.Width > 0 && source.Height > 0)
        {
            spriteBatch.Draw(
                texture,
                visible,
                source,
                Color.White);
        }

        if (_shapeDragging && _previewX >= 0)
            DrawShapePreview(spriteBatch, pixel, _shapeStartX, _shapeStartY, _previewX, _previewY);

        if (_showGrid && _zoom >= 4f)
            DrawPixelGrid(spriteBatch, pixel, visible);
    }

    private void DrawPixelGrid(SpriteBatch spriteBatch, Texture2D pixel, Rectangle clip)
    {
        if (_image == null)
            return;

        var colour = new Color(255, 255, 255, 40);
        var startX = Math.Max(0, (int)MathF.Floor((clip.X - _canvasX) / _zoom));
        var endX = Math.Min(_image.Width, (int)MathF.Ceiling((clip.Right - _canvasX) / _zoom));
        var startY = Math.Max(0, (int)MathF.Floor((clip.Y - _canvasY) / _zoom));
        var endY = Math.Min(_image.Height, (int)MathF.Ceiling((clip.Bottom - _canvasY) / _zoom));

        for (var x = startX; x <= endX; x++)
        {
            var sx = _canvasX + (int)(x * _zoom);
            if (sx < clip.X || sx >= clip.Right)
                continue;

            spriteBatch.Draw(
                pixel,
                new Rectangle(sx, clip.Y, 1, clip.Height),
                colour);
        }

        for (var y = startY; y <= endY; y++)
        {
            var sy = _canvasY + (int)(y * _zoom);
            if (sy < clip.Y || sy >= clip.Bottom)
                continue;

            spriteBatch.Draw(
                pixel,
                new Rectangle(clip.X, sy, clip.Width, 1),
                colour);
        }
    }

    private void DrawShapePreview(SpriteBatch spriteBatch, Texture2D pixel, int x0, int y0, int x1, int y1)
    {
        var colour = _colour < 0 ? Color.White : CentauriPalette.Get(_colour);
        switch (_tool)
        {
            case DrawTool.Line:
                BresenhamPreview(spriteBatch, pixel, x0, y0, x1, y1, colour);
                break;
            case DrawTool.Rectangle:
                if (UsesFilledShapes())
                    FillRectPreview(spriteBatch, pixel, x0, y0, x1, y1, colour);
                else
                {
                    BresenhamPreview(spriteBatch, pixel, x0, y0, x1, y0, colour);
                    BresenhamPreview(spriteBatch, pixel, x1, y0, x1, y1, colour);
                    BresenhamPreview(spriteBatch, pixel, x1, y1, x0, y1, colour);
                    BresenhamPreview(spriteBatch, pixel, x0, y1, x0, y0, colour);
                }
                break;
            case DrawTool.Circle:
                if (UsesFilledShapes())
                    FillEllipsePreview(spriteBatch, pixel, x0, y0, x1, y1, colour);
                else
                    EllipsePreview(spriteBatch, pixel, x0, y0, x1, y1, colour);
                break;
        }
    }

    private void FillRectPreview(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int x0, int y0, int x1, int y1,
        Color colour)
    {
        var left = Math.Min(x0, x1);
        var right = Math.Max(x0, x1);
        var top = Math.Min(y0, y1);
        var bottom = Math.Max(y0, y1);
        var size = Math.Max(1, (int)_zoom);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                _canvasX + (int)(left * _zoom),
                _canvasY + (int)(top * _zoom),
                Math.Max(size, (int)((right - left + 1) * _zoom)),
                Math.Max(size, (int)((bottom - top + 1) * _zoom))),
            colour * 0.7f);
    }

    private void FillEllipsePreview(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int x0, int y0, int x1, int y1,
        Color colour)
    {
        foreach (var (x, y) in TraceFilledEllipse(x0, y0, x1, y1))
            DrawPreviewPixel(spriteBatch, pixel, x, y, colour * 0.7f);
    }

    private void BresenhamPreview(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int x0, int y0, int x1, int y1,
        Color colour)
    {
        foreach (var (x, y) in TraceLine(x0, y0, x1, y1))
            DrawPreviewPixel(spriteBatch, pixel, x, y, colour);
    }

    private void EllipsePreview(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int x0, int y0, int x1, int y1,
        Color colour)
    {
        foreach (var (x, y) in TraceEllipse(x0, y0, x1, y1))
            DrawPreviewPixel(spriteBatch, pixel, x, y, colour);
    }

    private void DrawPreviewPixel(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, Color colour)
    {
        var size = Math.Max(1, (int)_zoom);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                _canvasX + (int)(x * _zoom),
                _canvasY + (int)(y * _zoom),
                size,
                size),
            colour);
    }

    private static void DrawChecker(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
    {
        const int cell = 8;
        for (var y = 0; y < rect.Height; y += cell)
        {
            for (var x = 0; x < rect.Width; x += cell)
            {
                var odd = ((x / cell) + (y / cell)) % 2 != 0;
                var colour = odd ? new Color(50, 50, 60) : new Color(35, 35, 45);
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        rect.X + x,
                        rect.Y + y,
                        Math.Min(cell, rect.Width - x),
                        Math.Min(cell, rect.Height - y)),
                    colour);
            }
        }
    }

    private Texture2D? GetThumbnail(GraphicsDevice device, ImageAsset image, int frameIndex)
    {
        frameIndex = Math.Clamp(frameIndex, 0, image.FrameCount - 1);
        var key = (image, frameIndex);
        if (!_thumbnails.TryGetValue(key, out var entry))
        {
            entry = new Thumbnail();
            _thumbnails[key] = entry;
        }

        if (entry.Texture == null)
            entry.Texture = new Texture2D(device, 32, 24);

        var frame = image.GetFrame(frameIndex);
        if (entry.Revision == frame.Revision)
            return entry.Texture;

        var buffer = new Color[32 * 24];
        for (var y = 0; y < 24; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var srcX = x * image.Width / 32;
                var srcY = y * image.Height / 24;
                var c = frame.Pixels[srcY, srcX];
                buffer[y * 32 + x] = c < 0
                    ? (((x / 4) + (y / 4)) % 2 == 0
                        ? new Color(50, 50, 60)
                        : new Color(35, 35, 45))
                    : CentauriPalette.Get(c);
            }
        }

        entry.Texture.SetData(buffer);
        entry.Revision = frame.Revision;
        return entry.Texture;
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= _assets.Count)
            return;

        EndStrokeIfNeeded();
        _selectedIndex = index;
        _image = _assets.Images[index];
        _image.SelectFrame(0);
        ClearHistory();
        FitZoom();
    }

    private static string Truncate(string text, int max)
    {
        return text.Length <= max ? text : text[..max];
    }
}
