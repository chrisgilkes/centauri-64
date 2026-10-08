using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Progression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

/// <summary>
/// Light-theme Byte World shop hub shown during career package purchase.
/// Only implemented departments are selectable; others appear disabled.
/// </summary>
public sealed class ComputerShopScreen
{
    private readonly struct MenuEntry
    {
        public string Label { get; }
        public bool Available { get; }

        public MenuEntry(string label, bool available)
        {
            Label = label;
            Available = available;
        }
    }

    private static readonly MenuEntry[] Entries =
    {
        new("[1] COMPUTERS AND PERIPHERALS", true),
        new("[2] BUYING AND SELLING GAMES", false),
        new("[3] BOOKS AND MAGAZINES", false),
        new("[4] BARGAIN BIN", false),
        new("[5] TALK TO MARTIN", false),
        new("[6] TRY THE DISPLAY MACHINES", false),
        new("[7] NOTICEBOARD", false),
        new("[8] THIS WEEK'S CHARTS", false)
    };

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private readonly SystemBinaryBorder _binaryBorder = new();
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private int _selected;
    private int _hover = -1;
    private int _cashPennies;
    private bool _hasComputer;
    private string _status = string.Empty;

    public event Action? ComputersSelected;
    public event Action? Cancelled;

    public ComputerShopScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open(int cashPennies, bool hasComputer = false)
    {
        _cashPennies = cashPennies;
        _hasComputer = hasComputer;
        _selected = 0;
        _hover = -1;
        _status = string.Empty;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        _binaryBorder.Update(gameTime);
        var keyboard = Keyboard.GetState();

        if (Pressed(keyboard, Keys.Escape))
        {
            Cancelled?.Invoke();
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        HandleMouse(mouse);

        if (Pressed(keyboard, Keys.Up))
            Move(-1);

        if (Pressed(keyboard, Keys.Down))
            Move(1);

        for (var i = 0; i < Entries.Length && i < 8; i++)
        {
            if (Pressed(keyboard, Keys.D1 + i) || Pressed(keyboard, Keys.NumPad1 + i))
            {
                _selected = i;
                Activate();
                break;
            }
        }

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            Activate();

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

        spriteBatch.Draw(_pixel, new Rectangle(40, 18, 560, 1), ShopUi.Line);
        BootUi.DrawText(_font, spriteBatch, "BYTE WORLD COMPUTERS", 40, 28, ShopUi.Red);
        BootUi.DrawText(_font, spriteBatch, "EST. 1984", 520, 28, ShopUi.Blue);
        spriteBatch.Draw(_pixel, new Rectangle(40, 48, 560, 1), ShopUi.Line);

        BootUi.DrawText(
            _font,
            spriteBatch,
            "COMPUTERS * SOFTWARE * ELECTRONICS",
            40,
            58,
            ShopUi.Blue);

        DrawMartinIntro(spriteBatch);

        var cash = PlayerProgress.FormatPounds(_cashPennies);
        BootUi.DrawText(_font, spriteBatch, "YOUR CASH", 40, 128, ShopUi.Muted);
        BootUi.DrawText(_font, spriteBatch, cash, 140, 128, ShopUi.Amber);

        var panel = new Rectangle(40, 148, 560, 232);
        BootUi.DrawBox(spriteBatch, _pixel, panel, ShopUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, panel, ShopUi.Line);

        BootUi.DrawText(_font, spriteBatch, "SHOP MENU", panel.X + 12, panel.Y + 10, ShopUi.Green);

        for (var i = 0; i < Entries.Length; i++)
        {
            var entry = Entries[i];
            var available = IsAvailable(i);
            var y = panel.Y + 32 + i * 22;
            var selected = i == _selected;

            if (selected)
            {
                spriteBatch.Draw(
                    _pixel,
                    new Rectangle(panel.X + 8, y - 2, panel.Width - 16, 18),
                    ShopUi.Highlight);
            }

            Color colour;
            if (!available)
                colour = ShopUi.Muted;
            else if (selected)
                colour = ShopUi.Amber;
            else
                colour = ShopUi.Green;

            var marker = selected ? ">" : " ";
            BootUi.DrawText(_font, spriteBatch, marker + " " + entry.Label, panel.X + 12, y, colour);
        }

        if (!string.IsNullOrEmpty(_status))
        {
            var lines = WrapStatus(_status, 68);
            BootUi.DrawText(_font, spriteBatch, lines[0], 40, 386, ShopUi.Red);
            if (lines.Length > 1)
                BootUi.DrawText(_font, spriteBatch, lines[1], 40, 402, ShopUi.Red);
        }

        spriteBatch.Draw(_pixel, new Rectangle(40, 420, 560, 1), ShopUi.Line);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "ARROWS SELECT    1-8 JUMP    ENTER OPEN    ESC BACK",
            40,
            436,
            ShopUi.Muted);

        spriteBatch.End();
    }

    private void DrawMartinIntro(SpriteBatch spriteBatch)
    {
        if (_hasComputer)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "Martin nods at your Centauri64 bag.",
                40,
                80,
                ShopUi.Text);
            BootUi.DrawText(
                _font,
                spriteBatch,
                "\"Back already? What can I get you?\"",
                40,
                96,
                ShopUi.Muted);
            return;
        }

        BootUi.DrawText(
            _font,
            spriteBatch,
            "Martin looks up from a stack of Centauri64 boxes.",
            40,
            74,
            ShopUi.Text);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "\"Three hundred quid, eh? You've been saving.\"",
            40,
            90,
            ShopUi.Muted);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "\"Three Centauri64 packs. Take your pick — leave money for games!\"",
            40,
            106,
            ShopUi.Muted);
    }

    private bool IsAvailable(int index)
    {
        if (index == 0)
            return !_hasComputer; // After purchase, cannot buy another starter pack here.

        return Entries[index].Available;
    }

    private string LockedMessage(int index)
    {
        if (!_hasComputer)
        {
            return index switch
            {
                0 => string.Empty,
                1 => "GET YOUR COMPUTER HOME FIRST. THEN WE'LL TALK GAMES.",
                2 => "FIRST THINGS FIRST. YOU'LL NEED A COMPUTER TO READ THOSE LISTINGS!",
                3 => "ONE THING AT A TIME! LET'S GET YOU SORTED WITH A MACHINE.",
                4 => "LET ME GET YOU SET UP WITH A COMPUTER FIRST.",
                5 => "YOU CAN TRY THOSE LATER. LET'S FIND YOU A MACHINE OF YOUR OWN.",
                6 => "NOTHING FOR YOU ON THERE JUST YET.",
                7 => "GET YOUR COMPUTER SORTED, THEN I'LL SHOW YOU WHAT'S SELLING.",
                _ => "COME BACK WHEN YOU'VE GOT YOUR MACHINE."
            };
        }

        // Post-purchase: departments still unimplemented — honest progression hints.
        return index switch
        {
            0 => "YOU ALREADY BOUGHT YOUR CENTAURI64.",
            1 => "GET A FEW GAMES UNDER YOUR BELT. THEN WE'LL TALK TRADING.",
            2 => "CHECK YOUR MAGAZINES AT HOME — NEW ISSUES WILL APPEAR THERE.",
            3 => "I'M STILL SORTING THROUGH THAT LOT. COME BACK LATER.",
            4 => "I'M BUSY WITH THE TILL. ASK ME AGAIN LATER.",
            5 => "THE DISPLAY MACHINES AREN'T HOOKED UP YET.",
            6 => "KEEP WORKING ON YOUR PROGRAMS. THERE'LL BE OPPORTUNITIES HERE SOON.",
            7 => "THE CHARTS WILL BE UP ONCE THE SOFTWARE MARKET GETS GOING.",
            _ => "NOT OPEN YET."
        };
    }

    private void HandleMouse(MouseState mouse)
    {
        _hover = -1;
        for (var i = 0; i < Entries.Length; i++)
        {
            if (!RowBounds(i).Contains(mouse.X, mouse.Y))
                continue;

            _hover = i;
            var clicked = mouse.LeftButton == ButtonState.Pressed &&
                          _previousMouse.LeftButton == ButtonState.Released;
            if (clicked)
            {
                _selected = i;
                Activate();
            }

            return;
        }
    }

    private void Move(int delta)
    {
        _selected = (_selected + delta + Entries.Length) % Entries.Length;
        _status = string.Empty;
    }

    private void Activate()
    {
        if (_selected < 0 || _selected >= Entries.Length)
            return;

        if (!IsAvailable(_selected))
        {
            _status = LockedMessage(_selected);
            return;
        }

        if (_selected == 0)
            ComputersSelected?.Invoke();
    }

    private static string[] WrapStatus(string text, int width)
    {
        if (text.Length <= width)
            return new[] { text };

        var split = text.LastIndexOf(' ', Math.Min(width, text.Length - 1));
        if (split <= 0)
            split = width;

        var first = text[..split].TrimEnd();
        var second = text[split..].TrimStart();
        if (second.Length > width)
            second = second[..(width - 1)] + ".";

        return new[] { first, second };
    }

    private static Rectangle RowBounds(int index) =>
        new(48, 178 + index * 22, 544, 18);

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
