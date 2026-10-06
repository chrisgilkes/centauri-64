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
    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private IReadOnlyList<CareerSlotSummary> _slots = Array.Empty<CareerSlotSummary>();
    private bool _hardcore;
    private int _slotIndex;

    public event Action<int>? BedroomSlotChosen;
    public event Action<int>? NewCareerChosen;
    public event Action? HardcoreChosen;
    public event Action<int>? DeleteSlotRequested;

    public ModeSelectScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open(IReadOnlyList<CareerSlotSummary> slots)
    {
        _slots = slots;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        _hardcore = false;
        _slotIndex = 0;
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();

        if (Pressed(keyboard, Keys.Left))
            _hardcore = false;

        if (Pressed(keyboard, Keys.Right))
            _hardcore = true;

        if (!_hardcore)
        {
            if (Pressed(keyboard, Keys.Up))
                _slotIndex = (_slotIndex + _slots.Count - 1) % _slots.Count;

            if (Pressed(keyboard, Keys.Down))
                _slotIndex = (_slotIndex + 1) % _slots.Count;
        }

        HandleMouse(mouse);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            Confirm();

        if (Pressed(keyboard, Keys.Delete) && !_hardcore)
        {
            var slot = _slots[_slotIndex];
            if (slot.Occupied)
                DeleteSlotRequested?.Invoke(slot.Slot);
        }

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            BootUi.Background);

        BootUi.DrawBox(spriteBatch, _pixel, new Rectangle(16, 16, 608, 40), BootUi.Header);
        BootUi.DrawText(_font, spriteBatch, "WELCOME TO CENTAURI64", 216, 28, Color.White);

        BootUi.DrawText(_font, spriteBatch, "BEDROOM CODER", 64, 72,
            !_hardcore ? BootUi.Yellow : BootUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "HARDCORE CODER", 400, 72,
            _hardcore ? BootUi.Yellow : BootUi.Muted);

        DrawBedroomColumn(spriteBatch);
        DrawHardcoreColumn(spriteBatch);

        var flavour = _hardcore
            ? "EVERYTHING UNLOCKED FROM THE START. NO CAREER. JUST PROGRAM."
            : "START IN 1986. LEARN TO PROGRAM. BUILD GAMES. BUILD YOUR CAREER.";
        BootUi.DrawBox(spriteBatch, _pixel, new Rectangle(16, 400, 608, 64), BootUi.Header);
        BootUi.DrawText(_font, spriteBatch, flavour, 32, 412, BootUi.Cream);
        BootUi.DrawText(_font, spriteBatch, "ARROWS MOVE   ENTER SELECT   DEL DELETE SLOT   MOUSE CLICK", 32, 436, BootUi.Muted);
        spriteBatch.End();
    }

    private void DrawBedroomColumn(SpriteBatch spriteBatch)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var y = 104 + i * 88;
            var bounds = SlotBounds(i);
            var selected = !_hardcore && i == _slotIndex;
            BootUi.DrawBox(spriteBatch, _pixel, bounds, selected ? BootUi.Highlight : BootUi.Panel);
            BootUi.DrawBorder(spriteBatch, _pixel, bounds, selected ? BootUi.Yellow : BootUi.Cyan);

            if (!slot.Occupied)
            {
                BootUi.DrawText(_font, spriteBatch, "NEW CAREER", bounds.X + 16, y + 28, BootUi.Cream);
                continue;
            }

            BootUi.DrawText(_font, spriteBatch, slot.Name, bounds.X + 16, y + 12, Color.White);
            BootUi.DrawText(_font, spriteBatch, slot.Rank, bounds.X + 16, y + 32, BootUi.Cyan);
            BootUi.DrawText(_font, spriteBatch, PlayerProgress.FormatPounds(slot.CashPennies),
                bounds.X + 16, y + 52, BootUi.Yellow);
        }
    }

    private void DrawHardcoreColumn(SpriteBatch spriteBatch)
    {
        var bounds = HardcoreBounds();
        var selected = _hardcore;
        BootUi.DrawBox(spriteBatch, _pixel, bounds, selected ? BootUi.Highlight : BootUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, bounds, selected ? BootUi.Yellow : BootUi.Cyan);

        BootUi.DrawText(_font, spriteBatch, "CENTAURI64", bounds.X + 40, 160, Color.White);
        BootUi.DrawText(_font, spriteBatch, "EVERYTHING UNLOCKED", bounds.X + 16, 200, BootUi.Yellow);
        BootUi.DrawText(_font, spriteBatch, "NO CAREER", bounds.X + 56, 232, BootUi.Cream);
        BootUi.DrawText(_font, spriteBatch, "NO PROGRESSION", bounds.X + 32, 256, BootUi.Cream);
        BootUi.DrawText(_font, spriteBatch, "JUST PROGRAM.", bounds.X + 40, 304, BootUi.Cyan);
    }

    private void HandleMouse(MouseState mouse)
    {
        var clicked = mouse.LeftButton == ButtonState.Pressed &&
                      _previousMouse.LeftButton == ButtonState.Released;

        if (HardcoreBounds().Contains(mouse.X, mouse.Y))
        {
            _hardcore = true;
            if (clicked)
                Confirm();
            return;
        }

        for (var i = 0; i < _slots.Count; i++)
        {
            if (!SlotBounds(i).Contains(mouse.X, mouse.Y))
                continue;

            _hardcore = false;
            _slotIndex = i;
            if (clicked)
                Confirm();
            return;
        }
    }

    private void Confirm()
    {
        if (_hardcore)
        {
            HardcoreChosen?.Invoke();
            return;
        }

        var slot = _slots[_slotIndex];
        if (slot.Occupied)
            BedroomSlotChosen?.Invoke(slot.Slot);
        else
            NewCareerChosen?.Invoke(slot.Slot);
    }

    private static Rectangle SlotBounds(int index) =>
        new(40, 96 + index * 88, 248, 80);

    private static Rectangle HardcoreBounds() =>
        new(352, 96, 248, 280);

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
