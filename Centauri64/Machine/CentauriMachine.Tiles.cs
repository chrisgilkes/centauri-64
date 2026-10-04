using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Centauri64.Machine.Maps;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    public const int TileSize = CentauriSprite.WIDTH;

    private readonly Dictionary<int, string> _tileDefinitions = new();
    private int[]? _tileMap;
    private int _tileColumns;
    private int _tileRows;

    private int _cameraX;
    private int _cameraY;
    private int? _cameraFollowSprite;

    public int CameraX => _cameraX;

    public int CameraY => _cameraY;

    public void DefineTile(int id, string assetName)
    {
        if (id <= 0)
        {
            throw new InvalidOperationException(
                "Tile id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(assetName))
        {
            throw new InvalidOperationException(
                "Tile name must not be empty.");
        }

        if (!_spriteAssets.Contains(assetName))
        {
            throw new InvalidOperationException(
                $"Unknown sprite {assetName}.");
        }

        _tileDefinitions[id] = assetName;
    }

    public void SetTileMap(int[] cells, int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
        {
            throw new InvalidOperationException(
                "MAP columns and rows must be greater than zero.");
        }

        var expected = columns * rows;

        if (cells.Length < expected)
        {
            throw new InvalidOperationException(
                $"MAP array needs at least {expected} cells.");
        }

        _tileMap = new int[expected];
        Array.Copy(cells, _tileMap, expected);
        _tileColumns = columns;
        _tileRows = rows;
        ClampCamera();
    }

    public void LoadMap(string name)
    {
        var map = _mapAssets.Get(name);

        _tileDefinitions.Clear();

        foreach (var pair in map.Definitions)
        {
            if (!_spriteAssets.Contains(pair.Value))
            {
                throw new InvalidOperationException(
                    $"Unknown sprite {pair.Value}.");
            }

            _tileDefinitions[pair.Key] = pair.Value;
        }

        SetTileMap(map.Cells, map.Columns, map.Rows);
    }

    public void ClearTileMap()
    {
        _tileMap = null;
        _tileColumns = 0;
        _tileRows = 0;
        ClampCamera();
    }

    public void ClearTiles()
    {
        ClearTileMap();
        _tileDefinitions.Clear();
        ClearCamera();
    }

    public void SetCamera(int x, int y)
    {
        _cameraFollowSprite = null;
        _cameraX = x;
        _cameraY = y;
        ClampCamera();
    }

    public void SetCameraFollow(int spriteIndex)
    {
        ValidateSpriteIndex(spriteIndex);
        _cameraFollowSprite = spriteIndex;
        UpdateCamera();
    }

    public void ClearCamera()
    {
        _cameraFollowSprite = null;
        _cameraX = 0;
        _cameraY = 0;
    }

    public void UpdateCamera()
    {
        if (_cameraFollowSprite.HasValue)
        {
            var sprite = _sprites[_cameraFollowSprite.Value];
            _cameraX = sprite.X + (CentauriSprite.WIDTH / 2) - (ScreenWidth / 2);
            _cameraY = sprite.Y + (CentauriSprite.HEIGHT / 2) - (ScreenHeight / 2);
        }

        ClampCamera();
    }

    private void ClampCamera()
    {
        if (_tileMap == null ||
            _tileColumns <= 0 ||
            _tileRows <= 0)
        {
            return;
        }

        var worldWidth = _tileColumns * TileSize;
        var worldHeight = _tileRows * TileSize;
        var maxX = Math.Max(0, worldWidth - ScreenWidth);
        var maxY = Math.Max(0, worldHeight - ScreenHeight);

        if (_cameraX < 0)
            _cameraX = 0;

        if (_cameraY < 0)
            _cameraY = 0;

        if (_cameraX > maxX)
            _cameraX = maxX;

        if (_cameraY > maxY)
            _cameraY = maxY;
    }

    public int TileAt(int pixelX, int pixelY)
    {
        if (_tileMap == null ||
            _tileColumns <= 0 ||
            _tileRows <= 0)
        {
            return 0;
        }

        if (pixelX < 0 ||
            pixelY < 0 ||
            pixelX >= _tileColumns * TileSize ||
            pixelY >= _tileRows * TileSize)
        {
            return 0;
        }

        var column = pixelX / TileSize;
        var row = pixelY / TileSize;
        var index = (row * _tileColumns) + column;

        return _tileMap[index];
    }

    public void DrawTiles(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (_tileMap == null)
            return;

        for (var row = 0; row < _tileRows; row++)
        {
            for (var column = 0; column < _tileColumns; column++)
            {
                var id = _tileMap[(row * _tileColumns) + column];

                if (id == 0)
                    continue;

                if (!_tileDefinitions.TryGetValue(id, out var assetName))
                    continue;

                DrawTile(
                    spriteBatch,
                    pixel,
                    assetName,
                    (column * TileSize) - _cameraX,
                    (row * TileSize) - _cameraY);
            }
        }
    }

    private void DrawTile(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        string assetName,
        int x,
        int y)
    {
        var asset = _spriteAssets.Get(assetName);
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
                    new Rectangle(x + px, y + py, 1, 1),
                    CentauriPalette.Get(colourIndex));
            }
        }
    }
}
