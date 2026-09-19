using System;
using System.Reflection.Metadata;
using Centauri64.Graphics;
using Centauri64.Machine;
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

    private readonly ScreenCell[,] _cells;

    private int _cursorColumn;
    private int _cursorRow;

    private const double CURSOR_FLASH_TIME = 0.5;

    private double _cursorTimer;
    private bool _cursorVisible = true;

    private KeyboardState _previousKeyboardState;

    private const double KEY_REPEAT_DELAY = 0.4;
    private const double KEY_REPEAT_INTERVAL = 0.05;

    private Keys? _repeatingKey;
    private double _keyRepeatTimer;

    private static readonly char[] ShiftedNumbers =
    {
        ')', '!', '"', '#', '$',
        '%', '^', '&', '*', '('
    };

    private static char? GetPunctuation(Keys key, bool shift)
    {
        return key switch
        {
            Keys.OemPeriod => shift ? '>' : '.',
            Keys.OemComma => shift ? '<' : ',',
            Keys.OemQuestion => shift ? '?' : '/',
            Keys.OemSemicolon => shift ? ':' : ';',
            Keys.OemQuotes => shift ? '@' : '\'',
            Keys.OemOpenBrackets => shift ? '{' : '[',
            Keys.OemCloseBrackets => shift ? '}' : ']',
            Keys.OemPipe => shift ? '|' : '\\',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemPlus => shift ? '+' : '=',
            _ => null
        };
    }


    private static bool CanRepeat(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z)
            return true;

        if (key >= Keys.D0 && key <= Keys.D9)
            return true;

        return key == Keys.Space ||
            key == Keys.Left ||
            key == Keys.Right ||
            key == Keys.Up ||
            key == Keys.Down ||
            key == Keys.Back ||
            key == Keys.Delete ||
            key == Keys.OemPeriod ||
            key == Keys.OemComma ||
            key == Keys.OemQuestion ||
            key == Keys.OemSemicolon ||
            key == Keys.OemQuotes ||
            key == Keys.OemOpenBrackets ||
            key == Keys.OemCloseBrackets ||
            key == Keys.OemPipe ||
            key == Keys.OemMinus ||
            key == Keys.OemPlus;
    }

    public event Action<string>? LineEntered;

    private int _foreground = 1;
    private int _background = 6;

    public int Foreground
    {
        get => _foreground;
        set => _foreground = value;
    }

    public int Background
    {
        get => _background;
        set => _background = value;
    }

    private const int SCREEN_OFFSET_X = CentauriMachine.BORDER_SIZE;

    private const int SCREEN_OFFSET_Y = CentauriMachine.BORDER_SIZE;

    public const int ScreenMargin = 16;

    public int DebugCellBackground => _cells[0, 0].Background;

    public TextConsole()
    {
        _cells = new ScreenCell[ROWS, COLUMNS];

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

        HandleKeyboard(gameTime);
    }

    private void HandleKeyboard(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        var shift =
            keyboardState.IsKeyDown(Keys.LeftShift) ||
            keyboardState.IsKeyDown(Keys.RightShift);

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (_previousKeyboardState.IsKeyUp(key))
            {
                HandleKey(key, shift);

                if (CanRepeat(key))
                {
                    _repeatingKey = key;
                    _keyRepeatTimer = KEY_REPEAT_DELAY;
                }
            }
        }

        UpdateKeyRepeat(keyboardState,shift,gameTime.ElapsedGameTime.TotalSeconds);

        _previousKeyboardState = keyboardState;
    }

    private void UpdateKeyRepeat(KeyboardState keyboardState,bool shift,double deltaTime)
    {
        if (!_repeatingKey.HasValue)
            return;

        var key = _repeatingKey.Value;

        if (keyboardState.IsKeyUp(key))
        {
            _repeatingKey = null;
            _keyRepeatTimer = 0.0;
            return;
        }

        _keyRepeatTimer -= deltaTime;

        if (_keyRepeatTimer <= 0.0)
        {
            HandleKey(key, shift);
            _keyRepeatTimer += KEY_REPEAT_INTERVAL;
        }
    }

    private void HandleKey(Keys key,  bool shift)
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
            var offset = key - Keys.D0;

            var character = shift? ShiftedNumbers[offset]: (char)('0' + offset);

            PutCharacter(character);
            ResetCursorFlash();
            return;
        }

        var punctuation = GetPunctuation(key, shift);

        if (punctuation.HasValue)
        {
            PutCharacter(punctuation.Value);
            ResetCursorFlash();
        }

        if (key == Keys.Left)
        {
            MoveCursorLeft();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Right)
        {
            MoveCursorRight();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Up)
        {
            MoveCursorUp();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Down)
        {
            MoveCursorDown();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Space)
        {
            PutCharacter(' ');
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Back)
        {
            Backspace();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Delete)
        {
            Delete();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.Enter)
        {
            var line = GetCurrentLine();

            NewLine();
            
            LineEntered?.Invoke(line);
            
            ResetCursorFlash();
        }

        if (key == Keys.Home)
        {
            _cursorColumn = 0;
            ResetCursorFlash();
            return;
        }

        if (key == Keys.End)
        {
            _cursorColumn = GetLineLength(_cursorRow);
            ResetCursorFlash();
            return;
        }
    }

    private int GetLineLength(int row)
    {
        for (var column = COLUMNS - 1;
            column >= 0;
            column--)
        {
            if (_cells[row, column].Character != ' ')
                return column + 1;
        }

        return 0;
    }

    private void MoveCursorLeft()
    {
        if (_cursorColumn > 0)
        {
            _cursorColumn--;
        }
    }

    private void MoveCursorRight()
    {
        if (_cursorColumn < COLUMNS - 1)
        {
            _cursorColumn++;
        }
    }

    private void MoveCursorUp()
    {
        if (_cursorRow > 0)
        {
            _cursorRow--;
        }
    }

    private void MoveCursorDown()
    {
        if (_cursorRow < ROWS - 1)
        {
            _cursorRow++;
        }
    }

    private void Backspace()
    {
        if (_cursorColumn == 0)
            return;

        _cursorColumn--;

        Delete();
    }

    private void Delete()
    {
        for (var column = _cursorColumn;
            column < COLUMNS - 1;
            column++)
        {
            _cells[_cursorRow, column] =
                _cells[_cursorRow, column + 1];
        }

        _cells[_cursorRow, COLUMNS - 1] =
            new ScreenCell(
                ' ',
                _foreground,
                _background);
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
                _cells[row, column] = new ScreenCell(' ',_foreground, _background);
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

    public void WriteAt(int x,int y,string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var column = x + i;

            if (column < 0 || column >= COLUMNS ||
                y < 0 || y >= ROWS)
            {
                continue;
            }

            _cells[y, column] = new ScreenCell(text[i], _foreground, _background);
        }
    }

    private void PutCharacter(char character)
    {
        // Shift everything to the right of the cursor
        // one character to make room.
        for (var column = COLUMNS - 1;
            column > _cursorColumn;
            column--)
        {
            _cells[_cursorRow, column] =
                _cells[_cursorRow, column - 1];
        }

        _cells[_cursorRow, _cursorColumn] =
            new ScreenCell(
                character,
                _foreground,
                _background);

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
                _cells[row - 1, column] = new ScreenCell(_cells[row, column].Character, _foreground, _background);
            }
        }

        for (var column = 0; column < COLUMNS; column++)
        {
            _cells[ROWS - 1, column] = new ScreenCell(' ', _foreground, _background);
        }
    }

    public void Draw(SpriteBatch spriteBatch,BitmapFont font,Texture2D pixel,Color foregroundColor,
            Color backgroundColor,bool drawBackground = true, bool drawCursor = true)
    {
        for (var row = 0; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                var cell = _cells[row, column];

                var foreground =
                    CentauriPalette.Get(cell.Foreground);

                var background =
                    CentauriPalette.Get(cell.Background);

                var drawX =
                    SCREEN_OFFSET_X +
                    column * CHARACTER_WIDTH;

                var drawY =
                    SCREEN_OFFSET_Y +
                    row * CHARACTER_HEIGHT;

                var cellRectangle = new Rectangle(
                    drawX,
                    drawY,
                    CHARACTER_WIDTH,
                    CHARACTER_HEIGHT);

                if (drawBackground)
                {       
                    spriteBatch.Draw(
                        pixel,
                        cellRectangle,
                        background);
                }

                if (cell.Character == ' ')
                    continue;

                font.DrawCharacter(
                    spriteBatch,
                    cell.Character,
                    new Vector2(drawX, drawY),
                    foreground);
            }
        }

        if (drawCursor && _cursorVisible)
        {
            var cursorX =
                SCREEN_OFFSET_X +
                _cursorColumn * CHARACTER_WIDTH;

            var cursorY =
                SCREEN_OFFSET_Y +
                _cursorRow * CHARACTER_HEIGHT;

            var cursorPosition =
                new Vector2(cursorX, cursorY);

            var cursorRectangle = new Rectangle(
                cursorX,
                cursorY,
                CHARACTER_WIDTH,
                CHARACTER_HEIGHT);

            spriteBatch.Draw(
                pixel,
                cursorRectangle,
                foregroundColor);

            var character =
                _cells[_cursorRow, _cursorColumn].Character;

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

    private string GetCurrentLine()
    {
        var lineLength = GetLineLength(_cursorRow);

        var characters = new char[lineLength];

        for (var column = 0;
            column < lineLength;
            column++)
        {
            characters[column] =
                _cells[_cursorRow, column].Character;
        }

        return new string(characters).TrimEnd();
    }

    public void ClearScreenReady()
    {
        Clear();

        WriteLine("");
        WriteLine("READY.");
    }
}