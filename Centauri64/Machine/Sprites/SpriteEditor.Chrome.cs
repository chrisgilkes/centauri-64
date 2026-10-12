using System;

using Centauri64.CreativeTools;
using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Machine.Sprites;

public sealed partial class SpriteEditor
{
    private void BuildMenus()
    {
        _menuBar.SetMenus(new[]
        {
            new MenuDefinition("SPRITE",
                new MenuItem("NEW SPRITE", () => BeginNewSpritePrompt(), "N"),
                new MenuItem("COPY SPRITE", () => BeginCopySpritePrompt(), "SHIFT+N"),
                new MenuItem("RENAME SPRITE", () => BeginRenameSpritePrompt(), "R", () => _asset != null),
                new MenuItem("DELETE SPRITE", () => _confirm = ConfirmKind.DeleteSprite, "X", () => _asset != null),
                new MenuItem("PREV SPRITE", SelectPreviousAsset, "<"),
                new MenuItem("NEXT SPRITE", SelectNextAsset, ">"),
                new MenuItem("EXIT", Close, "ESC")),
            new MenuDefinition("ANIM",
                new MenuItem("NEW ANIMATION", () => BeginNewAnimationPrompt(), "M", () => _asset != null),
                new MenuItem("COPY ANIMATION", () => BeginCopyAnimationPrompt(), "SHIFT+M", () => _animation != null),
                new MenuItem("DELETE ANIMATION", () => _confirm = ConfirmKind.DeleteAnimation, "SHIFT+D", () => _animation != null),
                new MenuItem("PREV ANIMATION", SelectPreviousAnimation, "UP"),
                new MenuItem("NEXT ANIMATION", SelectNextAnimation, "DOWN"),
                new MenuItem("BIND ARTWORK", BeginArtworkSelection, "B", () => _animation != null),
                new MenuItem("OPEN ART STUDIO", OpenArtStudio, "I")),
            new MenuDefinition("FRAME",
                new MenuItem("PREV FRAME", SelectPreviousFrame, "["),
                new MenuItem("NEXT FRAME", SelectNextFrame, "]")),
            new MenuDefinition("HELP",
                new MenuItem("CONTROLS", () => _showingHelp = true, "?"))
        });
    }

    private void BuildToolbar()
    {
        _toolBar.SetButtons(new[]
        {
            new ToolButton("new", "NEW", "NEW SPRITE", BeginNewSpritePrompt, shortcut: "N"),
            new ToolButton("anim", "ANIM", "NEW ANIMATION", BeginNewAnimationPrompt, shortcut: "M"),
            ToolButton.Separator(),
            new ToolButton("bind", "BIND", "BIND 16X16 ART STUDIO IMAGE INTO THIS ANIM", BeginArtworkSelection, shortcut: "B"),
            new ToolButton("art", "ART", "OPEN ART STUDIO TO PAINT 16X16 FRAMES", OpenArtStudio, shortcut: "I"),
            ToolButton.Separator(),
            new ToolButton("frprev", "[-", "PREVIOUS BAKED FRAME", SelectPreviousFrame, shortcut: "["),
            new ToolButton("frnext", "+]", "NEXT BAKED FRAME", SelectNextFrame, shortcut: "]")
        });
    }

    private void OpenArtStudio()
    {
        RequestSpriteArtworkEditor?.Invoke();
    }

    private void LayoutChrome()
    {
        var workspaceY = _toolBar.Bottom;
        var workspaceH = ScreenH - 16 - PreviewStripHeight - workspaceY;
        var workspaceX = BrowserWidth;
        var workspaceW = ScreenW - BrowserWidth - PaletteWidth;

        var maxPixel = Math.Min(workspaceW - 16, workspaceH - 16) / CentauriSprite.WIDTH;
        PixelSize = Math.Clamp(maxPixel, 8, 24);

        var gridW = CentauriSprite.WIDTH * PixelSize;
        var gridH = CentauriSprite.HEIGHT * PixelSize;
        GridX = workspaceX + (workspaceW - gridW) / 2;
        GridY = workspaceY + (workspaceH - gridH) / 2;

        PaletteX = ScreenW - PaletteWidth + 8;
        PaletteY = workspaceY + 20;

        PreviewWidth = Math.Min(200, workspaceW - 16);
        PreviewHeight = PreviewStripHeight - 16;
        PreviewX = workspaceX + 8;
        PreviewY = ScreenH - 16 - PreviewStripHeight + 8;
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        if (!IsActive)
            return;

        CreativeUiTheme.UseSpriteTool();
        LayoutChrome();

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, ScreenW, ScreenH),
            CreativeUiTheme.CanvasChrome);

        DrawBrowser(spriteBatch, font, pixel);
        DrawCanvasPanel(spriteBatch, font, pixel);
        DrawPalettePanel(spriteBatch, font, pixel);
        DrawPreviewStrip(spriteBatch, font, pixel);

        _toolBar.Draw(spriteBatch, font, pixel, ScreenW);
        _menuBar.Draw(spriteBatch, font, pixel, ScreenW);

        var status = BuildStatusText();
        var tip = !string.IsNullOrEmpty(_message) ? _message : _statusTip;
        _statusBar.Draw(spriteBatch, font, pixel, ScreenW, status, tip);

        DrawModalOverlay(spriteBatch, font, pixel);
    }

    private void DrawBrowser(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var top = _toolBar.Bottom;
        var height = ScreenH - 16 - PreviewStripHeight - top;
        spriteBatch.Draw(pixel, new Rectangle(0, top, BrowserWidth, height), CreativeUiTheme.Panel);
        font.Draw(spriteBatch, "SPRITES", new Vector2(8, top + 4), CreativeUiTheme.Accent);

        var assets = _assets.Assets;
        var y = top + 22;
        for (var i = 0; i < assets.Count; i++)
        {
            if (y + 14 > top + height - 4)
                break;

            var selected = i == _currentAssetIndex;
            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(2, y - 2, BrowserWidth - 4, 14),
                    CreativeUiTheme.MenuHot);
            }

            font.Draw(
                spriteBatch,
                Truncate(assets[i].Name, 12),
                new Vector2(8, y),
                selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
            y += 14;
        }

        if (assets.Count == 0)
        {
            font.Draw(spriteBatch, "NONE YET", new Vector2(8, top + 24), CreativeUiTheme.Muted);
            font.Draw(spriteBatch, "N = NEW", new Vector2(8, top + 40), CreativeUiTheme.Muted);
        }
    }

    private void DrawCanvasPanel(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var top = _toolBar.Bottom;
        var area = new Rectangle(
            BrowserWidth,
            top,
            ScreenW - BrowserWidth - PaletteWidth,
            ScreenH - 16 - PreviewStripHeight - top);
        spriteBatch.Draw(pixel, area, CreativeUiTheme.CanvasChrome);

        if (_frame == null)
        {
            font.Draw(
                spriteBatch,
                "N NEW SPRITE",
                new Vector2(area.X + 24, area.Y + area.Height / 2),
                CreativeUiTheme.Muted);
            return;
        }

        DrawPixelCanvas(spriteBatch, pixel);
    }

    private void DrawPixelCanvas(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_frame == null)
            return;

        SpriteFrame? onionFrame = null;
        if (_onionSkinEnabled &&
            _animation != null &&
            _animation.Frames.Count > 1)
        {
            var onionFrameIndex = _currentFrameIndex - 1;
            if (onionFrameIndex < 0)
                onionFrameIndex = _animation.Frames.Count - 1;
            onionFrame = _animation.Frames[onionFrameIndex];
        }

        var gridColour = new Color(0, 0, 0, 110);

        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                var dest = new Rectangle(
                    GridX + x * PixelSize,
                    GridY + y * PixelSize,
                    PixelSize,
                    PixelSize);

                var colourIndex = _frame.Pixels[y, x];
                Color colour;
                if (colourIndex != CentauriSprite.TRANSPARENT)
                {
                    colour = CentauriPalette.Get(colourIndex);
                }
                else if (onionFrame != null &&
                         onionFrame.Pixels[y, x] != CentauriSprite.TRANSPARENT)
                {
                    colour = CentauriPalette.Get(onionFrame.Pixels[y, x]) * 0.3f;
                }
                else
                {
                    var odd = ((x + y) & 1) != 0;
                    colour = odd ? new Color(50, 50, 60) : new Color(35, 35, 45);
                }

                spriteBatch.Draw(pixel, dest, colour);
            }
        }

        var gridW = CentauriSprite.WIDTH * PixelSize;
        var gridH = CentauriSprite.HEIGHT * PixelSize;
        for (var x = 0; x <= CentauriSprite.WIDTH; x++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(GridX + x * PixelSize, GridY, 1, gridH),
                gridColour);
        }

        for (var y = 0; y <= CentauriSprite.HEIGHT; y++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(GridX, GridY + y * PixelSize, gridW, 1),
                gridColour);
        }
    }

    private void DrawPalettePanel(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var top = _toolBar.Bottom;
        var height = ScreenH - 16 - PreviewStripHeight - top;
        var x = ScreenW - PaletteWidth;
        spriteBatch.Draw(pixel, new Rectangle(x, top, PaletteWidth, height), CreativeUiTheme.Panel);
        font.Draw(spriteBatch, "BINDER", new Vector2(x + 8, top + 4), CreativeUiTheme.Accent);
        font.Draw(spriteBatch, "PAINT IN", new Vector2(x + 8, top + 28), CreativeUiTheme.Muted);
        font.Draw(spriteBatch, "ART STUDIO", new Vector2(x + 8, top + 42), CreativeUiTheme.Highlight);
        font.Draw(spriteBatch, "I OPEN", new Vector2(x + 8, top + 66), CreativeUiTheme.Text);
        font.Draw(spriteBatch, "B BIND", new Vector2(x + 8, top + 80), CreativeUiTheme.Text);
        font.Draw(spriteBatch, "CLICK GRID", new Vector2(x + 8, top + 104), CreativeUiTheme.Muted);
        font.Draw(spriteBatch, "TO BIND", new Vector2(x + 8, top + 118), CreativeUiTheme.Muted);
    }

    private void DrawPreviewStrip(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        var strip = new Rectangle(
            BrowserWidth,
            ScreenH - 16 - PreviewStripHeight,
            ScreenW - BrowserWidth - PaletteWidth,
            PreviewStripHeight);
        spriteBatch.Draw(pixel, strip, CreativeUiTheme.Panel);
        font.Draw(spriteBatch, "PREVIEW", new Vector2(strip.X + 8, strip.Y + 2), CreativeUiTheme.Accent);

        if (_animation == null || _animation.Frames.Count == 0)
            return;

        // Square thumb slot — same approach as Image Editor frame strip (no wide frame).
        var thumbSlot = new Rectangle(strip.X + 8, strip.Y + 16, 48, 48);
        spriteBatch.Draw(pixel, thumbSlot, CreativeUiTheme.PanelLight);
        DrawMiniChecker(spriteBatch, pixel, thumbSlot);

        var previewFrame = _animation.Frames[_previewFrameIndex];
        var dest = FitWithin(thumbSlot, CentauriSprite.WIDTH, CentauriSprite.HEIGHT);
        var scaleX = dest.Width / (float)CentauriSprite.WIDTH;
        var scaleY = dest.Height / (float)CentauriSprite.HEIGHT;

        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                var colourIndex = previewFrame.Pixels[y, x];
                if (colourIndex == CentauriSprite.TRANSPARENT)
                    continue;

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        dest.X + (int)(x * scaleX),
                        dest.Y + (int)(y * scaleY),
                        Math.Max(1, (int)MathF.Ceiling(scaleX)),
                        Math.Max(1, (int)MathF.Ceiling(scaleY))),
                    CentauriPalette.Get(colourIndex));
            }
        }

        var infoX = thumbSlot.Right + 12;
        if (_asset != null)
            font.Draw(spriteBatch, Truncate(_asset.Name, 12), new Vector2(infoX, strip.Y + 18), CreativeUiTheme.Text);
        if (_animation != null)
        {
            font.Draw(
                spriteBatch,
                Truncate(_animation.Name, 12),
                new Vector2(infoX, strip.Y + 32),
                CreativeUiTheme.Muted);
            font.Draw(
                spriteBatch,
                $"FR {_currentFrameIndex + 1}/{_animation.Frames.Count}",
                new Vector2(infoX, strip.Y + 46),
                CreativeUiTheme.Muted);
        }
    }

    private static Rectangle FitWithin(Rectangle slot, int srcW, int srcH)
    {
        srcW = Math.Max(1, srcW);
        srcH = Math.Max(1, srcH);
        var scale = Math.Min(slot.Width / (float)srcW, slot.Height / (float)srcH);
        var w = Math.Max(1, (int)Math.Round(srcW * scale));
        var h = Math.Max(1, (int)Math.Round(srcH * scale));
        return new Rectangle(
            slot.X + (slot.Width - w) / 2,
            slot.Y + (slot.Height - h) / 2,
            w,
            h);
    }

    private void DrawModalOverlay(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        if (!_enteringSpriteName &&
            !_enteringAnimationName &&
            _confirm == ConfirmKind.None &&
            !_selectingArtwork &&
            !_showingHelp)
        {
            return;
        }

        var panel = new Rectangle(160, 120, 320, 220);
        spriteBatch.Draw(pixel, panel, CreativeUiTheme.Panel);
        DrawRectangle(spriteBatch, pixel, panel, CreativeUiTheme.Accent);

        var x = panel.X + 16;
        if (_enteringSpriteName)
        {
            var title = "NEW SPRITE:";
            if (_copyingSprite)
                title = "COPY SPRITE:";
            else if (_renamingSprite)
                title = "RENAME SPRITE:";
            DrawPrompt(spriteBatch, font, x, title, _newSpriteName);
            return;
        }

        if (_enteringAnimationName)
        {
            DrawPrompt(
                spriteBatch,
                font,
                x,
                _copyingAnimation ? "COPY ANIMATION:" : "NEW ANIMATION:",
                _newAnimationName);
            return;
        }

        if (_confirm != ConfirmKind.None)
        {
            font.Draw(spriteBatch, ConfirmText(), new Vector2(x, 160), CreativeUiTheme.Highlight);
            font.Draw(spriteBatch, "Y YES    N NO", new Vector2(x, 190), CreativeUiTheme.Text);
            return;
        }

        if (_selectingArtwork)
        {
            DrawArtworkSelection(spriteBatch, font, x);
            return;
        }

        if (_showingHelp)
            DrawHelp(spriteBatch, font, x);
    }

    private string BuildStatusText()
    {
        if (_asset == null)
            return "SPRITE BINDER — NEW SPRITE, THEN BIND ART STUDIO";

        var anim = _animation?.Name ?? "-";
        var frame = _animation == null
            ? "0/0"
            : $"{_currentFrameIndex + 1}/{_animation.Frames.Count}";
        var art = string.IsNullOrEmpty(_animation?.SourceImageName)
            ? "UNBOUND"
            : _animation!.SourceImageName;
        var dirty = _dirty ? "  *UNSAVED" : "";
        return $"SPRITE BINDER: {_asset.Name}  ANIM:{anim}  FR:{frame}  ART:{art}{dirty}";
    }

    private bool HandleBrowserMouse(MouseState mouse)
    {
        var top = _toolBar.Bottom;
        var height = ScreenH - 16 - PreviewStripHeight - top;
        if (mouse.X < 0 || mouse.X >= BrowserWidth ||
            mouse.Y < top || mouse.Y >= top + height)
        {
            return false;
        }

        var clicked =
            mouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released;
        if (!clicked)
            return true;

        var index = (mouse.Y - top - 22) / 14;
        if (index >= 0 && index < _assets.Assets.Count)
            SelectAsset(index);

        return true;
    }

    private void BeginNewSpritePrompt()
    {
        _enteringSpriteName = true;
        _copyingSprite = false;
        _renamingSprite = false;
        _newSpriteName = "";
        _message = "";
    }

    private void BeginCopySpritePrompt()
    {
        _enteringSpriteName = true;
        _copyingSprite = true;
        _renamingSprite = false;
        _newSpriteName = "";
        _message = "";
    }

    private void BeginRenameSpritePrompt()
    {
        if (_asset == null)
            return;
        _enteringSpriteName = true;
        _copyingSprite = false;
        _renamingSprite = true;
        _newSpriteName = _asset.Name;
        _message = "";
    }

    private void BeginNewAnimationPrompt()
    {
        if (_asset == null)
            return;
        _copyingAnimation = false;
        _enteringAnimationName = true;
        _newAnimationName = "";
        _message = "";
    }

    private void BeginCopyAnimationPrompt()
    {
        if (_animation == null)
            return;
        _copyingAnimation = true;
        _enteringAnimationName = true;
        _newAnimationName = "";
        _message = "";
    }

    private void ToggleOnion()
    {
        _onionSkinEnabled = !_onionSkinEnabled;
        _statusTip = _onionSkinEnabled ? "ONION SKIN ON" : "ONION SKIN OFF";
    }

    private void ConfirmClearFrame()
    {
        if (_frame == null)
            return;
        PushUndo();
        _frame.Clear();
        MarkDirty();
        _statusTip = "FRAME CLEARED";
    }

    private static void DrawRectangle(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle rectangle,
        Color colour)
    {
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 1), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Bottom - 1, rectangle.Width, 1), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), colour);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 1, rectangle.Y, 1, rectangle.Height), colour);
    }

    private static void DrawMiniChecker(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
    {
        const int cell = 4;
        for (var y = 0; y < rect.Height; y += cell)
        {
            for (var x = 0; x < rect.Width; x += cell)
            {
                var odd = ((x / cell) + (y / cell)) % 2 != 0;
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        rect.X + x,
                        rect.Y + y,
                        Math.Min(cell, rect.Width - x),
                        Math.Min(cell, rect.Height - y)),
                    odd ? new Color(50, 50, 60) : new Color(35, 35, 45));
            }
        }
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max];
}
