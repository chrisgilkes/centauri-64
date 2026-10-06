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
            SystemUi.Background);

        spriteBatch.Draw(_pixel, new Rectangle(40, 28, 560, 1), SystemUi.Line);
        BootUi.DrawText(_font, spriteBatch, "CENTAURI64", 40, 40, SystemUi.Green);
        BootUi.DrawText(_font, spriteBatch, "SYSTEM", 520, 40, SystemUi.Muted);
        spriteBatch.Draw(_pixel, new Rectangle(40, 64, 560, 1), SystemUi.Line);

        BootUi.DrawText(_font, spriteBatch, "BEDROOM CODER", 40, 80,
            !_hardcore ? SystemUi.Amber : SystemUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "HARDCORE CODER", 392, 80,
            _hardcore ? SystemUi.Amber : SystemUi.Muted);

        if (!_hardcore)
            spriteBatch.Draw(_pixel, new Rectangle(40, 100, 136, 2), SystemUi.Amber);
        else
            spriteBatch.Draw(_pixel, new Rectangle(392, 100, 144, 2), SystemUi.Amber);

        DrawBedroomColumn(spriteBatch);
        DrawHardcoreColumn(spriteBatch);

        var flavour = _hardcore
            ? "EVERYTHING UNLOCKED FROM THE START. NO CAREER. JUST PROGRAM."
            : "START IN 1986. LEARN TO PROGRAM. BUILD GAMES. BUILD YOUR CAREER.";
        spriteBatch.Draw(_pixel, new Rectangle(40, 396, 560, 1), SystemUi.Line);
        BootUi.DrawText(_font, spriteBatch, flavour, 40, 412, SystemUi.Text);
        BootUi.DrawText(_font, spriteBatch, "ARROWS MOVE   ENTER SELECT   DEL DELETE SLOT   MOUSE CLICK", 40, 440, SystemUi.Muted);
        spriteBatch.End();
    }

    private void DrawBedroomColumn(SpriteBatch spriteBatch)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var bounds = SlotBounds(i);
            var selected = !_hardcore && i == _slotIndex;
            BootUi.DrawBox(spriteBatch, _pixel, bounds, selected ? SystemUi.Highlight : SystemUi.Panel);
            BootUi.DrawBorder(spriteBatch, _pixel, bounds, selected ? SystemUi.Amber : SystemUi.Line);

            var marker = selected ? ">" : " ";
            if (!slot.Occupied)
            {
                BootUi.DrawText(_font, spriteBatch, marker + " NEW CAREER", bounds.X + 12, bounds.Y + 28, SystemUi.Text);
                continue;
            }

            BootUi.DrawText(_font, spriteBatch, marker + " " + slot.Name, bounds.X + 12, bounds.Y + 12, SystemUi.Text);
            BootUi.DrawText(_font, spriteBatch, slot.Rank, bounds.X + 28, bounds.Y + 32, SystemUi.Green);
            BootUi.DrawText(_font, spriteBatch, PlayerProgress.FormatPounds(slot.CashPennies),
                bounds.X + 28, bounds.Y + 52, SystemUi.Amber);
        }
    }

    private void DrawHardcoreColumn(SpriteBatch spriteBatch)
    {
        var bounds = HardcoreBounds();
        var selected = _hardcore;
        BootUi.DrawBox(spriteBatch, _pixel, bounds, selected ? SystemUi.Highlight : SystemUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, bounds, selected ? SystemUi.Amber : SystemUi.Line);

        BootUi.DrawText(_font, spriteBatch, "CENTAURI64", bounds.X + 40, 160, SystemUi.Text);
        BootUi.DrawText(_font, spriteBatch, "EVERYTHING UNLOCKED", bounds.X + 16, 200, SystemUi.Amber);
        BootUi.DrawText(_font, spriteBatch, "NO CAREER", bounds.X + 56, 232, SystemUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "NO PROGRESSION", bounds.X + 32, 256, SystemUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "JUST PROGRAM.", bounds.X + 40, 304, SystemUi.Green);
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
