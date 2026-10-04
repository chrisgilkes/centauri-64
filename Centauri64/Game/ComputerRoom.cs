using System;
using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Machine;

namespace Centauri64.Game;

public sealed class ComputerRoom
{
    private const int BannerLength = 74;
    private const double FlipInterval = 0.45;
    private const int FlipsPerTick = 6;
    private const double FlashSeconds = 0.35;

    private readonly BitmapFont _font;
    private readonly Texture2D _whitePixel;
    private readonly Random _random = new();
    private readonly char[] _topBanner;
    private readonly char[] _bottomBanner;
    private readonly double[] _topFlash;
    private readonly double[] _bottomFlash;

    private static readonly Color Background = new(22, 55, 72);
    private static readonly Color Header     = new(36, 72, 110);
    private static readonly Color Cyan       = new(91, 214, 205);
    private static readonly Color Cream      = new(238, 232, 190);
    private static readonly Color Yellow     = new(232, 205, 92);
    private static readonly Color Muted      = new(130, 165, 170);
    private static readonly Color Dark       = new(14, 28, 38);

    private KeyboardState _previousKeyboard;
    private double _flipTimer;

    public event Action? ComputerSelected;

    public event Action? ManualSelected;

    public event Action? SoftwareSelected;

    private bool _computerPoweredOn;

    public bool ComputerPoweredOn => _computerPoweredOn;

    public ComputerRoom(BitmapFont font, Texture2D whitePixel)
    {
        _font       = font;
        _whitePixel = whitePixel;
        _topBanner = CreateBanner();
        _bottomBanner = CreateBanner();
        _topFlash = new double[BannerLength];
        _bottomFlash = new double[BannerLength];
    }

    public void SetComputerPoweredOn()
    {
        _computerPoweredOn = true;
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (Pressed(keyboard, Keys.D1) ||
            Pressed(keyboard, Keys.NumPad1))
        {
            ComputerSelected?.Invoke();
        }

        if (Pressed(keyboard, Keys.D2) ||
            Pressed(keyboard, Keys.NumPad2))
        {
            ManualSelected?.Invoke();
        }

        if (Pressed(keyboard, Keys.D3) ||
            Pressed(keyboard, Keys.NumPad3))
        {
            SoftwareSelected?.Invoke();
        }

        var delta = gameTime.ElapsedGameTime.TotalSeconds;
        _flipTimer += delta;
        AgeFlashes(_topFlash, delta);
        AgeFlashes(_bottomFlash, delta);

        if (_flipTimer >= FlipInterval)
        {
            _flipTimer -= FlipInterval;
            FlipBits(_topBanner, _topFlash);
            FlipBits(_bottomBanner, _bottomFlash);
        }

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        DrawBox(
            spriteBatch,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            Background);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 16, 608, 48),
            Header);

        DrawText(
            spriteBatch,
            "YOUR BEDROOM",
            272,
            24,
            Cream);

        var dayText = DateTime.Now
            .ToString("dddd")
            .ToUpperInvariant();

        DrawText(
            spriteBatch,
            dayText,
            64,
            44,
            Yellow);

        DrawText(
            spriteBatch,
            "CENTAURI64",
            280,
            44,
            Cyan);

        var timeText = DateTime.Now.ToString("h:mm tt").ToUpperInvariant();
        var timeX = 576 - (timeText.Length * 8);

        DrawText(
            spriteBatch,
            timeText,
            timeX,
            44,
            Yellow);

        DrawBanner(
            spriteBatch,
            _topBanner,
            _topFlash,
            24,
            72);

        DrawText(spriteBatch,
            "+-----------------------------+",
            192, 96, Cyan);

        DrawText(spriteBatch,
            "|     **** CENTAURI64 ****    |",
            192, 104, Cyan);

        DrawText(spriteBatch,
            "|                             |",
            192, 112, Cyan);

        var computerStatus = _computerPoweredOn
            ? "|           READY.            |"
            : "|        SWITCHED OFF         |";

        DrawText(
            spriteBatch,
            computerStatus,
            192,
            120,
            _computerPoweredOn ? Cream : Muted);

        DrawText(spriteBatch,
            "|                             |",
            192, 128, Cyan);

        DrawText(spriteBatch,
            "|          TAPE DECK          |",
            192, 136, Cream);

        DrawText(spriteBatch,
            "+-----------------------------+",
            192, 144, Cyan);

        DrawText(spriteBatch, "[1]", 72, 184, Yellow);

        var computerOption = _computerPoweredOn
            ? "USE CENTAURI64"
            : "BOOT UP CENTAURI64";

        DrawText(
            spriteBatch,
            computerOption,
            112,
            184,
            Cream);

        DrawText(spriteBatch, "[2]", 72, 208, Yellow);
        DrawText(spriteBatch, "PROGRAMMING MANUAL", 112, 208, Cream);

        DrawText(spriteBatch, "[3]", 72, 232, Yellow);
        DrawText(spriteBatch, "MY SOFTWARE", 112, 232, Cream);

        DrawText(spriteBatch, "[4]", 72, 256, Yellow);
        DrawText(spriteBatch, "MAGAZINES", 112, 256, Cream);

        DrawText(spriteBatch, "[5]", 72, 280, Yellow);
        DrawText(spriteBatch, "NOTICE BOARD", 112, 280, Cream);

        DrawBanner(
            spriteBatch,
            _bottomBanner,
            _bottomFlash,
            24,
            408);

        DrawBox(
            spriteBatch,
            new Rectangle(16, 424, 608, 24),
            Cyan);

        DrawText(
            spriteBatch,
            "[1]-[5] SELECT",
            48,
            432,
            Dark);

        spriteBatch.End();
    }

    private static char[] CreateBanner()
    {
        var banner = new char[BannerLength];

        for (var i = 0; i < BannerLength; i++)
        {
            banner[i] = (i % 2) == 0 ? '0' : '1';
        }

        return banner;
    }

    private void FlipBits(char[] banner, double[] flashes)
    {
        for (var n = 0; n < FlipsPerTick; n++)
        {
            var index = _random.Next(banner.Length);
            banner[index] = banner[index] == '0' ? '1' : '0';
            flashes[index] = FlashSeconds;
        }
    }

    private static void AgeFlashes(double[] flashes, double delta)
    {
        for (var i = 0; i < flashes.Length; i++)
        {
            if (flashes[i] <= 0)
                continue;

            flashes[i] -= delta;

            if (flashes[i] < 0)
                flashes[i] = 0;
        }
    }

    private void DrawBanner(
        SpriteBatch spriteBatch,
        char[] banner,
        double[] flashes,
        int x,
        int y)
    {
        for (var i = 0; i < banner.Length; i++)
        {
            var colour = flashes[i] > 0 ? Yellow : Cyan;

            DrawText(
                spriteBatch,
                banner[i].ToString(),
                x + (i * 8),
                y,
                colour);
        }
    }

    private void DrawText(
        SpriteBatch spriteBatch,
        string text,
        int x,
        int y,
        Color colour)
    {
        _font.Draw(spriteBatch, text, new Vector2(x, y), colour);
    }

    private void DrawBox(
        SpriteBatch spriteBatch,
        Rectangle rectangle,
        Color colour)
    {
        spriteBatch.Draw(_whitePixel, rectangle, colour);
    }

    private bool Pressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) &&
            !_previousKeyboard.IsKeyDown(key);
    }
}
