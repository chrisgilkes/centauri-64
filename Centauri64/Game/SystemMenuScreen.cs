using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class SystemMenuScreen
{
    private static readonly string[] Labels =
    {
        "[1]  RESUME CENTAURI64",
        "[2]  PROGRAMMING MANUAL",
        "[3]  MY SOFTWARE",
        "[4]  MAGAZINES",
        "[5]  SETTINGS",
        "[6]  REBOOT"
    };

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private int _selected;

    public event Action? ResumeSelected;
    public event Action? ManualSelected;
    public event Action? SoftwareSelected;
    public event Action? MagazinesSelected;
    public event Action? SettingsSelected;
    public event Action? RebootSelected;

    public SystemMenuScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open()
    {
        _previous = Keyboard.GetState();
        _selected = 0;
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var count = Labels.Length;

        if (Pressed(keyboard, Keys.Escape))
        {
            ResumeSelected?.Invoke();
            _previous = keyboard;
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            _selected = (_selected + count - 1) % count;

        if (Pressed(keyboard, Keys.Down))
            _selected = (_selected + 1) % count;

        for (var i = 0; i < count; i++)
        {
            if (Pressed(keyboard, Keys.D1 + i) || Pressed(keyboard, Keys.NumPad1 + i))
            {
                _selected = i;
                InvokeSelected();
                _previous = keyboard;
                return;
            }
        }

        if (Pressed(keyboard, Keys.Enter))
            InvokeSelected();

        _previous = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            BootUi.Background);
        BootUi.DrawBox(spriteBatch, _pixel, new Rectangle(16, 16, 608, 40), BootUi.Header);
        BootUi.DrawText(_font, spriteBatch, "SYSTEM", 280, 28, Color.White);

        var y = 140;
        for (var i = 0; i < Labels.Length; i++)
        {
            var colour = _selected == i ? BootUi.Yellow : BootUi.Cream;
            BootUi.DrawText(_font, spriteBatch, Labels[i], 160, y, colour);
            y += 28;
        }

        BootUi.DrawText(_font, spriteBatch, "HARDCORE CODER  /  NO CAREER", 184, 360, BootUi.Cyan);
        BootUi.DrawText(_font, spriteBatch, "UP DOWN  ENTER SELECT  ESC RESUME", 160, 432, BootUi.Muted);
        spriteBatch.End();
    }

    private void InvokeSelected()
    {
        switch (_selected)
        {
            case 0:
                ResumeSelected?.Invoke();
                break;
            case 1:
                ManualSelected?.Invoke();
                break;
            case 2:
                SoftwareSelected?.Invoke();
                break;
            case 3:
                MagazinesSelected?.Invoke();
                break;
            case 4:
                SettingsSelected?.Invoke();
                break;
            case 5:
                RebootSelected?.Invoke();
                break;
        }
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
