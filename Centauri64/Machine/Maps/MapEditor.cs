using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Machine.Sprites;

namespace Centauri64.Machine.Maps;

public sealed class MapEditor
{
    private enum ConfirmKind
    {
        None,
        DeleteMap
    }

    private const int GridX = 16;
    private const int GridY = 40;
    private const int ViewColumns = 18;
    private const int ViewRows = 12;
    private const int CellScale = 2;
    private const int DefaultColumns = 40;
    private const int DefaultRows = 15;
    private const int PanelX = 440;
    private const int MaxUndo = 32;

    private readonly MapAssetStore _maps;
    private readonly SpriteAssetStore _sprites;

    private MapAsset? _map;
    private int _currentIndex;
    private int _panX;
    private int _panY;
    private int _selectedTileId = 1;
    private int _spritePaletteIndex;
    private bool _painting;
    private bool _dirty;
    private bool _enteringName;
    private bool _renaming;
    private string _nameBuffer = "";
    private string _message = "";
    private ConfirmKind _confirm;
    private readonly List<int[]> _undo = new();

    public bool IsActive { get; private set; }

    public event Action<string>? Notice;

    public MapEditor(MapAssetStore maps, SpriteAssetStore sprites)
    {
        _maps = maps;
        _sprites = sprites;
    }

    public void Open()
    {
        IsActive = true;
        _message = "";
        _confirm = ConfirmKind.None;
        _enteringName = false;
        _renaming = false;
        _painting = false;
        _undo.Clear();

        var maps = _maps.Maps;

        if (maps.Count == 0)
        {
            _map = null;
            _currentIndex = 0;
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= maps.Count)
            _currentIndex = 0;

        SelectMap(_currentIndex);
    }

    public void Close()
    {
        if (_dirty)
            Notice?.Invoke("SAVE TO KEEP MAPS");

        IsActive = false;
        _painting = false;
        _confirm = ConfirmKind.None;
        _map = null;
    }

    public void MarkSaved()
    {
        _dirty = false;
    }

    public void Update(
        GameTime gameTime,
        MouseState mouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (!IsActive)
            return;

        if (_enteringName)
        {
            UpdateNameEntry(keyboard, previousKeyboard);
            return;
        }

        if (_confirm != ConfirmKind.None)
        {
            UpdateConfirm(keyboard, previousKeyboard);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            Close();
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.N))
        {
            _enteringName = true;
            _renaming = false;
            _nameBuffer = "";
            _message = "";
            return;
        }

        if (_map == null)
            return;

        if (Pressed(keyboard, previousKeyboard, Keys.R))
        {
            _enteringName = true;
            _renaming = true;
            _nameBuffer = _map.Name;
            _message = "";
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.X))
        {
            _confirm = ConfirmKind.DeleteMap;
            return;
        }

        if (((keyboard.IsKeyDown(Keys.LeftControl) ||
              keyboard.IsKeyDown(Keys.RightControl)) &&
             Pressed(keyboard, previousKeyboard, Keys.Z)) ||
            Pressed(keyboard, previousKeyboard, Keys.U))
        {
            Undo();
            return;
        }

        var shift =
            keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift);
        var panStep = shift ? 4 : 1;

        if (Pressed(keyboard, previousKeyboard, Keys.Left))
        {
            if (shift)
                _panX = Math.Max(0, _panX - panStep);
            else
                SelectPreviousMap();

            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Right))
        {
            if (shift)
                _panX = Math.Min(MaxPanX(), _panX + panStep);
            else
                SelectNextMap();

            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Up))
        {
            _panY = Math.Max(0, _panY - panStep);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Down))
        {
            _panY = Math.Min(MaxPanY(), _panY + panStep);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.OemOpenBrackets))
        {
            CycleSprite(-1);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.OemCloseBrackets))
        {
            CycleSprite(1);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D1) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad1))
        {
            BindSelectedSprite(1);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D2) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad2))
        {
            BindSelectedSprite(2);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D3) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad3))
        {
            BindSelectedSprite(3);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D4) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad4))
        {
            BindSelectedSprite(4);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D5) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad5))
        {
            BindSelectedSprite(5);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D6) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad6))
        {
            BindSelectedSprite(6);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D7) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad7))
        {
            BindSelectedSprite(7);
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.D8) ||
            Pressed(keyboard, previousKeyboard, Keys.NumPad8))
        {
            BindSelectedSprite(8);
            return;
        }

        if (mouse.LeftButton != ButtonState.Pressed &&
            mouse.RightButton != ButtonState.Pressed)
        {
            _painting = false;
        }

        if (TrySelectDefinition(mouse))
            return;

        var cellSize = CentauriSprite.WIDTH * CellScale;
        var viewWidth = ViewColumns * cellSize;
        var viewHeight = ViewRows * cellSize;

        if (mouse.X < GridX ||
            mouse.X >= GridX + viewWidth ||
            mouse.Y < GridY ||
            mouse.Y >= GridY + viewHeight)
        {
            return;
        }

        var column = _panX + ((mouse.X - GridX) / cellSize);
        var row = _panY + ((mouse.Y - GridY) / cellSize);

        if (column < 0 || column >= _map.Columns ||
            row < 0 || row >= _map.Rows)
        {
            return;
        }

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            BeginStroke();
            _map.SetCell(column, row, _selectedTileId);
        }

        if (mouse.RightButton == ButtonState.Pressed)
        {
            BeginStroke();
            _map.SetCell(column, row, 0);
        }
    }

    public void Draw(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        if (!IsActive)
            return;

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, 640, 480),
            Color.Black);

        font.Draw(
            spriteBatch,
            "MAP EDITOR",
            new Vector2(PanelX, 20),
            Color.White);

        DrawGrid(spriteBatch, pixel);
        DrawPanel(spriteBatch, font, pixel);
    }

    private void DrawGrid(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var cellSize = CentauriSprite.WIDTH * CellScale;
        var viewWidth = ViewColumns * cellSize;
        var viewHeight = ViewRows * cellSize;

        spriteBatch.Draw(
            pixel,
            new Rectangle(GridX, GridY, viewWidth, viewHeight),
            new Color(24, 24, 24));

        if (_map == null)
            return;

        for (var viewRow = 0; viewRow < ViewRows; viewRow++)
        {
            var row = _panY + viewRow;

            if (row >= _map.Rows)
                break;

            for (var viewColumn = 0; viewColumn < ViewColumns; viewColumn++)
            {
                var column = _panX + viewColumn;

                if (column >= _map.Columns)
                    break;

                var id = _map.GetCell(column, row);
                var x = GridX + (viewColumn * cellSize);
                var y = GridY + (viewRow * cellSize);

                if (id == 0)
                {
                    spriteBatch.Draw(
                        pixel,
                        new Rectangle(x, y, cellSize, cellSize),
                        new Color(32, 32, 32));
                    continue;
                }

                if (!_map.Definitions.TryGetValue(id, out var assetName) ||
                    !_sprites.Contains(assetName))
                {
                    spriteBatch.Draw(
                        pixel,
                        new Rectangle(x, y, cellSize, cellSize),
                        Color.DarkRed);
                    continue;
                }

                DrawTilePreview(
                    spriteBatch,
                    pixel,
                    assetName,
                    x,
                    y,
                    CellScale);
            }
        }

        for (var x = 0; x <= ViewColumns; x++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(GridX + (x * cellSize), GridY, 1, viewHeight),
                Color.Gray);
        }

        for (var y = 0; y <= ViewRows; y++)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(GridX, GridY + (y * cellSize), viewWidth, 1),
                Color.Gray);
        }
    }

    private void DrawPanel(SpriteBatch spriteBatch, BitmapFont font, Texture2D pixel)
    {
        if (_enteringName)
        {
            font.Draw(
                spriteBatch,
                _renaming ? "RENAME MAP:" : "NEW MAP:",
                new Vector2(PanelX, 60),
                Color.White);

            font.Draw(
                spriteBatch,
                _nameBuffer + "_",
                new Vector2(PanelX, 80),
                Color.White);

            font.Draw(
                spriteBatch,
                "ENTER OK  ESC CANCEL",
                new Vector2(PanelX, 110),
                Color.White);

            return;
        }

        if (_confirm != ConfirmKind.None)
        {
            font.Draw(
                spriteBatch,
                "DELETE MAP?",
                new Vector2(PanelX, 60),
                Color.Yellow);

            font.Draw(
                spriteBatch,
                "Y YES    N NO",
                new Vector2(PanelX, 80),
                Color.White);

            return;
        }

        if (_map == null)
        {
            font.Draw(
                spriteBatch,
                "NO MAPS",
                new Vector2(PanelX, 60),
                Color.White);

            font.Draw(
                spriteBatch,
                "N NEW MAP",
                new Vector2(PanelX, 90),
                Color.White);

            font.Draw(
                spriteBatch,
                "ESC EXIT",
                new Vector2(PanelX, 110),
                Color.White);

            return;
        }

        font.Draw(
            spriteBatch,
            $"MAP: {_map.Name}",
            new Vector2(PanelX, 50),
            Color.White);

        font.Draw(
            spriteBatch,
            $"SIZE: {_map.Columns}x{_map.Rows}",
            new Vector2(PanelX, 66),
            Color.White);

        font.Draw(
            spriteBatch,
            $"PAN: {_panX},{_panY}",
            new Vector2(PanelX, 82),
            Color.Gray);

        font.Draw(
            spriteBatch,
            $"TILE: {_selectedTileId}",
            new Vector2(PanelX, 110),
            Color.Yellow);

        DrawDefinitions(spriteBatch, font, pixel);

        var assets = _sprites.Assets;

        if (assets.Count > 0)
        {
            var sprite = assets[_spritePaletteIndex];

            font.Draw(
                spriteBatch,
                $"[ ] {sprite.Name}",
                new Vector2(PanelX, 280),
                Color.White);

            font.Draw(
                spriteBatch,
                "1-8 BIND ID",
                new Vector2(PanelX, 296),
                Color.Gray);

            DrawTilePreview(
                spriteBatch,
                pixel,
                sprite.Name,
                PanelX,
                318,
                4);
        }

        if (!string.IsNullOrEmpty(_message))
        {
            font.Draw(
                spriteBatch,
                _message,
                new Vector2(PanelX, 390),
                Color.Yellow);
        }

        if (_dirty)
        {
            font.Draw(
                spriteBatch,
                "SAVE TAPE TO KEEP MAPS",
                new Vector2(PanelX, 410),
                Color.Yellow);
        }

        font.Draw(
            spriteBatch,
            "N NEW  R RENAME  X DEL",
            new Vector2(PanelX, 440),
            Color.White);

        font.Draw(
            spriteBatch,
            "ARROWS PAN/MAP  ESC",
            new Vector2(PanelX, 456),
            Color.White);
    }

    private void DrawDefinitions(
        SpriteBatch spriteBatch,
        BitmapFont font,
        Texture2D pixel)
    {
        var y = 130;

        font.Draw(
            spriteBatch,
            "DEFS",
            new Vector2(PanelX, y),
            Color.White);

        y += 16;

        foreach (var pair in _map!.Definitions)
        {
            var colour = pair.Key == _selectedTileId
                ? Color.Yellow
                : Color.White;

            font.Draw(
                spriteBatch,
                $"{pair.Key}:{pair.Value}",
                new Vector2(PanelX, y),
                colour);

            if (_sprites.Contains(pair.Value))
            {
                DrawTilePreview(
                    spriteBatch,
                    pixel,
                    pair.Value,
                    PanelX + 120,
                    y - 2,
                    1);
            }

            y += 18;

            if (y > 260)
                break;
        }
    }

    private void DrawTilePreview(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        string assetName,
        int x,
        int y,
        int scale)
    {
        var asset = _sprites.Get(assetName);
        var animation = asset.GetAnimation("DEFAULT");

        if (animation.Frames.Count == 0)
            return;

        var frame = animation.Frames[0];

        for (var py = 0; py < CentauriSprite.HEIGHT; py++)
        {
            for (var px = 0; px < CentauriSprite.WIDTH; px++)
            {
                var colourIndex = frame.Pixels[py, px];

                if (colourIndex == CentauriSprite.TRANSPARENT)
                    continue;

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        x + (px * scale),
                        y + (py * scale),
                        scale,
                        scale),
                    CentauriPalette.Get(colourIndex));
            }
        }
    }

    private bool TrySelectDefinition(MouseState mouse)
    {
        if (_map == null || mouse.LeftButton != ButtonState.Pressed)
            return false;

        var y = 146;

        foreach (var pair in _map.Definitions)
        {
            if (mouse.X >= PanelX &&
                mouse.X < PanelX + 160 &&
                mouse.Y >= y &&
                mouse.Y < y + 16)
            {
                _selectedTileId = pair.Key;
                return true;
            }

            y += 18;

            if (y > 260)
                break;
        }

        return false;
    }

    private void SelectMap(int index)
    {
        var maps = _maps.Maps;

        if (index < 0 || index >= maps.Count)
            return;

        _currentIndex = index;
        _map = maps[index];
        _panX = 0;
        _panY = 0;
        _undo.Clear();

        if (_map.Definitions.Count > 0)
        {
            foreach (var id in _map.Definitions.Keys)
            {
                _selectedTileId = id;
                break;
            }
        }
    }

    private void SelectPreviousMap()
    {
        var maps = _maps.Maps;

        if (maps.Count == 0)
            return;

        var index = _currentIndex - 1;

        if (index < 0)
            index = maps.Count - 1;

        SelectMap(index);
    }

    private void SelectNextMap()
    {
        var maps = _maps.Maps;

        if (maps.Count == 0)
            return;

        var index = _currentIndex + 1;

        if (index >= maps.Count)
            index = 0;

        SelectMap(index);
    }

    private void CycleSprite(int direction)
    {
        var assets = _sprites.Assets;

        if (assets.Count == 0)
            return;

        _spritePaletteIndex =
            (_spritePaletteIndex + direction + assets.Count) % assets.Count;
    }

    private void BindSelectedSprite(int id)
    {
        if (_map == null)
            return;

        var assets = _sprites.Assets;

        if (assets.Count == 0)
        {
            _message = "NO SPRITES";
            return;
        }

        _map.Definitions[id] = assets[_spritePaletteIndex].Name;
        _selectedTileId = id;
        _dirty = true;
        _message = $"TILE {id}={assets[_spritePaletteIndex].Name}";
    }

    private void UpdateNameEntry(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            _enteringName = false;
            _renaming = false;
            _nameBuffer = "";
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Back) &&
            _nameBuffer.Length > 0)
        {
            _nameBuffer = _nameBuffer[..^1];
            return;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Enter))
        {
            CommitName();
            return;
        }

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!previousKeyboard.IsKeyUp(key))
                continue;

            if (key >= Keys.A && key <= Keys.Z)
            {
                if (_nameBuffer.Length < 12)
                    _nameBuffer += (char)('A' + (key - Keys.A));

                return;
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (_nameBuffer.Length < 12)
                    _nameBuffer += (char)('0' + (key - Keys.D0));

                return;
            }
        }
    }

    private void CommitName()
    {
        if (string.IsNullOrWhiteSpace(_nameBuffer))
            return;

        var name = _nameBuffer.ToUpperInvariant();

        if (_renaming)
        {
            if (_map == null)
                return;

            if (_map.Name != name && _maps.Contains(name))
            {
                _message = "NAME TAKEN";
                return;
            }

            _maps.Rename(_map.Name, name);
            _dirty = true;
            _enteringName = false;
            _renaming = false;
            _nameBuffer = "";
            SelectMapByName(name);
            return;
        }

        if (_maps.Contains(name))
        {
            _message = "NAME TAKEN";
            return;
        }

        var map = new MapAsset(name, DefaultColumns, DefaultRows);
        _maps.Add(map);
        _dirty = true;
        _enteringName = false;
        _nameBuffer = "";
        SelectMapByName(name);
    }

    private void SelectMapByName(string name)
    {
        var maps = _maps.Maps;

        for (var i = 0; i < maps.Count; i++)
        {
            if (maps[i].Name == name)
            {
                SelectMap(i);
                return;
            }
        }
    }

    private void UpdateConfirm(
        KeyboardState keyboard,
        KeyboardState previousKeyboard)
    {
        if (Pressed(keyboard, previousKeyboard, Keys.Escape) ||
            Pressed(keyboard, previousKeyboard, Keys.N))
        {
            _confirm = ConfirmKind.None;
            return;
        }

        if (!Pressed(keyboard, previousKeyboard, Keys.Y))
            return;

        _confirm = ConfirmKind.None;

        if (_map == null)
            return;

        _maps.Remove(_map.Name);
        _dirty = true;
        _undo.Clear();

        var maps = _maps.Maps;

        if (maps.Count == 0)
        {
            _map = null;
            _currentIndex = 0;
            return;
        }

        if (_currentIndex >= maps.Count)
            _currentIndex = maps.Count - 1;

        SelectMap(_currentIndex);
    }

    private void BeginStroke()
    {
        if (_painting || _map == null)
            return;

        PushUndo();
        _painting = true;
        _dirty = true;
    }

    private void PushUndo()
    {
        if (_map == null)
            return;

        var copy = new int[_map.Cells.Length];
        Array.Copy(_map.Cells, copy, copy.Length);
        _undo.Add(copy);

        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);
    }

    private void Undo()
    {
        if (_map == null || _undo.Count == 0)
        {
            _message = "NOTHING TO UNDO";
            return;
        }

        var cells = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Array.Copy(cells, _map.Cells, cells.Length);
        _dirty = true;
        _message = "";
    }

    private int MaxPanX()
    {
        if (_map == null)
            return 0;

        return Math.Max(0, _map.Columns - ViewColumns);
    }

    private int MaxPanY()
    {
        if (_map == null)
            return 0;

        return Math.Max(0, _map.Rows - ViewRows);
    }

    private static bool Pressed(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key)
    {
        return keyboard.IsKeyDown(key) &&
            previousKeyboard.IsKeyUp(key);
    }
}
