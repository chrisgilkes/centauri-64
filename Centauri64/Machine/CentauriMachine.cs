using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Centauri64.Machine;

public sealed class CentauriMachine
{
    private readonly TextConsole _console;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 400;

    public const int BORDER_SIZE = 8;

    public const int DISPLAY_WIDTH =
        SCREEN_WIDTH + BORDER_SIZE * 2;

    public const int DISPLAY_HEIGHT =
        SCREEN_HEIGHT + BORDER_SIZE * 2;

    public int BorderColour { get; private set; } = 6;

    public const int MAX_SPRITES = 16;

    private readonly CentauriSprite[] _sprites = new CentauriSprite[MAX_SPRITES];

    public IReadOnlyList<CentauriSprite> Sprites => _sprites;

    private readonly SpriteRenderer _spriteRenderer = new();

    public CentauriMachine(TextConsole console)
    {
        _console = console;

        for (var i = 0; i < MAX_SPRITES; i++)
        {
            _sprites[i] = new CentauriSprite();
        }
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

    public void CreateTestSprite()
    {
        var sprite = _sprites[0];

        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                if (x == y ||
                    x == CentauriSprite.WIDTH - 1 - y)
                {
                    sprite.Pixels[y, x] = 7;
                }
            }
        }

        sprite.X = 100;
        sprite.Y = 100;
        sprite.Visible = true;
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

   public void DrawSprites(SpriteBatch spriteBatch,Texture2D pixel)
    {
        _spriteRenderer.Draw(
            spriteBatch,
            pixel,
            _sprites);
    }
}