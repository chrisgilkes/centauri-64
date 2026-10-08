using System;
using System.Collections.Generic;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Progression;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class ModeSelectScreen
{
    private const int OptionHardcore = 3;
    private const int OptionCount = 4;
    private const double DoubleClickSeconds = 0.35;

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private readonly SystemBinaryBorder _binaryBorder = new();
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private IReadOnlyList<CareerSlotSummary> _slots = Array.Empty<CareerSlotSummary>();

    /// <summary>0–2 career slots, 3 = Hardcore.</summary>
    private int _option;
    private int _hoverOption = -1;

    private bool _confirmDelete;
    private int _confirmFocus; // 0 = CANCEL, 1 = DELETE
    private int _pendingDeleteSlot;

    private int _lastClickOption = -1;
    private double _lastClickTime = -1;

    public event Action<int>? BedroomSlotChosen;
    public event Action<int>? NewCareerChosen;
    public event Action? HardcoreChosen;

    /// <summary>Fired only after the player confirms deletion.</summary>
    public event Action<int>? DeleteSlotRequested;

    /// <summary>Fired when the remembered option changes (slot 0–2 or Hardcore=3).</summary>
    public event Action<int>? SelectionRemembered;

    public ModeSelectScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open(IReadOnlyList<CareerSlotSummary> slots, int rememberedOption = 0)
    {
        _slots = slots;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        _confirmDelete = false;
        _hoverOption = -1;
        _lastClickOption = -1;
        _lastClickTime = -1;
        _option = ClampOption(rememberedOption);
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();
        var now = gameTime.TotalGameTime.TotalSeconds;
        _binaryBorder.Update(gameTime);

        if (_confirmDelete)
        {
            UpdateDeleteConfirm(keyboard, mouse);
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        HandleMouse(mouse, now);

        if (Pressed(keyboard, Keys.Up))
            MoveOption(-1);

        if (Pressed(keyboard, Keys.Down))
            MoveOption(1);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            Confirm();

        if (Pressed(keyboard, Keys.Delete) && IsOccupiedCareerSelected())
            BeginDeleteConfirm();

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            SystemUi.Background);

        _binaryBorder.Draw(spriteBatch, _font, SystemUi.Green, SystemUi.Line);
        DrawHeader(spriteBatch);
        DrawCareerSection(spriteBatch);
        DrawHardcorePanel(spriteBatch);
        DrawFooter(spriteBatch);

        if (_confirmDelete)
            DrawDeleteConfirm(spriteBatch);

        spriteBatch.End();
    }

    private void DrawHeader(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, new Rectangle(40, 18, 560, 1), SystemUi.Line);
        BootUi.DrawText(_font, spriteBatch, "CENTAURI64", 40, 28, SystemUi.Green);
        BootUi.DrawText(_font, spriteBatch, "1986 · SYSTEM", 456, 28, SystemUi.Muted);
        spriteBatch.Draw(_pixel, new Rectangle(40, 48, 560, 1), SystemUi.Line);

        BootUi.DrawText(_font, spriteBatch, "WELCOME BACK, CODER.", 40, 58, SystemUi.Text);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "Choose a career to continue, or start programming freely.",
            40,
            74,
            SystemUi.Muted);

        BootUi.DrawText(_font, spriteBatch, "BEDROOM CODER — CAREER MODE", 40, 96, SystemUi.Green);
    }

    private void DrawCareerSection(SpriteBatch spriteBatch)
    {
        for (var i = 0; i < _slots.Count; i++)
            DrawCareerCard(spriteBatch, i, _slots[i]);
    }

    private void DrawCareerCard(SpriteBatch spriteBatch, int index, CareerSlotSummary slot)
    {
        var bounds = SlotBounds(index);
        var selected = !_confirmDelete && _option == index;
        var hovered = !_confirmDelete && _hoverOption == index && !selected;

        var fill = selected ? SystemUi.Highlight : SystemUi.Panel;
        var border = selected ? SystemUi.Amber : hovered ? SystemUi.Green : SystemUi.Line;

        BootUi.DrawBox(spriteBatch, _pixel, bounds, fill);
        BootUi.DrawBorder(spriteBatch, _pixel, bounds, border);

        if (!slot.Occupied)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "+ NEW BEDROOM CODER",
                bounds.X + 12,
                bounds.Y + 14,
                selected ? SystemUi.Amber : SystemUi.Text);
            BootUi.DrawText(
                _font,
                spriteBatch,
                "Start your programming career in 1986.",
                bounds.X + 12,
                bounds.Y + 34,
                SystemUi.Muted);

            if (selected)
            {
                BootUi.DrawText(
                    _font,
                    spriteBatch,
                    "CONTINUE >",
                    bounds.Right - 104,
                    bounds.Y + 52,
                    SystemUi.Amber);
            }

            return;
        }

        var name = Truncate(slot.Name, 20);
        BootUi.DrawText(
            _font,
            spriteBatch,
            name,
            bounds.X + 12,
            bounds.Y + 10,
            SystemUi.Text);

        if (selected)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "SELECTED",
                bounds.Right - 84,
                bounds.Y + 10,
                SystemUi.Amber);
        }

        BootUi.DrawText(
            _font,
            spriteBatch,
            Truncate(slot.Rank, 14),
            bounds.X + 12,
            bounds.Y + 28,
            SystemUi.Green);

        var cash = PlayerProgress.FormatPounds(slot.CashPennies);
        BootUi.DrawText(
            _font,
            spriteBatch,
            cash,
            bounds.Right - 12 - cash.Length * 8,
            bounds.Y + 28,
            SystemUi.Amber);

        var mags = $"MAGAZINES {slot.MagazinesOwned:00}/{slot.MagazinesTotal:00}";
        BootUi.DrawText(
            _font,
            spriteBatch,
            mags,
            bounds.X + 12,
            bounds.Y + 46,
            SystemUi.Muted);

        var last = "LAST PLAYED: " + FormatLastPlayed(slot.LastPlayedUtc);
        BootUi.DrawText(
            _font,
            spriteBatch,
            Truncate(last, 28),
            bounds.Right - 12 - Math.Min(last.Length, 28) * 8,
            bounds.Y + 46,
            SystemUi.Muted);

        if (selected)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "CONTINUE >",
                bounds.Right - 104,
                bounds.Y + 58,
                SystemUi.Amber);
        }
    }

    private void DrawHardcorePanel(SpriteBatch spriteBatch)
    {
        var bounds = HardcoreBounds();
        var selected = !_confirmDelete && _option == OptionHardcore;
        var hovered = !_confirmDelete && _hoverOption == OptionHardcore && !selected;

        BootUi.DrawBox(spriteBatch, _pixel, bounds, selected ? SystemUi.Highlight : SystemUi.Panel);
        BootUi.DrawBorder(
            spriteBatch,
            _pixel,
            bounds,
            selected ? SystemUi.Amber : hovered ? SystemUi.Green : SystemUi.Line);

        BootUi.DrawText(_font, spriteBatch, "HARDCORE CODER", bounds.X + 12, bounds.Y + 12, SystemUi.Text);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "ALL TOOLS UNLOCKED",
            bounds.Right - 172,
            bounds.Y + 12,
            SystemUi.Green);

        BootUi.DrawText(
            _font,
            spriteBatch,
            "No career, no restrictions. Just you and the Centauri64.",
            bounds.X + 12,
            bounds.Y + 32,
            SystemUi.Muted);

        if (selected)
        {
            BootUi.DrawText(
                _font,
                spriteBatch,
                "START PROGRAMMING >",
                bounds.Right - 180,
                bounds.Y + 50,
                SystemUi.Amber);
        }
    }

    private void DrawFooter(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, new Rectangle(40, 400, 560, 1), SystemUi.Line);

        string flavour;
        if (_option == OptionHardcore)
        {
            flavour = "No career. No progression. Just a computer and your imagination.";
        }
        else if (_option >= 0 && _option < _slots.Count && _slots[_option].Occupied)
        {
            flavour =
                "Your programming adventure continues. Magazines to collect, games to write, and publishers to impress.";
        }
        else
        {
            flavour = "Start in 1986 with your first computer and discover the world of BASIC programming.";
        }

        BootUi.DrawText(_font, spriteBatch, Truncate(flavour, 70), 40, 412, SystemUi.Text);

        var help = IsOccupiedCareerSelected()
            ? "ARROWS SELECT    ENTER CONTINUE    DEL DELETE    MOUSE CLICK"
            : "ARROWS SELECT    ENTER CONTINUE    MOUSE CLICK";
        BootUi.DrawText(_font, spriteBatch, help, 40, 444, SystemUi.Muted);
    }

    private void DrawDeleteConfirm(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            new Color(0, 0, 0, 180));

        var box = new Rectangle(120, 140, 400, 180);
        BootUi.DrawBox(spriteBatch, _pixel, box, SystemUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, box, SystemUi.Amber);

        var name = "CAREER";
        foreach (var slot in _slots)
        {
            if (slot.Slot == _pendingDeleteSlot && slot.Occupied)
            {
                name = slot.Name;
                break;
            }
        }

        BootUi.DrawText(_font, spriteBatch, "DELETE CAREER?", box.X + 24, box.Y + 24, SystemUi.Amber);
        BootUi.DrawText(_font, spriteBatch, name, box.X + 24, box.Y + 52, SystemUi.Text);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "This will permanently delete this",
            box.X + 24,
            box.Y + 80,
            SystemUi.Muted);
        BootUi.DrawText(
            _font,
            spriteBatch,
            "career's progress. Programs are kept.",
            box.X + 24,
            box.Y + 96,
            SystemUi.Muted);

        var cancel = CancelButtonBounds();
        var delete = DeleteButtonBounds();

        DrawButton(spriteBatch, cancel, "[ CANCEL ]", _confirmFocus == 0);
        DrawButton(spriteBatch, delete, "[ DELETE ]", _confirmFocus == 1, destructive: true);
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        string label,
        bool focused,
        bool destructive = false)
    {
        var border = focused
            ? (destructive ? SystemUi.Amber : SystemUi.Green)
            : SystemUi.Line;
        BootUi.DrawBox(spriteBatch, _pixel, bounds, SystemUi.Background);
        BootUi.DrawBorder(spriteBatch, _pixel, bounds, border);
        BootUi.DrawText(
            _font,
            spriteBatch,
            label,
            bounds.X + 12,
            bounds.Y + 8,
            focused ? (destructive ? SystemUi.Amber : SystemUi.Text) : SystemUi.Muted);
    }

    private void UpdateDeleteConfirm(KeyboardState keyboard, MouseState mouse)
    {
        if (Pressed(keyboard, Keys.Escape))
        {
            _confirmDelete = false;
            return;
        }

        if (Pressed(keyboard, Keys.Left) || Pressed(keyboard, Keys.Right))
            _confirmFocus = 1 - _confirmFocus;

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
        {
            if (_confirmFocus == 0)
                _confirmDelete = false;
            else
                ConfirmDelete();
            return;
        }

        var clicked = mouse.LeftButton == ButtonState.Pressed &&
                      _previousMouse.LeftButton == ButtonState.Released;

        if (CancelButtonBounds().Contains(mouse.X, mouse.Y))
        {
            _confirmFocus = 0;
            if (clicked)
                _confirmDelete = false;
            return;
        }

        if (DeleteButtonBounds().Contains(mouse.X, mouse.Y))
        {
            _confirmFocus = 1;
            if (clicked)
                ConfirmDelete();
        }
    }

    private void ConfirmDelete()
    {
        var slot = _pendingDeleteSlot;
        _confirmDelete = false;
        DeleteSlotRequested?.Invoke(slot);
    }

    private void BeginDeleteConfirm()
    {
        if (!IsOccupiedCareerSelected())
            return;

        _pendingDeleteSlot = _slots[_option].Slot;
        _confirmFocus = 0;
        _confirmDelete = true;
    }

    private void HandleMouse(MouseState mouse, double now)
    {
        _hoverOption = HitTest(mouse.X, mouse.Y);

        var clicked = mouse.LeftButton == ButtonState.Pressed &&
                      _previousMouse.LeftButton == ButtonState.Released;
        if (!clicked || _hoverOption < 0)
            return;

        var option = _hoverOption;
        var doubleClick =
            option == _lastClickOption &&
            _lastClickTime >= 0 &&
            now - _lastClickTime <= DoubleClickSeconds;

        _option = option;
        RememberSelection();

        if (doubleClick)
        {
            _lastClickOption = -1;
            _lastClickTime = -1;
            Confirm();
            return;
        }

        _lastClickOption = option;
        _lastClickTime = now;

        // Single click on CONTINUE / START PROGRAMMING action zone activates.
        if (IsActionZone(option, mouse.X, mouse.Y))
            Confirm();
    }

    private bool IsActionZone(int option, int x, int y)
    {
        if (option == OptionHardcore)
            return StartProgrammingBounds().Contains(x, y);

        if (option >= 0 && option < _slots.Count)
            return ContinueBounds(option).Contains(x, y);

        return false;
    }

    private int HitTest(int x, int y)
    {
        if (HardcoreBounds().Contains(x, y))
            return OptionHardcore;

        for (var i = 0; i < _slots.Count; i++)
        {
            if (SlotBounds(i).Contains(x, y))
                return i;
        }

        return -1;
    }

    private void MoveOption(int delta)
    {
        _option = (_option + delta + OptionCount) % OptionCount;
        RememberSelection();
    }

    private void Confirm()
    {
        RememberSelection();

        if (_option == OptionHardcore)
        {
            HardcoreChosen?.Invoke();
            return;
        }

        if (_option < 0 || _option >= _slots.Count)
            return;

        var slot = _slots[_option];
        if (slot.Occupied)
            BedroomSlotChosen?.Invoke(slot.Slot);
        else
            NewCareerChosen?.Invoke(slot.Slot);
    }

    private void RememberSelection() =>
        SelectionRemembered?.Invoke(_option);

    private bool IsOccupiedCareerSelected() =>
        _option >= 0 &&
        _option < _slots.Count &&
        _slots[_option].Occupied;

    private int ClampOption(int option)
    {
        if (option < 0 || option >= OptionCount)
            return 0;

        return option;
    }

    private static Rectangle SlotBounds(int index) =>
        new(40, 110 + index * 72, 560, 68);

    private static Rectangle HardcoreBounds() =>
        new(40, 330, 560, 62);

    private static Rectangle ContinueBounds(int index)
    {
        var slot = SlotBounds(index);
        return new Rectangle(slot.Right - 112, slot.Bottom - 22, 100, 18);
    }

    private static Rectangle StartProgrammingBounds()
    {
        var panel = HardcoreBounds();
        return new Rectangle(panel.Right - 188, panel.Bottom - 22, 176, 18);
    }

    private static Rectangle CancelButtonBounds() =>
        new(160, 268, 120, 28);

    private static Rectangle DeleteButtonBounds() =>
        new(360, 268, 120, 28);

    public static string FormatLastPlayed(DateTime? utc)
    {
        if (utc == null)
            return "UNKNOWN";

        var local = utc.Value.ToLocalTime().Date;
        var today = DateTime.Now.Date;
        if (local == today)
            return "TODAY";
        if (local == today.AddDays(-1))
            return "YESTERDAY";

        return local.ToString("dd MMM yyyy").ToUpperInvariant();
    }

    private static string Truncate(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text;

        if (maxChars <= 1)
            return text[..1];

        return text[..(maxChars - 1)] + ".";
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
