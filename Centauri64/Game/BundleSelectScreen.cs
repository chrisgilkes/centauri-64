using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Progression;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

/// <summary>
/// Light-theme computer package selection (career creation).
/// Pixel UI matching the shop screens — no print/catalogue presentation.
/// </summary>
public sealed class BundleSelectScreen
{
    private enum Phase
    {
        Browse,
        Confirm,
        Thanks
    }

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private readonly SystemBinaryBorder _binaryBorder = new();
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private int _index;
    private int _hoverControl = -1; // browse: 0 prev, 1 order, 2 next; confirm: 0 no, 1 yes
    private int _cashPennies = ComputerPurchase.StartingCashPennies;
    private Phase _phase = Phase.Browse;
    private string _thanks = string.Empty;
    private string _error = string.Empty;
    private bool _confirmYes = true;

    public event Action<string>? BundleChosen;
    public event Action? PurchaseAcknowledged;
    public event Action? Cancelled;

    public BundleSelectScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open(int cashPennies = ComputerPurchase.StartingCashPennies)
    {
        _cashPennies = cashPennies;
        _index = 0;
        _hoverControl = -1;
        _phase = Phase.Browse;
        _thanks = string.Empty;
        _error = string.Empty;
        _confirmYes = true;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void ShowThanks(string message)
    {
        _thanks = message;
        _error = string.Empty;
        _phase = Phase.Thanks;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void ShowError(string message)
    {
        _error = message;
        _phase = Phase.Browse;
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        _binaryBorder.Update(gameTime);
        var keyboard = Keyboard.GetState();
        var count = ComputerBundleCatalog.Bundles.Length;

        if (_phase == Phase.Thanks)
        {
            if (Pressed(keyboard, Keys.Enter) ||
                Pressed(keyboard, Keys.Space) ||
                Pressed(keyboard, Keys.Escape) ||
                Clicked(mouse))
            {
                PurchaseAcknowledged?.Invoke();
            }

            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (_phase == Phase.Confirm)
        {
            UpdateConfirm(keyboard, mouse);
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, Keys.Escape))
        {
            Cancelled?.Invoke();
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, Keys.Left))
        {
            _index = (_index + count - 1) % count;
            _error = string.Empty;
        }

        if (Pressed(keyboard, Keys.Right))
        {
            _index = (_index + 1) % count;
            _error = string.Empty;
        }

        HandleBrowseMouse(mouse);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            BeginConfirm();

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(
            spriteBatch,
            _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            ShopUi.Background);

        _binaryBorder.Draw(spriteBatch, _font, ShopUi.BinaryLit, ShopUi.BinaryDim);

        if (_phase == Phase.Thanks)
        {
            DrawThanks(spriteBatch);
            spriteBatch.End();
            return;
        }

        if (_phase == Phase.Confirm)
        {
            DrawConfirm(spriteBatch);
            spriteBatch.End();
            return;
        }

        DrawBrowse(spriteBatch);
        spriteBatch.End();
    }

    private void DrawBrowse(SpriteBatch spriteBatch)
    {
        var bundle = ComputerBundleCatalog.Bundles[_index];
        var count = ComputerBundleCatalog.Bundles.Length;

        spriteBatch.Draw(_pixel, new Rectangle(40, 18, 560, 1), ShopUi.Line);
        BootUi.DrawText(_font, spriteBatch, "CENTAURI COMPUTER CENTRE", 40, 28, ShopUi.Red);
        BootUi.DrawText(_font, spriteBatch, "1986", 568, 28, ShopUi.Blue);
        spriteBatch.Draw(_pixel, new Rectangle(40, 48, 560, 1), ShopUi.Line);

        BootUi.DrawText(_font, spriteBatch, "COMPUTER PACKAGES", 40, 58, ShopUi.Blue);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "Choose a pack for your bedroom machine.",
            40,
            74,
            ShopUi.Muted);

        BootUi.DrawText(
            _font,
            spriteBatch,
            $"PACK {_index + 1}/{count}",
            480,
            74,
            ShopUi.Green);

        var panel = new Rectangle(40, 96, 560, 280);
        BootUi.DrawBox(spriteBatch, _pixel, panel, ShopUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, panel, ShopUi.Amber);

        BootUi.DrawText(_font, spriteBatch, bundle.Name, panel.X + 16, panel.Y + 14, ShopUi.Text);
        BootUi.DrawText(_font, spriteBatch, "SELECTED", panel.Right - 84, panel.Y + 14, ShopUi.Amber);
        BootUi.DrawText(_font, spriteBatch, bundle.Tagline, panel.X + 16, panel.Y + 34, ShopUi.Green);

        spriteBatch.Draw(_pixel, new Rectangle(panel.X + 16, panel.Y + 54, panel.Width - 32, 1), ShopUi.Line);

        BootUi.DrawText(_font, spriteBatch, "INCLUDED:", panel.X + 16, panel.Y + 66, ShopUi.Blue);
        var y = panel.Y + 86;
        foreach (var item in bundle.Included)
        {
            BootUi.DrawText(_font, spriteBatch, "* " + Truncate(item, 60), panel.X + 16, y, ShopUi.Text);
            y += 16;
        }

        BootUi.DrawText(_font, spriteBatch, "PRICE", panel.X + 16, panel.Y + 220, ShopUi.Muted);
        BootUi.DrawText(_font, spriteBatch, bundle.Price, panel.X + 72, panel.Y + 220, ShopUi.Amber);

        var remaining = _cashPennies - bundle.PricePennies;
        BootUi.DrawText(
            _font,
            spriteBatch,
            "REMAINING " + PlayerProgress.FormatPounds(remaining),
            panel.X + 200,
            panel.Y + 220,
            ShopUi.Green);

        BootUi.DrawText(
            _font,
            spriteBatch,
            Truncate(BuildWhyCopy(bundle), 64),
            panel.X + 16,
            panel.Y + 244,
            ShopUi.Muted);

        DrawControl(spriteBatch, PrevBounds(), "< PREV", _hoverControl == 0);
        DrawControl(spriteBatch, OrderBounds(), "ORDER THIS PACK - " + bundle.Price, _hoverControl == 1, accent: true);
        DrawControl(spriteBatch, NextBounds(), "NEXT >", _hoverControl == 2);

        spriteBatch.Draw(_pixel, new Rectangle(40, 420, 560, 1), ShopUi.Line);
        if (!string.IsNullOrEmpty(_error))
        {
            BootUi.DrawText(_font, spriteBatch, Truncate(_error, 68), 40, 436, ShopUi.Red);
        }
        else
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "LEFT / RIGHT CHANGE PACK    ENTER ORDER    ESC BACK",
                40,
                436,
                ShopUi.Muted);
        }
    }

    private void DrawConfirm(SpriteBatch spriteBatch)
    {
        var bundle = ComputerBundleCatalog.Bundles[_index];
        var remaining = _cashPennies - bundle.PricePennies;

        spriteBatch.Draw(_pixel, new Rectangle(40, 18, 560, 1), ShopUi.Line);
        BootUi.DrawText(_font, spriteBatch, "BYTE WORLD COMPUTERS", 40, 28, ShopUi.Red);
        BootUi.DrawText(_font, spriteBatch, "CONFIRM", 520, 28, ShopUi.Blue);
        spriteBatch.Draw(_pixel, new Rectangle(40, 48, 560, 1), ShopUi.Line);

        var panel = new Rectangle(80, 100, 480, 240);
        BootUi.DrawBox(spriteBatch, _pixel, panel, ShopUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, panel, ShopUi.Amber);

        BootUi.DrawText(_font, spriteBatch, bundle.Name, panel.X + 24, panel.Y + 24, ShopUi.Text);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "PACKAGE PRICE: " + PlayerProgress.FormatPounds(bundle.PricePennies),
            panel.X + 24,
            panel.Y + 56,
            ShopUi.Muted);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "YOUR SAVINGS:  " + PlayerProgress.FormatPounds(_cashPennies),
            panel.X + 24,
            panel.Y + 76,
            ShopUi.Muted);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "REMAINING:     " + PlayerProgress.FormatPounds(remaining),
            panel.X + 24,
            panel.Y + 96,
            ShopUi.Amber);

        BootUi.DrawText(_font, spriteBatch, "BUY THIS COMPUTER?", panel.X + 24, panel.Y + 136, ShopUi.Green);

        var noSelected = !_confirmYes || _hoverControl == 0;
        var yesSelected = _confirmYes || _hoverControl == 1;
        DrawControl(spriteBatch, ConfirmNoBounds(), "[NO]", noSelected);
        DrawControl(spriteBatch, ConfirmYesBounds(), "[YES]", yesSelected, accent: _confirmYes);

        BootUi.DrawText(
            _font,
            spriteBatch,
            "LEFT / RIGHT    ENTER CONFIRM    ESC CANCEL",
            40,
            436,
            ShopUi.Muted);
    }

    private void DrawThanks(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, new Rectangle(40, 18, 560, 1), ShopUi.Line);
        BootUi.DrawText(_font, spriteBatch, "BYTE WORLD COMPUTERS", 40, 28, ShopUi.Red);
        BootUi.DrawText(_font, spriteBatch, "SOLD", 560, 28, ShopUi.Green);
        spriteBatch.Draw(_pixel, new Rectangle(40, 48, 560, 1), ShopUi.Line);

        var panel = new Rectangle(80, 120, 480, 200);
        BootUi.DrawBox(spriteBatch, _pixel, panel, ShopUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, panel, ShopUi.Green);

        BootUi.DrawText(_font, spriteBatch, "MARTIN:", panel.X + 24, panel.Y + 32, ShopUi.Blue);
        BootUi.DrawText(
            _font,
            spriteBatch,
            Truncate(_thanks, 50),
            panel.X + 24,
            panel.Y + 64,
            ShopUi.Text);

        if (_thanks.Length > 50)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                Truncate(_thanks[50..].TrimStart(), 50),
                panel.X + 24,
                panel.Y + 80,
                ShopUi.Text);
        }

        BootUi.DrawText(
            _font,
            spriteBatch,
            "TIME TO GET THAT MACHINE HOME.",
            panel.X + 24,
            panel.Y + 120,
            ShopUi.Muted);

        BootUi.DrawText(
            _font,
            spriteBatch,
            "ENTER / SPACE CONTINUE",
            40,
            436,
            ShopUi.Muted);
    }

    private void UpdateConfirm(KeyboardState keyboard, MouseState mouse)
    {
        if (Pressed(keyboard, Keys.Escape) || Pressed(keyboard, Keys.N))
        {
            _phase = Phase.Browse;
            return;
        }

        if (Pressed(keyboard, Keys.Left) || Pressed(keyboard, Keys.Right))
            _confirmYes = !_confirmYes;

        _hoverControl = -1;
        if (ConfirmNoBounds().Contains(mouse.X, mouse.Y))
            _hoverControl = 0;
        else if (ConfirmYesBounds().Contains(mouse.X, mouse.Y))
            _hoverControl = 1;

        if (Clicked(mouse))
        {
            if (_hoverControl == 0)
            {
                _phase = Phase.Browse;
                return;
            }

            if (_hoverControl == 1)
            {
                ConfirmPurchase();
                return;
            }
        }

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space) || Pressed(keyboard, Keys.Y))
        {
            if (_confirmYes)
                ConfirmPurchase();
            else
                _phase = Phase.Browse;
        }
    }

    private void BeginConfirm()
    {
        _confirmYes = true;
        _error = string.Empty;
        _phase = Phase.Confirm;
    }

    private void ConfirmPurchase() =>
        BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);

    private void DrawControl(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        string label,
        bool hovered,
        bool accent = false)
    {
        var fill = accent || hovered ? ShopUi.Highlight : ShopUi.Panel;
        var border = accent ? ShopUi.Amber : hovered ? ShopUi.Green : ShopUi.Line;
        BootUi.DrawBox(spriteBatch, _pixel, bounds, fill);
        BootUi.DrawBorder(spriteBatch, _pixel, bounds, border);

        var textColour = accent ? ShopUi.Amber : ShopUi.Text;
        var textWidth = label.Length * 8;
        var x = bounds.X + Math.Max(4, (bounds.Width - textWidth) / 2);
        var y = bounds.Y + (bounds.Height - 8) / 2;
        BootUi.DrawText(_font, spriteBatch, Truncate(label, bounds.Width / 8 - 1), x, y, textColour);
    }

    private static string BuildWhyCopy(ComputerBundle bundle)
    {
        if (!string.IsNullOrWhiteSpace(bundle.Tagline))
            return bundle.Tagline + " Everything listed is included.";

        return "A complete Centauri64 pack ready for your bedroom desk.";
    }

    private void HandleBrowseMouse(MouseState mouse)
    {
        _hoverControl = -1;
        if (PrevBounds().Contains(mouse.X, mouse.Y))
            _hoverControl = 0;
        else if (OrderBounds().Contains(mouse.X, mouse.Y))
            _hoverControl = 1;
        else if (NextBounds().Contains(mouse.X, mouse.Y))
            _hoverControl = 2;

        if (!Clicked(mouse))
            return;

        var count = ComputerBundleCatalog.Bundles.Length;
        if (_hoverControl == 0)
        {
            _index = (_index + count - 1) % count;
            _error = string.Empty;
        }
        else if (_hoverControl == 2)
        {
            _index = (_index + 1) % count;
            _error = string.Empty;
        }
        else if (_hoverControl == 1)
        {
            BeginConfirm();
        }
    }

    private bool Clicked(MouseState mouse) =>
        mouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released;

    private static Rectangle PrevBounds() => new(40, 388, 96, 24);

    private static Rectangle OrderBounds() => new(152, 388, 336, 24);

    private static Rectangle NextBounds() => new(504, 388, 96, 24);

    private static Rectangle ConfirmNoBounds() => new(160, 280, 120, 28);

    private static Rectangle ConfirmYesBounds() => new(360, 280, 120, 28);

    private static string Truncate(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;

        return maxChars <= 1 ? text[..1] : text[..(maxChars - 1)] + ".";
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
