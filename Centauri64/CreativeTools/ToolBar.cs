using System;
using System.Collections.Generic;

using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.CreativeTools;

public sealed class ToolButton
{
    public string Id { get; }
    public string Label { get; }
    public string Shortcut { get; }
    public string Tip { get; }
    public Action Action { get; }
    public Func<bool>? IsSelected { get; }
    public bool IsSeparator { get; }

    public ToolButton(
        string id,
        string label,
        string tip,
        Action action,
        Func<bool>? isSelected = null,
        string shortcut = "")
    {
        Id = id;
        Label = label;
        Shortcut = shortcut ?? "";
        Tip = tip;
        Action = action;
        IsSelected = isSelected;
    }

    private ToolButton()
    {
        Id = "sep";
        Label = "";
        Shortcut = "";
        Tip = "";
        Action = static () => { };
        IsSeparator = true;
    }

    public static ToolButton Separator() => new();
}

public sealed class ToolBar
{
    private const int PadX = 6;
    private const int Gap = 4;
    private const int SeparatorWidth = 8;
    private const int CharWidth = 8;

    private readonly List<ToolButton> _buttons = new();
    private readonly List<Rectangle> _bounds = new();
    private readonly int _y;
    private readonly int _rowHeight;
    private int _layoutWidth = 640;
    private int _totalHeight;
    private int _hoverIndex = -1;

    public int Bottom => _y + _totalHeight;
    public string HoverTip { get; private set; } = "";

    public ToolBar(int y, int rowHeight = 24, int unusedLegacyButtonWidth = 48)
    {
        _y = y;
        _rowHeight = rowHeight;
        _totalHeight = rowHeight;
        _ = unusedLegacyButtonWidth;
    }

    public void SetButtons(IEnumerable<ToolButton> buttons)
    {
        _buttons.Clear();
        _buttons.AddRange(buttons);
        RebuildBounds(_layoutWidth);
    }

    public bool HandleMouse(MouseState mouse, MouseState previous)
    {
        _hoverIndex = -1;
        HoverTip = "";

        if (mouse.Y < _y || mouse.Y >= Bottom)
            return false;

        for (var i = 0; i < _bounds.Count; i++)
        {
            if (!_bounds[i].Contains(mouse.X, mouse.Y))
                continue;

            if (_buttons[i].IsSeparator)
                return true;

            _hoverIndex = i;
            HoverTip = _buttons[i].Tip;

            if (mouse.LeftButton == ButtonState.Pressed &&
                previous.LeftButton == ButtonState.Released)
            {
                _buttons[i].Action();
            }

            return true;
        }

        return true;
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel, int width)
    {
        if (width != _layoutWidth || _bounds.Count != _buttons.Count)
            RebuildBounds(width);

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, _y, width, _totalHeight),
            CreativeUiTheme.Panel);

        for (var i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            var bounds = _bounds[i];

            if (button.IsSeparator)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        bounds.X + bounds.Width / 2,
                        bounds.Y + 5,
                        1,
                        bounds.Height - 10),
                    CreativeUiTheme.Border);
                continue;
            }

            var selected = button.IsSelected?.Invoke() ?? false;
            var hot = i == _hoverIndex;

            if (selected || hot)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(bounds.X, bounds.Y + 1, bounds.Width, bounds.Height - 2),
                    selected ? CreativeUiTheme.MenuHot : CreativeUiTheme.PanelLight);
            }

            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(bounds.X, bounds.Bottom - 3, bounds.Width, 2),
                    CreativeUiTheme.Accent);
            }

            var textColour = selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text;
            font.Draw(
                spriteBatch,
                button.Label,
                new Vector2(bounds.X + PadX, bounds.Y + 8),
                textColour);

            if (!string.IsNullOrEmpty(button.Shortcut))
            {
                var shortcut = "[" + button.Shortcut + "]";
                var sx = bounds.X + PadX + button.Label.Length * CharWidth;
                font.Draw(
                    spriteBatch,
                    shortcut,
                    new Vector2(sx, bounds.Y + 8),
                    selected ? CreativeUiTheme.Accent : CreativeUiTheme.Muted);
            }
        }
    }

    private void RebuildBounds(int screenWidth)
    {
        _layoutWidth = Math.Max(160, screenWidth);
        _bounds.Clear();

        var x = Gap;
        var row = 0;
        for (var i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            var buttonWidth = button.IsSeparator
                ? SeparatorWidth
                : MeasureWidth(button);

            // Wrap to next row if this control would clip.
            if (x > Gap && x + buttonWidth + Gap > _layoutWidth)
            {
                row++;
                x = Gap;
            }

            // Separators at the start of a row are skipped visually.
            if (button.IsSeparator && x == Gap)
            {
                _bounds.Add(new Rectangle(x, _y + row * _rowHeight, 0, _rowHeight));
                continue;
            }

            _bounds.Add(new Rectangle(
                x,
                _y + row * _rowHeight,
                buttonWidth,
                _rowHeight));
            x += buttonWidth + Gap;
        }

        _totalHeight = (row + 1) * _rowHeight;
    }

    private static int MeasureWidth(ToolButton button)
    {
        var labelWidth = button.Label.Length * CharWidth;
        var shortcutWidth = string.IsNullOrEmpty(button.Shortcut)
            ? 0
            : (button.Shortcut.Length + 2) * CharWidth;
        return PadX * 2 + labelWidth + shortcutWidth;
    }
}
