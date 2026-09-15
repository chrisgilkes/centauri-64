using Centauri64.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Console;

public sealed class TextConsole
{
    public const int COLUMNS = 80;
    public const int ROWS = 50;

    private const int CHARACTER_WIDTH = 8;
    private const int CHARACTER_HEIGHT = 8;

    private readonly char[,] _characters = new char[ROWS, COLUMNS];

    private int _cursorColumn;
    private int _cursorRow;

    private const double CURSOR_FLASH_TIME = 0.5;

    private double _cursorTimer;
    private bool _cursorVisible = true;

    private KeyboardState _previousKeyboardState;

    public TextConsole()
    {
        Clear();
    }

    public void Update(GameTime gameTime)
    {
        _cursorTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (_cursorTimer >= CURSOR_FLASH_TIME)
        {
            _cursorTimer -= CURSOR_FLASH_TIME;

            _cursorVisible = !_cursorVisible;
        }

        HandleKeyboard();
    }

    private void HandleKeyboard()
    {
        var keyboardState = Keyboard.GetState();

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (_previousKeyboardState.IsKeyUp(key))
            {
                HandleKey(key);
            }
        }

        _previousKeyboardState = keyboardState;
    }

    private void HandleKey(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            var character = (char)('A' + (key - Keys.A));
            PutCharacter(character);
            ResetCursorFlash();
            return;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            var character = (char)('0' + (key - Keys.D0));
            PutCharacter(character);
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Space)
        {
            PutCharacter(' ');
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Enter)
        {
            NewLine();
            ResetCursorFlash();
        }
    }

    private void ResetCursorFlash()
    {
        _cursorTimer = 0.0;
        _cursorVisible = true;
    }

    public void Clear()
    {
        for (var row = 0; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                _characters[row, column] = ' ';
            }
        }

        _cursorColumn = 0;
        _cursorRow = 0;
    }

    public void Write(string text)
    {
        foreach (var character in text)
        {
            PutCharacter(character);
        }
    }

    public void WriteLine(string text)
    {
        Write(text);
        NewLine();
    }

    private void PutCharacter(char character)
    {
        _characters[_cursorRow, _cursorColumn] = character;

        _cursorColumn++;

        if (_cursorColumn >= COLUMNS)
        {
            NewLine();
        }
    }

    private void NewLine()
    {
        _cursorColumn = 0;
        _cursorRow++;

        if (_cursorRow >= ROWS)
        {
            Scroll();
            _cursorRow = ROWS - 1;
        }
    }

    private void Scroll()
    {
        for (var row = 1; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                _characters[row - 1, column] =
                    _characters[row, column];
            }
        }

        for (var column = 0; column < COLUMNS; column++)
        {
            _characters[ROWS - 1, column] = ' ';
        }
    }

    public void Draw(SpriteBatch spriteBatch,BitmapFont font,Texture2D pixel,Color foregroundColor,Color backgroundColor)
    {
        for (var row = 0; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                var character = _characters[row, column];

                if (character == ' ')
                    continue;

               font.DrawCharacter(
                    spriteBatch,
                    character,
                    new Vector2(
                        column * CHARACTER_WIDTH,
                        row * CHARACTER_HEIGHT),
                    foregroundColor);
            }
        }

        if (_cursorVisible)
        {
            var cursorPosition = new Vector2(
                _cursorColumn * CHARACTER_WIDTH,
                _cursorRow * CHARACTER_HEIGHT);

            var cursorRectangle = new Rectangle(
                (int)cursorPosition.X,
                (int)cursorPosition.Y,
                CHARACTER_WIDTH,
                CHARACTER_HEIGHT);

            spriteBatch.Draw(
                pixel,
                cursorRectangle,
                foregroundColor);

            var character = _characters[_cursorRow, _cursorColumn];

            if (character != ' ')
            {
                font.DrawCharacter(
                    spriteBatch,
                    character,
                    cursorPosition,
                    backgroundColor);
            }
        }
    }
}