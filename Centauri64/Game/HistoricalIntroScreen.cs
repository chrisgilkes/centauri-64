using System;
using System.Text;

using Centauri64.Graphics;
using Centauri64.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class HistoricalIntroScreen
{
    private static readonly string[] Lines =
    {
        "1986.",
        "",
        "CENTAURI COMPUTER SYSTEMS ENTERS THE CROWDED",
        "HOME COMPUTER MARKET WITH AN AMBITIOUS NEW",
        "MACHINE.",
        "",
        "THE CENTAURI64",
        "",
        "POWERFUL GRAPHICS.",
        "PROGRAMMABLE SPRITES.",
        "BUILT-IN BASIC.",
        "",
        "A COMPUTER FOR PEOPLE WHO DON'T JUST WANT",
        "TO PLAY GAMES.",
        "",
        "THEY WANT TO MAKE THEM."
    };

    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private readonly StringBuilder _visible = new();
    private int _line;
    private int _column;
    private double _timer;
    private bool _complete;
    private const double CharSeconds = 0.055;

    public event Action? Finished;

    public HistoricalIntroScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open()
    {
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        _visible.Clear();
        _line = 0;
        _column = 0;
        _timer = 0;
        _complete = false;
        AdvanceVisible();
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var pressed = AnyAdvance(keyboard, mouse);

        if (!_complete)
        {
            if (pressed)
            {
                CompleteAll();
            }
            else
            {
                _timer += gameTime.ElapsedGameTime.TotalSeconds;
                while (_timer >= CharSeconds && !_complete)
                {
                    _timer -= CharSeconds;
                    TypeNext();
                }
            }
        }
        else if (pressed)
        {
            Finished?.Invoke();
        }

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            Color.Black);

        var y = 72;
        foreach (var line in _visible.ToString().Split('\n'))
        {
            var colour = line == "THE CENTAURI64" || line == "1986."
                ? BootUi.Yellow
                : BootUi.Cream;
            _font.Draw(spriteBatch, line, new Vector2(48, y), colour);
            y += 20;
        }

        var hint = _complete
            ? "PRESS ANY KEY TO CONTINUE"
            : "PRESS ANY KEY TO SKIP";
        _font.Draw(spriteBatch, hint, new Vector2(200, 440), BootUi.Muted);
        spriteBatch.End();
    }

    private void TypeNext()
    {
        if (_line >= Lines.Length)
        {
            _complete = true;
            return;
        }

        var line = Lines[_line];
        if (_column >= line.Length)
        {
            _line++;
            _column = 0;
            if (_line < Lines.Length)
                _visible.Append('\n');
            else
                _complete = true;
            return;
        }

        _visible.Append(line[_column]);
        _column++;
    }

    private void CompleteAll()
    {
        _visible.Clear();
        _visible.Append(string.Join('\n', Lines));
        _complete = true;
        _line = Lines.Length;
    }

    private void AdvanceVisible()
    {
        _visible.Clear();
    }

    private bool AnyAdvance(KeyboardState keyboard, MouseState mouse)
    {
        if (keyboard.GetPressedKeyCount() > 0 &&
            _previous.GetPressedKeyCount() == 0)
            return true;

        if (mouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released)
            return true;

        return false;
    }
}
