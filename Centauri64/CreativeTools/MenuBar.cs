using System;
using System.Collections.Generic;

using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.CreativeTools;

public sealed class MenuItem
{
    public string Label { get; }
    public string Shortcut { get; }
    public Action? Action { get; }
    public Func<bool>? IsEnabled { get; }

    public MenuItem(
        string label,
        Action? action = null,
        string shortcut = "",
        Func<bool>? isEnabled = null)
    {
        Label = label;
        Action = action;
        Shortcut = shortcut;
        IsEnabled = isEnabled;
    }
}

public sealed class MenuDefinition
{
    public string Title { get; }
    public IReadOnlyList<MenuItem> Items { get; }

    public MenuDefinition(string title, params MenuItem[] items)
    {
        Title = title;
        Items = items;
    }
}

public sealed class MenuBar
{
    private readonly List<MenuDefinition> _menus = new();
    private int _openIndex = -1;
    private int _hoverItem = -1;
    private readonly int _height;
    private readonly int _itemWidth;

    public int Height => _height;
    public bool IsOpen => _openIndex >= 0;

    public MenuBar(int height = 16, int itemWidth = 72)
    {
        _height = height;
        _itemWidth = itemWidth;
    }

    public void SetMenus(IEnumerable<MenuDefinition> menus)
    {
        _menus.Clear();
        _menus.AddRange(menus);
        Close();
    }

    public void Close()
    {
        _openIndex = -1;
        _hoverItem = -1;
    }

    public bool HandleMouse(
        MouseState mouse,
        MouseState previous,
        int screenWidth)
    {
        var clicked =
            mouse.LeftButton == ButtonState.Pressed &&
            previous.LeftButton == ButtonState.Released;

        if (_openIndex >= 0)
        {
            var menu = _menus[_openIndex];
            var menuX = _openIndex * _itemWidth;
            var dropRect = new Rectangle(
                menuX,
                _height,
                Math.Max(_itemWidth + 80, 160),
                menu.Items.Count * _height);

            _hoverItem = -1;
            if (dropRect.Contains(mouse.X, mouse.Y))
            {
                _hoverItem = (mouse.Y - _height) / _height;
                if (_hoverItem < 0 || _hoverItem >= menu.Items.Count)
                    _hoverItem = -1;
            }

            if (clicked)
            {
                if (_hoverItem >= 0)
                {
                    var item = menu.Items[_hoverItem];
                    var enabled = item.IsEnabled?.Invoke() ?? true;
                    Close();
                    if (enabled)
                        item.Action?.Invoke();
                    return true;
                }

                if (mouse.Y < _height)
                {
                    var index = mouse.X / _itemWidth;
                    if (index >= 0 && index < _menus.Count)
                    {
                        _openIndex = index == _openIndex ? -1 : index;
                        _hoverItem = -1;
                        return true;
                    }
                }

                Close();
                return true;
            }

            return dropRect.Contains(mouse.X, mouse.Y) || mouse.Y < _height;
        }

        if (clicked && mouse.Y < _height && mouse.X < screenWidth)
        {
            var index = mouse.X / _itemWidth;
            if (index >= 0 && index < _menus.Count)
            {
                _openIndex = index;
                _hoverItem = -1;
                return true;
            }
        }

        return false;
    }

    public bool HandleKeyboard(KeyboardState keyboard, KeyboardState previous)
    {
        if (!IsOpen)
            return false;

        if (WasPressed(keyboard, previous, Keys.Escape))
        {
            Close();
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Up))
        {
            var count = _menus[_openIndex].Items.Count;
            _hoverItem = _hoverItem <= 0 ? count - 1 : _hoverItem - 1;
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Down))
        {
            var count = _menus[_openIndex].Items.Count;
            _hoverItem = (_hoverItem + 1) % count;
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Left))
        {
            _openIndex = (_openIndex - 1 + _menus.Count) % _menus.Count;
            _hoverItem = 0;
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Right))
        {
            _openIndex = (_openIndex + 1) % _menus.Count;
            _hoverItem = 0;
            return true;
        }

        if (WasPressed(keyboard, previous, Keys.Enter) ||
            WasPressed(keyboard, previous, Keys.Space))
        {
            if (_hoverItem < 0)
                _hoverItem = 0;

            var item = _menus[_openIndex].Items[_hoverItem];
            var enabled = item.IsEnabled?.Invoke() ?? true;
            Close();
            if (enabled)
                item.Action?.Invoke();
            return true;
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel, int width)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, width, _height),
            CreativeUiTheme.Panel);

        for (var i = 0; i < _menus.Count; i++)
        {
            var x = i * _itemWidth;
            var selected = i == _openIndex;
            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x, 0, _itemWidth, _height),
                    CreativeUiTheme.MenuHot);
            }

            font.Draw(
                spriteBatch,
                _menus[i].Title,
                new Vector2(x + 6, 4),
                selected ? CreativeUiTheme.Highlight : CreativeUiTheme.Text);
        }

        if (_openIndex < 0)
            return;

        var menu = _menus[_openIndex];
        var menuX = _openIndex * _itemWidth;
        var dropWidth = Math.Max(_itemWidth + 80, 160);
        var dropHeight = menu.Items.Count * _height;

        spriteBatch.Draw(
            pixel,
            new Rectangle(menuX, _height, dropWidth, dropHeight),
            CreativeUiTheme.PanelLight);

        spriteBatch.Draw(
            pixel,
            new Rectangle(menuX, _height, dropWidth, 1),
            CreativeUiTheme.Border);

        for (var i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];
            var y = _height + i * _height;
            var enabled = item.IsEnabled?.Invoke() ?? true;
            var hot = i == _hoverItem;

            if (hot)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(menuX, y, dropWidth, _height),
                    CreativeUiTheme.MenuHot);
            }

            var colour = !enabled
                ? CreativeUiTheme.Muted
                : hot
                    ? CreativeUiTheme.Highlight
                    : CreativeUiTheme.Text;

            font.Draw(spriteBatch, item.Label, new Vector2(menuX + 6, y + 4), colour);

            if (!string.IsNullOrEmpty(item.Shortcut))
            {
                font.Draw(
                    spriteBatch,
                    item.Shortcut,
                    new Vector2(menuX + dropWidth - 8 - item.Shortcut.Length * 8, y + 4),
                    CreativeUiTheme.Muted);
            }
        }
    }

    private static bool WasPressed(
        KeyboardState keyboard,
        KeyboardState previous,
        Keys key)
    {
        return keyboard.IsKeyDown(key) && previous.IsKeyUp(key);
    }
}
