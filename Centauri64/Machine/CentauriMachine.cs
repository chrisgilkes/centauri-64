using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;

using Centauri64.Machine.Sprites;
using Centauri64.Graphics;
using Centauri64.Basic;

using Centauri64.Machine.Audio;

namespace Centauri64.Machine;

public sealed class CentauriMachine
{
    private readonly TextConsole _console;
    private readonly TextConsole _programConsole;

    private readonly BasicMachine _basic;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 400;

    public const int BORDER_SIZE = 8;

    public const int DEVELOPMENT_WIDTH = SCREEN_WIDTH + BORDER_SIZE * 2;
    public const int DEVELOPMENT_HEIGHT = SCREEN_HEIGHT + BORDER_SIZE * 2;

    public const int ARCADE_WIDTH = 320;
    public const int ARCADE_HEIGHT = 180;

    public const int GAME_WIDTH = ARCADE_WIDTH + BORDER_SIZE * 2;

    public const int GAME_HEIGHT =ARCADE_HEIGHT + BORDER_SIZE * 2;

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

    private readonly CentauriAudio _audio = new();

    private CentauriDisplayMode _displayMode = CentauriDisplayMode.HighResolution;

    public CentauriDisplayMode DisplayMode => _displayMode;

    private sealed class PositionedText
    {
        public int X { get; }
        public int Y { get; }
        public string Text { get; }
        public int Colour { get; }

        public PositionedText(
            int x,
            int y,
            string text,
            int colour)
        {
            X = x;
            Y = y;
            Text = text;
            Colour = colour;
        }
    }

    private readonly List<PositionedText> _positionedText = new();

    private sealed class PlotPoint
    {
        public int X { get; }
        public int Y { get; }
        public int Colour { get; }

        public PlotPoint(int x, int y, int colour)
        {
            X = x;
            Y = y;
            Colour = colour;
        }
    }

    private readonly List<PlotPoint> _plotPoints = new();

    private sealed class LinePrimitive
    {
        public int X1 { get; }
        public int Y1 { get; }
        public int X2 { get; }
        public int Y2 { get; }
        public int Colour { get; }

        public LinePrimitive(
            int x1,
            int y1,
            int x2,
            int y2,
            int colour)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Colour = colour;
        }
    }

    private readonly List<LinePrimitive> _lines = new();

    private sealed class RectPrimitive
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Colour { get; }
        public bool Filled { get; }

        public RectPrimitive(
            int x,
            int y,
            int width,
            int height,
            int colour,
            bool filled)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Colour = colour;
            Filled = filled;
        }
    }

    private readonly List<RectPrimitive> _rectangles = new();


    public CentauriMachine(TextConsole console, TextConsole programConsole)
    {
        _console = console;
        _programConsole = programConsole;

        for (var i = 0; i < MAX_SPRITES; i++)
        {
            _sprites[i] = new CentauriSprite();
        }

        _spriteEditor = new SpriteEditor(_spriteAssets);

        CreateBuiltInSpriteAssets();
    }

    public void SetDisplayMode(int mode)
    {
        if (!Enum.IsDefined(typeof(CentauriDisplayMode), mode))
        {
            throw new InvalidOperationException(
                $"Unsupported display mode {mode}.");
        }

        _displayMode = (CentauriDisplayMode)mode;
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

    public void WriteText(int x, int y, string text)
    {
        _positionedText.Add(
            new PositionedText(
                x,
                y,
                text,
                _programConsole.Foreground));
    }

    public void ClearScreen()
    {
        _programConsole.Clear();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();
    }

    public void SetInk(int colour)
    {
        ValidateColour(colour);

        _programConsole.Foreground = colour;
    }

    public void SetBorder(int colour)
    {
        ValidateColour(colour);

        BorderColour = colour;
    }

    public void SetPaper(int colour)
    {
        ValidateColour(colour);

        _programConsole.Background = colour;
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

    public void HideAllSprites()
    {
        foreach (var sprite in _sprites)
        {
            sprite.Visible = false;
        }
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

    public void DrawText(SpriteBatch spriteBatch,BitmapFont font)
    {
        foreach (var item in _positionedText)
        {
            font.Draw(
                spriteBatch,
                item.Text,
                new Vector2(item.X, item.Y),
                CentauriPalette.Get(item.Colour));
        }
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

    public bool SpritesCollide(int firstIndex, int secondIndex)
    {
        ValidateSpriteIndex(firstIndex);
        ValidateSpriteIndex(secondIndex);

        var first = _sprites[firstIndex];
        var second = _sprites[secondIndex];

        if (!first.Visible || !second.Visible)
            return false;

        return
            first.X < second.X + CentauriSprite.WIDTH &&
            first.X + CentauriSprite.WIDTH > second.X &&
            first.Y < second.Y + CentauriSprite.HEIGHT &&
            first.Y + CentauriSprite.HEIGHT > second.Y;
    }

    public void Beep(int frequency,int durationMs)
    {
        _audio.Beep(
            frequency,
            durationMs);
    }

    public void ResetDisplay()
    {
        _console.Foreground = DEFAULT_INK;
        _console.Background = DEFAULT_PAPER;
        BorderColour = DEFAULT_BORDER;

        _console.Clear();
    }

    public void ResetProgramDisplay()
    {
        _programConsole.Foreground = DEFAULT_INK;
        _programConsole.Background = DEFAULT_PAPER;
        BorderColour = DEFAULT_BORDER;

        _programConsole.Clear();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();

        _displayMode =
            CentauriDisplayMode.HighResolution;
    }

    public void Print(string text)
    {
        _programConsole.WriteLine(text);
    }

    public void Plot(int x, int y, int colour)
    {
        ValidateColour(colour);

        _plotPoints.Add(
            new PlotPoint(x, y, colour));
    }

    public void DrawGraphics(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var point in _plotPoints)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    point.X + BORDER_SIZE,
                    point.Y + BORDER_SIZE,
                    1,
                    1),
                CentauriPalette.Get(point.Colour));
        }

        foreach (var line in _lines)
        {
            DrawLine(
                spriteBatch,
                pixel,
                line.X1,
                line.Y1,
                line.X2,
                line.Y2,
                CentauriPalette.Get(line.Colour));
        }

        foreach (var rect in _rectangles)
        {
            var colour =
                CentauriPalette.Get(rect.Colour);

            if (rect.Filled)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        rect.X + BORDER_SIZE,
                        rect.Y + BORDER_SIZE,
                        rect.Width,
                        rect.Height),
                    colour);
            }
            else
            {
                var right =
                    rect.X + rect.Width - 1;

                var bottom =
                    rect.Y + rect.Height - 1;

                DrawLine(
                    spriteBatch,
                    pixel,
                    rect.X,
                    rect.Y,
                    right,
                    rect.Y,
                    colour);

                DrawLine(
                    spriteBatch,
                    pixel,
                    right,
                    rect.Y,
                    right,
                    bottom,
                    colour);

                DrawLine(
                    spriteBatch,
                    pixel,
                    right,
                    bottom,
                    rect.X,
                    bottom,
                    colour);

                DrawLine(
                    spriteBatch,
                    pixel,
                    rect.X,
                    bottom,
                    rect.X,
                    rect.Y,
                    colour);
            }
        }
    }

    public void Line(int x1,int y1,int x2,int y2,int colour)
    {
        ValidateColour(colour);

        _lines.Add(
            new LinePrimitive(
                x1,
                y1,
                x2,
                y2,
                colour));
    }

    private static void DrawLine(SpriteBatch spriteBatch,Texture2D pixel,int x1,int y1,int x2,int y2,Color colour)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;

        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;

        var error = dx + dy;

        while (true)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    x1 + BORDER_SIZE,
                    y1 + BORDER_SIZE,
                    1,
                    1),
                colour);

            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var error2 = error * 2;

            if (error2 >= dy)
            {
                error += dy;
                x1 += sx;
            }

            if (error2 <= dx)
            {
                error += dx;
                y1 += sy;
            }
        }
    }

    public void Rect(int x,int y,int width,int height,int colour,bool filled)
    {
        ValidateColour(colour);

        _rectangles.Add(
            new RectPrimitive(
                x,
                y,
                width,
                height,
                colour,
                filled));
    }
}