using System;
using System.Collections.Generic;
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
    public event Action? InputChanged;

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

    public int CursorColumn => _cursorColumn;
    public int CursorRow => _cursorRow;

    private readonly List<string> _inputHistory = new();

    private int _historyIndex = -1;

    private const int MAX_SCROLLBACK_LINES = 256;

    private readonly List<ScreenCell[]> _scrollback = new();

    private int _scrollbackOffset;

    public TextConsole()
    {
        _cells = new ScreenCell[ROWS, COLUMNS];

        Clear();
    }

    public string GetCurrentLine()
    {
        var characters = new char[COLUMNS];

        for (var column = 0; column < COLUMNS; column++)
        {
            characters[column] = _cells[_cursorRow, column].Character;
        }

        return new string(characters).TrimEnd();
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

        var control =
            keyboardState.IsKeyDown(Keys.LeftControl) ||
            keyboardState.IsKeyDown(Keys.RightControl);

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (_previousKeyboardState.IsKeyUp(key))
            {
                HandleKey(key, shift, control);

                if (CanRepeat(key))
                {
                    _repeatingKey = key;
                    _keyRepeatTimer = KEY_REPEAT_DELAY;
                }
            }
        }

        UpdateKeyRepeat(keyboardState,shift,control,gameTime.ElapsedGameTime.TotalSeconds);

        _previousKeyboardState = keyboardState;
    }

    private void UpdateKeyRepeat(KeyboardState keyboardState,bool shift,bool control,double deltaTime)
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
            HandleKey(key, shift, control);
            _keyRepeatTimer += KEY_REPEAT_INTERVAL;
        }
    }

    private void HandleKey(Keys key,  bool shift, bool control)
    {
        if (_scrollbackOffset > 0 && key != Keys.PageUp && key != Keys.PageDown)
        {
            _scrollbackOffset = 0;
        }

        if (key >= Keys.A && key <= Keys.Z)
        {
            var character = (char)('A' + (key - Keys.A));
            PutCharacter(character);
            ResetCursorFlash();
            InputChanged?.Invoke();
            return;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            var offset = key - Keys.D0;

            var character = shift? ShiftedNumbers[offset]: (char)('0' + offset);

            PutCharacter(character);
            ResetCursorFlash();
            InputChanged?.Invoke();
            return;
        }

        var punctuation = GetPunctuation(key, shift);

        if (punctuation.HasValue)
        {
            PutCharacter(punctuation.Value);
            ResetCursorFlash();
            InputChanged?.Invoke();
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
            if (control)
            {
                PreviousHistory();
            }
            else
            {
                MoveCursorUp();
            }

            ResetCursorFlash();
            return;
        }

        if (key == Keys.Down)
        {
            if (control)
            {
                NextHistory();
            }
            else
            {
                MoveCursorDown();
            }

            ResetCursorFlash();
            return;
        }

        if (key == Keys.Space)
        {
            PutCharacter(' ');
            ResetCursorFlash();
            InputChanged?.Invoke();
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

            if (!string.IsNullOrWhiteSpace(line))
            {
                _inputHistory.Add(line);
            }

            _historyIndex = _inputHistory.Count;

            NewLine();

            LineEntered?.Invoke(line);

            ResetCursorFlash();
            return;
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

        if (key == Keys.PageUp)
        {
            PageUp();
            ResetCursorFlash();
            return;
        }

        if (key == Keys.PageDown)
        {
            PageDown();
            ResetCursorFlash();
            return;
        }
        
    }

    private void PageUp()
    {
        if (_scrollback.Count == 0)
            return;

        _scrollbackOffset = Math.Min(
            _scrollbackOffset + 40,
            _scrollback.Count);
    }

    private void PageDown()
    {
        _scrollbackOffset = Math.Max(
            0,
            _scrollbackOffset - 40);
    }

    private void PreviousHistory()
    {
        if (_inputHistory.Count == 0)
            return;

        if (_historyIndex > 0)
            _historyIndex--;

        SetCurrentLine(
            _inputHistory[_historyIndex]);
    }

    private void NextHistory()
    {
        if (_inputHistory.Count == 0)
            return;

        if (_historyIndex < _inputHistory.Count - 1)
        {
            _historyIndex++;

            SetCurrentLine(
                _inputHistory[_historyIndex]);

            return;
        }

        _historyIndex = _inputHistory.Count;

        SetCurrentLine(string.Empty);
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

        InputChanged?.Invoke();
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
            InputChanged?.Invoke();
        }
    }

    public void Write(string text, int foreground)
    {
        var previousForeground = _foreground;

        _foreground = foreground;

        Write(text);

        _foreground = previousForeground;
    }

    public void WriteLine(string text)
    {
        Write(text);
        NewLine();
    }

    public void WriteLine(string text, int foreground)
    {
        Write(text, foreground);
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

    public void SetCellForeground(int column,int row,int foreground)
    {
        if (column < 0 || column >= COLUMNS ||
            row < 0 || row >= ROWS)
        {
            return;
        }

        var cell = _cells[row, column];

        _cells[row, column] =new ScreenCell(cell.Character,foreground,cell.Background);
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
        var scrolledLine = new ScreenCell[COLUMNS];

        for (var column = 0; column < COLUMNS; column++)
        {
            var cell = _cells[0, column];

            scrolledLine[column] =new ScreenCell(cell.Character,cell.Foreground,cell.Background);
        }

        _scrollback.Add(scrolledLine);

        if (_scrollback.Count > MAX_SCROLLBACK_LINES)
        {
            _scrollback.RemoveAt(0);
        }

        for (var row = 1; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                var sourceCell = _cells[row, column];
                _cells[row - 1, column] = new ScreenCell(sourceCell.Character,sourceCell.Foreground,sourceCell.Background);
            }
        }

        for (var column = 0; column < COLUMNS; column++)
        {
            _cells[ROWS - 1, column] =new ScreenCell(' ',_foreground,_background);
        }
    }

    private ScreenCell GetDisplayCell(int row,int column)
    {
        if (_scrollbackOffset == 0)
        {
            return _cells[row, column];
        }

        var historyStart = _scrollback.Count - _scrollbackOffset;

        var historyIndex =
            historyStart + row;

        if (historyIndex >= 0 &&
            historyIndex < _scrollback.Count)
        {
            return _scrollback[historyIndex][column];
        }

        var liveRow =
            historyIndex - _scrollback.Count;

        if (liveRow >= 0 &&
            liveRow < ROWS)
        {
            return _cells[liveRow, column];
        }

        return new ScreenCell(
            ' ',
            _foreground,
            _background);
    }

    public void Draw(SpriteBatch spriteBatch,BitmapFont font,Texture2D pixel,Color foregroundColor,
            Color backgroundColor,bool drawBackground = true, bool drawCursor = true)
    {
        for (var row = 0; row < ROWS; row++)
        {
            for (var column = 0; column < COLUMNS; column++)
            {
                var cell = GetDisplayCell(row, column);

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

        if (drawCursor && _cursorVisible && _scrollbackOffset == 0)
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

    public void ClearScreenReady()
    {
        Clear();

        WriteLine("");
        WriteLine("READY.");
    }

    public void SetCurrentLine(string text)
    {
        // Clear the current row.
        for (var column = 0; column < COLUMNS; column++)
        {
            _cells[_cursorRow, column] =
                new ScreenCell(
                    ' ',
                    _foreground,
                    _background);
        }

        // Write the new text directly into the row.
        var length = Math.Min(text.Length, COLUMNS);

        for (var column = 0; column < length; column++)
        {
            _cells[_cursorRow, column] =
                new ScreenCell(
                    text[column],
                    _foreground,
                    _background);
        }

        // Put the cursor at the end of the line.
        _cursorColumn = Math.Min(length, COLUMNS - 1);

        ResetCursorFlash();
        InputChanged?.Invoke();
    }
}