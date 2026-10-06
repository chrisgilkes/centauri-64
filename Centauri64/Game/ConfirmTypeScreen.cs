using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class ConfirmTypeScreen
{
    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private string _buffer = string.Empty;
    private string _title = string.Empty;
    private string _body = string.Empty;
    private string _word = "DELETE";
    private string _status = string.Empty;

    public event Action? Confirmed;
    public event Action? Cancelled;

    public ConfirmTypeScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open(string title, string body, string word)
    {
        _title = title;
        _body = body;
        _word = word.ToUpperInvariant();
        _buffer = string.Empty;
        _status = "TYPE " + _word + " TO CONFIRM.";
        _previous = Keyboard.GetState();
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
            _buffer = _buffer[..^1];

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!Pressed(keyboard, key))
                continue;
            if (key >= Keys.A && key <= Keys.Z && _buffer.Length < 16)
                _buffer += (char)('A' + (key - Keys.A));
        }

        if (Pressed(keyboard, Keys.Enter))
        {
            if (_buffer == _word)
                Confirmed?.Invoke();
            else
                _status = "TYPE " + _word + " EXACTLY, OR PRESS ESC.";
        }

        _previous = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            BootUi.Background);
        BootUi.DrawBox(spriteBatch, _pixel, new Rectangle(16, 16, 608, 40), BootUi.Header);
        BootUi.DrawText(_font, spriteBatch, _title, 48, 28, BootUi.Yellow);

        var y = 96;
        foreach (var line in _body.Split('\n'))
        {
            BootUi.DrawText(_font, spriteBatch, line, 48, y, BootUi.Cream);
            y += 20;
        }

        BootUi.DrawText(_font, spriteBatch, "> " + _buffer + "_", 48, 280, Color.White);
        BootUi.DrawText(_font, spriteBatch, _status, 48, 320, BootUi.Muted);
        BootUi.DrawText(_font, spriteBatch, "ENTER CONFIRM    ESC CANCEL", 48, 432, BootUi.Muted);
        spriteBatch.End();
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
