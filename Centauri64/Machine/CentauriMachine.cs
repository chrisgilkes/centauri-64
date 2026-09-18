using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

using Centauri64.Machine.Sprites;
using Centauri64.Graphics;

namespace Centauri64.Machine;

public sealed class CentauriMachine
{
    private readonly TextConsole _console;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 400;

    public const int BORDER_SIZE = 8;

    public const int DEVELOPMENT_WIDTH = SCREEN_WIDTH + BORDER_SIZE * 2;
    public const int DEVELOPMENT_HEIGHT = SCREEN_HEIGHT + BORDER_SIZE * 2;

    public const int GAME_WIDTH = 320;
    public const int GAME_HEIGHT = 180;

    public int BorderColour { get; private set; } = DEFAULT_BORDER;

    public const int MAX_SPRITES = 16;

    private readonly CentauriSprite[] _sprites = new CentauriSprite[MAX_SPRITES];

    public IReadOnlyList<CentauriSprite> Sprites => _sprites;

    private readonly SpriteRenderer _spriteRenderer = new();

    private readonly SpriteAssetStore _spriteAssets = new();

    private readonly SpriteEditor _spriteEditor;

    public SpriteEditor SpriteEditor => _spriteEditor;

    public SpriteAssetStore SpriteAssets => _spriteAssets;

    public const int DEFAULT_INK = 1;
    public const int DEFAULT_PAPER = 6;
    public const int DEFAULT_BORDER = 6;

    public CentauriMachine(TextConsole console)
    {
        _console = console;

        for (var i = 0; i < MAX_SPRITES; i++)
        {
            _sprites[i] = new CentauriSprite();
        }

        _spriteEditor = new SpriteEditor(_spriteAssets);

        CreateBuiltInSpriteAssets();
    }

    public bool IsKeyDown(string keyName)
    {
        var keyboard = Keyboard.GetState();

        return keyName switch
        {
            "LEFT" => keyboard.IsKeyDown(Keys.Left),
            "RIGHT" => keyboard.IsKeyDown(Keys.Right),
            "UP" => keyboard.IsKeyDown(Keys.Up),
            "DOWN" => keyboard.IsKeyDown(Keys.Down),
            "SPACE" => keyboard.IsKeyDown(Keys.Space),

            _ => false
        };
    }

    public void WriteText(int x,int y,string text)
    {
        _console.WriteAt(x, y, text);
    }

    public void ClearScreen()
    {
        _console.Clear();
    }

    public void SetInk(int colour)
    {
        ValidateColour(colour);

        _console.Foreground = colour;
    }

    public void SetBorder(int colour)
    {
        ValidateColour(colour);

        BorderColour = colour;
    }

    public void SetPaper(int colour)
    {
        ValidateColour(colour);

        _console.Background = colour;
    }

    private static void ValidateColour(int colour)
    {
        if (colour < 0 || colour > 15)
        {
            throw new InvalidOperationException(
                "Colour must be between 0 and 15.");
        }
    }

    public void SetSpritePosition(int index,int x,int y)
    {
        ValidateSpriteIndex(index);

        _sprites[index].X = x;
        _sprites[index].Y = y;
    }

    public void ShowSprite(int index)
    {
        ValidateSpriteIndex(index);

        _sprites[index].Visible = true;
    }

    public void HideSprite(int index)
    {
        ValidateSpriteIndex(index);

        _sprites[index].Visible = false;
    }

    private static void ValidateSpriteIndex(int index)
    {
        if (index < 0 || index >= MAX_SPRITES)
        {
            throw new InvalidOperationException(
                $"Sprite must be between 0 and {MAX_SPRITES - 1}.");
        }
    }

    public void UpdateSpriteEditor(MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        _spriteEditor.Update(
            mouse,
            keyboard,
            previousKeyboard);
    }

    public void DrawSpriteEditor(SpriteBatch spriteBatch,BitmapFont font, Texture2D pixel)
    {
        _spriteEditor.Draw(spriteBatch,font, pixel);
    }

   public void DrawSprites(SpriteBatch spriteBatch,Texture2D pixel)
    {
        _spriteRenderer.Draw(
            spriteBatch,
            pixel,
            _sprites);
    }

    private void CreateBuiltInSpriteAssets()
    {
        var player =
            new SpriteAsset("PLAYER");

        var animation =
            player.AddAnimation("DEFAULT");

        var frame =
            animation.AddFrame();

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                if (x == y ||
                    x == CentauriSprite.WIDTH - 1 - y)
                {
                    frame.Pixels[y, x] = 7;
                }
            }
        }

        _spriteAssets.Add(player);
    }

    public void SetSprite(int index,string assetName)
    {
        ValidateSpriteIndex(index);

        var asset =
            _spriteAssets.Get(assetName);

        var animation =
            asset.GetAnimation("DEFAULT");

        if (animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sprite {assetName} has no frames.");
        }

        var frame =
            animation.Frames[0];

        var sprite =
            _sprites[index];

        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                sprite.Pixels[y, x] =
                    frame.Pixels[y, x];
            }
        }

        sprite.Visible = true;
    }

    public void ResetDisplay()
    {
        SetInk(DEFAULT_INK);
        SetPaper(DEFAULT_PAPER);
        SetBorder(DEFAULT_BORDER);

        ClearScreen();
    }
}