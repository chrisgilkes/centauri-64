using System;
using System.Text;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class CareerNameScreen
{
    private const int MaxLength = 12;

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private readonly StringBuilder _buffer = new();
    private KeyboardState _previous;
    private string _status = string.Empty;

    public event Action<string>? NameAccepted;
    public event Action? Cancelled;

    public CareerNameScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open()
    {
        _buffer.Clear();
        _previous = Keyboard.GetState();
        _status = "LETTERS AND NUMBERS. ENTER TO CONTINUE.";
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (Pressed(keyboard, Keys.Escape))
        {
            Cancelled?.Invoke();
            _previous = keyboard;
            return;
        }

        if (Pressed(keyboard, Keys.Back) && _buffer.Length > 0)
            _buffer.Length--;

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!Pressed(keyboard, key) || _buffer.Length >= MaxLength)
                continue;

            if (key >= Keys.A && key <= Keys.Z)
                _buffer.Append((char)('A' + (key - Keys.A)));
            else if (key >= Keys.D0 && key <= Keys.D9)
                _buffer.Append((char)('0' + (key - Keys.D0)));
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                _buffer.Append((char)('0' + (key - Keys.NumPad0)));
            else if (key == Keys.Space && _buffer.Length > 0)
                _buffer.Append(' ');
        }

        if (Pressed(keyboard, Keys.Enter))
        {
            var name = _buffer.ToString().Trim();
            if (name.Length == 0)
                _status = "PLEASE ENTER A NAME.";
            else
                NameAccepted?.Invoke(name);
        }

        _previous = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            SystemUi.Background);

        spriteBatch.Draw(_pixel, new Rectangle(40, 28, 560, 1), SystemUi.Line);
        BootUi.DrawText(_font, spriteBatch, "CENTAURI64", 40, 40, SystemUi.Green);
        BootUi.DrawText(_font, spriteBatch, "NEW CAREER", 424, 40, SystemUi.Amber);
        spriteBatch.Draw(_pixel, new Rectangle(40, 64, 560, 1), SystemUi.Line);

        BootUi.DrawText(_font, spriteBatch, "YOUR NAME", 40, 160, SystemUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "> " + _buffer + "_", 40, 200, SystemUi.Text);
        spriteBatch.Draw(_pixel, new Rectangle(40, 228, 320, 1), SystemUi.Line);
        BootUi.DrawText(_font, spriteBatch, _status, 40, 280, SystemUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "ENTER CONTINUE    ESC CANCEL", 40, 440, SystemUi.Muted);
        spriteBatch.End();
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
