using System;
using System.Linq;

using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class BundleSelectScreen
{
    private readonly PrintFonts _print;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private int _index;

    public event Action<string>? BundleChosen;
    public event Action? Cancelled;

    public BundleSelectScreen(PrintFonts print, Texture2D pixel)
    {
        _print = print;
        _pixel = pixel;
    }

    public void Open()
    {
        _index = 0;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();
        var count = ComputerBundleCatalog.Bundles.Length;

        if (Pressed(keyboard, Keys.Escape))
        {
            Cancelled?.Invoke();
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, Keys.Left))
            _index = (_index + count - 1) % count;

        if (Pressed(keyboard, Keys.Right))
            _index = (_index + 1) % count;

        HandleMouse(mouse);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch, Matrix transform)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
        PrintTheme.FillPaper(spriteBatch, _pixel);
        PrintTheme.DrawMasthead(
            spriteBatch,
            _pixel,
            _print,
            "CENTAURI COMPUTER CENTRE",
            "AUTUMN 1986 CATALOGUE",
            "EST. 1986");

        var bundle = ComputerBundleCatalog.Bundles[_index];
        var panel = ProductPanel();
        PrintTheme.Box(spriteBatch, _pixel, panel, Color.White);
        PrintTheme.Frame(spriteBatch, _pixel, panel, PrintTheme.SpotBlue, 3);

        PrintTheme.Box(spriteBatch, _pixel, new Rectangle(panel.X, panel.Y, 18, panel.Height), PrintTheme.Masthead);
        PrintTheme.Box(spriteBatch, _pixel, new Rectangle(panel.Right - 18, panel.Y, 18, panel.Height), PrintTheme.SpotBlue);

        _print.Draw(spriteBatch, _print.Caption, "THIS MONTH'S PACK", panel.X + 40, panel.Y + 24, PrintTheme.Masthead);
        _print.Draw(spriteBatch, _print.Title, bundle.Name, panel.X + 40, panel.Y + 52, PrintTheme.Ink);

        var taglineY = panel.Y + 100;
        foreach (var line in PrintFonts.Wrap(_print.Body, bundle.Tagline, panel.Width - 280).Take(2))
        {
            _print.Draw(spriteBatch, _print.Body, line, panel.X + 40, taglineY, PrintTheme.InkMuted);
            taglineY += 28;
        }

        var priceBadge = new Rectangle(panel.Right - 220, panel.Y + 28, 180, 72);
        PrintTheme.Box(spriteBatch, _pixel, priceBadge, PrintTheme.Highlight);
        PrintTheme.Frame(spriteBatch, _pixel, priceBadge, PrintTheme.Masthead, 2);
        _print.Draw(spriteBatch, _print.Caption, "PRICE", priceBadge.X + 16, priceBadge.Y + 8, PrintTheme.Masthead);
        _print.Draw(spriteBatch, _print.Title, bundle.Price, priceBadge.X + 16, priceBadge.Y + 28, PrintTheme.Ink);

        _print.Draw(spriteBatch, _print.Caption, "INCLUDED", panel.X + 40, panel.Y + 160, PrintTheme.SpotBlue);
        var y = panel.Y + 196;
        foreach (var item in bundle.Included)
        {
            _print.Draw(spriteBatch, _print.Body, "*  " + item, panel.X + 48, y, PrintTheme.Ink);
            y += 36;
        }

        DrawButton(spriteBatch, PrevBounds(), "PREVIOUS PACK");
        DrawButton(spriteBatch, NextBounds(), "NEXT PACK");
        DrawButton(spriteBatch, BuyBounds(), "BUY THIS PACK", true);

        PrintTheme.Footer(spriteBatch, _pixel, _print, "LEFT / RIGHT CHANGE PACK    ENTER BUY    ESC BACK");
        spriteBatch.End();
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle bounds, string label, bool accent = false)
    {
        PrintTheme.Box(spriteBatch, _pixel, bounds, accent ? PrintTheme.Masthead : PrintTheme.PaperDark);
        PrintTheme.Frame(spriteBatch, _pixel, bounds, accent ? PrintTheme.Ink : PrintTheme.Rule, 2);
        var size = _print.Measure(_print.Body, label);
        var x = bounds.X + (bounds.Width - (int)size.X) / 2;
        var y = bounds.Y + (bounds.Height - (int)size.Y) / 2;
        _print.Draw(spriteBatch, _print.Body, label, x, y, accent ? Color.White : PrintTheme.Ink);
    }

    private void HandleMouse(MouseState mouse)
    {
        var clicked = mouse.LeftButton == ButtonState.Pressed &&
                      _previousMouse.LeftButton == ButtonState.Released;
        if (!clicked)
            return;

        var count = ComputerBundleCatalog.Bundles.Length;
        if (PrevBounds().Contains(mouse.X, mouse.Y))
            _index = (_index + count - 1) % count;
        else if (NextBounds().Contains(mouse.X, mouse.Y))
            _index = (_index + 1) % count;
        else if (BuyBounds().Contains(mouse.X, mouse.Y) || ProductPanel().Contains(mouse.X, mouse.Y))
            BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);
    }

    private static Rectangle ProductPanel() =>
        new(80, 140, 1120, 560);

    private static Rectangle PrevBounds() =>
        new(80, 724, 280, 52);

    private static Rectangle NextBounds() =>
        new(920, 724, 280, 52);

    private static Rectangle BuyBounds() =>
        new(400, 724, 480, 52);

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
