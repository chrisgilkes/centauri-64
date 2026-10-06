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
    public string Tip { get; }
    public Action Action { get; }
    public Func<bool>? IsSelected { get; }

    public ToolButton(
        string id,
        string label,
        string tip,
        Action action,
        Func<bool>? isSelected = null)
    {
        Id = id;
        Label = label;
        Tip = tip;
        Action = action;
        IsSelected = isSelected;
    }
}

public sealed class ToolBar
{
    private readonly List<ToolButton> _buttons = new();
    private readonly int _y;
    private readonly int _height;
    private readonly int _buttonWidth;
    private int _hoverIndex = -1;

    public int Bottom => _y + _height;
    public string HoverTip { get; private set; } = "";

    public ToolBar(int y, int height = 24, int buttonWidth = 48)
    {
        _y = y;
        _height = height;
        _buttonWidth = buttonWidth;
    }

    public void SetButtons(IEnumerable<ToolButton> buttons)
    {
        _buttons.Clear();
        _buttons.AddRange(buttons);
    }

    public bool HandleMouse(MouseState mouse, MouseState previous)
    {
        _hoverIndex = -1;
        HoverTip = "";

        if (mouse.Y < _y || mouse.Y >= _y + _height)
            return false;

        var index = mouse.X / _buttonWidth;
        if (index < 0 || index >= _buttons.Count)
            return true;

        _hoverIndex = index;
        HoverTip = _buttons[index].Tip;

        if (mouse.LeftButton == ButtonState.Pressed &&
            previous.LeftButton == ButtonState.Released)
        {
            _buttons[index].Action();
        }

        return true;
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel, int width)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, _y, width, _height),
            CreativeUiTheme.Panel);

        for (var i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            var x = i * _buttonWidth;
            var selected = button.IsSelected?.Invoke() ?? false;
            var hot = i == _hoverIndex;

            if (selected || hot)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x + 1, _y + 1, _buttonWidth - 2, _height - 2),
                    selected ? CreativeUiTheme.MenuHot : CreativeUiTheme.PanelLight);
            }

            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x + 1, _y + _height - 3, _buttonWidth - 2, 2),
                    CreativeUiTheme.Accent);
            }

            var label = button.Label.Length <= 5
                ? button.Label
                : button.Label[..5];

            font.Draw(
                spriteBatch,
                label,
                new Vector2(x + 4, _y + 8),
                selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
        }
    }
}
