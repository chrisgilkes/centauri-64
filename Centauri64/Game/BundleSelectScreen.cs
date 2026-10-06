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
            "1986 CATALOGUE",
            "EST. 1986");

        var bundle = ComputerBundleCatalog.Bundles[_index];
        var page = ContentBounds();
        PrintTheme.Box(spriteBatch, _pixel, page, Color.White);
        PrintTheme.Frame(spriteBatch, _pixel, page, PrintTheme.Rule, 2);

        // Left product column + right art placeholder.
        var left = new Rectangle(page.X + 28, page.Y + 24, 620, page.Height - 48);
        var art = ArtBounds();
        PrintTheme.Box(spriteBatch, _pixel, art, PrintTheme.Paper);
        PrintTheme.Frame(spriteBatch, _pixel, art, PrintTheme.SpotBlue, 2);

        var layout = new PrintLayout(spriteBatch, _pixel, _print, left.X, left.Y, left.Width, left.Bottom - 8);
        layout.Section("THIS MONTH'S PACK");
        layout.Heading(bundle.Name);
        layout.Paragraph(bundle.Tagline, PrintTheme.InkMuted, maxLines: 3);
        layout.Space(8);
        layout.Rule();
        layout.Section("INCLUDED", PrintTheme.SpotBlue);
        layout.BulletList(bundle.Included, PrintTheme.Ink, "* ");
        layout.Space(12);
        layout.Section("WHY CHOOSE THIS PACK?", PrintTheme.Masthead);
        layout.Paragraph(BuildWhyCopy(bundle), PrintTheme.Ink, maxLines: 5);

        DrawArtPlaceholder(spriteBatch, art, bundle);
        DrawPricePlate(spriteBatch, PriceBounds(), bundle.Price);

        DrawTextControl(spriteBatch, PrevBounds(), "<  PREVIOUS PACK", accent: false);
        DrawTextControl(spriteBatch, NextBounds(), "NEXT PACK  >", accent: false);
        DrawTextControl(
            spriteBatch,
            BuyBounds(),
            "ORDER THIS PACK - " + bundle.Price,
            accent: true);

        PrintTheme.Footer(
            spriteBatch,
            _pixel,
            _print,
            "LEFT / RIGHT CHANGE PACK    ENTER ORDER    ESC BACK");
        spriteBatch.End();
    }

    private void DrawArtPlaceholder(SpriteBatch spriteBatch, Rectangle art, ComputerBundle bundle)
    {
        _print.Draw(spriteBatch, _print.Caption, "PRODUCT PHOTOGRAPH", art.X + 24, art.Y + 24, PrintTheme.InkMuted);
        PrintTheme.RuleH(spriteBatch, _pixel, art.X + 24, art.Y + 52, art.Width - 48);

        var titleY = art.Y + art.Height / 2 - 40;
        var nameSize = _print.Measure(_print.Title, "CENTAURI64");
        _print.Draw(
            spriteBatch,
            _print.Title,
            "CENTAURI64",
            art.X + (art.Width - (int)nameSize.X) / 2,
            titleY,
            PrintTheme.Ink);

        foreach (var line in PrintFonts.Wrap(_print.Body, bundle.Name, art.Width - 48).Take(2))
        {
            var size = _print.Measure(_print.Body, line);
            _print.Draw(
                spriteBatch,
                _print.Body,
                line,
                art.X + (art.Width - (int)size.X) / 2,
                titleY + 48,
                PrintTheme.SpotBlue);
            titleY += 28;
        }

        _print.Draw(
            spriteBatch,
            _print.Caption,
            "Pack artwork coming in a later printing.",
            art.X + 24,
            art.Bottom - 40,
            PrintTheme.InkMuted);
    }

    private void DrawPricePlate(SpriteBatch spriteBatch, Rectangle bounds, string price)
    {
        PrintTheme.Box(spriteBatch, _pixel, bounds, PrintTheme.Highlight);
        PrintTheme.Frame(spriteBatch, _pixel, bounds, PrintTheme.Masthead, 2);
        _print.Draw(spriteBatch, _print.Caption, "OUR PRICE", bounds.X + 18, bounds.Y + 12, PrintTheme.Masthead);
        _print.Draw(spriteBatch, _print.Title, price, bounds.X + 18, bounds.Y + 40, PrintTheme.Ink);
    }

    private void DrawTextControl(SpriteBatch spriteBatch, Rectangle bounds, string label, bool accent)
    {
        if (accent)
        {
            PrintTheme.Box(spriteBatch, _pixel, bounds, PrintTheme.Masthead);
            PrintTheme.Frame(spriteBatch, _pixel, bounds, PrintTheme.Ink, 2);
        }
        else
        {
            PrintTheme.Box(spriteBatch, _pixel, bounds, PrintTheme.Paper);
            PrintTheme.Frame(spriteBatch, _pixel, bounds, PrintTheme.Rule, 1);
        }

        var size = _print.Measure(_print.Body, label);
        var x = bounds.X + (bounds.Width - (int)size.X) / 2;
        var y = bounds.Y + (bounds.Height - (int)size.Y) / 2;
        _print.Draw(spriteBatch, _print.Body, label, x, y, accent ? Color.White : PrintTheme.Ink);
    }

    private static string BuildWhyCopy(ComputerBundle bundle)
    {
        if (!string.IsNullOrWhiteSpace(bundle.Tagline))
            return bundle.Tagline + " Everything listed is included in the price shown.";

        return "A complete Centauri64 pack ready for your bedroom desk.";
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
        else if (BuyBounds().Contains(mouse.X, mouse.Y))
            BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);
    }

    private static Rectangle ContentBounds() =>
        new(56, 120, 1168, 580);

    private static Rectangle ArtBounds() =>
        new(740, 150, 450, 420);

    private static Rectangle PriceBounds() =>
        new(740, 588, 450, 88);

    private static Rectangle PrevBounds() =>
        new(56, 720, 280, 48);

    private static Rectangle NextBounds() =>
        new(944, 720, 280, 48);

    private static Rectangle BuyBounds() =>
        new(360, 720, 560, 48);

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
