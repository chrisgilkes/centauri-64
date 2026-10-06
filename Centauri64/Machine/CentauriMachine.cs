using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;

using Centauri64.Machine.Sprites;
using Centauri64.Machine.Maps;
using Centauri64.Graphics;
using Centauri64.Basic;

using Centauri64.Machine.Audio;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    public const int SYSTEM_MEMORY_BYTES = 64 * 1024;
    public const int BASIC_MEMORY_BYTES = 48 * 1024;

    private readonly TextConsole _console;
    private readonly TextConsole _programConsole;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 480;

    public const int DEVELOPMENT_WIDTH = SCREEN_WIDTH;
    public const int DEVELOPMENT_HEIGHT = SCREEN_HEIGHT;

    public const int ARCADE_WIDTH = 320;
    public const int ARCADE_HEIGHT = 240;

    public const int GAME_WIDTH = ARCADE_WIDTH;

    public const int GAME_HEIGHT =ARCADE_HEIGHT;

    public const int DEFAULT_INK = 1;
    public const int DEFAULT_PAPER = 0;

    private CentauriDisplayMode _displayMode = CentauriDisplayMode.Console;

    private int _paperColour = DEFAULT_PAPER;

    public CentauriDisplayMode DisplayMode => _displayMode;

    public int SystemMemoryBytes => SYSTEM_MEMORY_BYTES;

    public int BasicMemoryBytes => BASIC_MEMORY_BYTES;

    public int ScreenWidth => _displayMode switch
    {
        CentauriDisplayMode.HighResolution => SCREEN_WIDTH,
        CentauriDisplayMode.Arcade => ARCADE_WIDTH,
        _ => SCREEN_WIDTH
    };

    public int ScreenHeight => _displayMode switch
    {
        CentauriDisplayMode.HighResolution => SCREEN_HEIGHT,
        CentauriDisplayMode.Arcade => ARCADE_HEIGHT,
        _ => SCREEN_HEIGHT
    };

    public int PaperColour => _paperColour;

    public CentauriMachine(TextConsole console, TextConsole programConsole)
    {
        _console        = console;
        _programConsole = programConsole;

        for (var i = 0; i < MAX_SPRITES; i++)
        {
            _sprites[i] = new CentauriSprite();
        }

        _spriteEditor = new SpriteEditor(_spriteAssets);
        _mapEditor = new MapEditor(_mapAssets, _spriteAssets);
        CreateImageEditor();

        CreateBuiltInSpriteAssets();
    }

    public void ResetEditorAssets()
    {
        _spriteAssets.Clear();
        _mapAssets.Clear();
        _imageAssets.Clear();
        ClearImageLayers();
        CreateBuiltInSpriteAssets();
        _spriteEditor.MarkSaved();
        _mapEditor.MarkSaved();
        _imageEditor.MarkSaved();
    }

    public void EnsureBuiltInSpriteAssets()
    {
        CreateBuiltInSpriteAssets();
    }

    /// <summary>
    /// BASIC MODE command. Only graphics modes are accepted:
    /// 1 = High Resolution (640×480), 2 = Arcade (320×240).
    /// The default BASIC console is not a MODE number.
    /// </summary>
    public void SetDisplayMode(int mode)
    {
        _displayMode = mode switch
        {
            1 => CentauriDisplayMode.HighResolution,
            2 => CentauriDisplayMode.Arcade,
            _ => throw new InvalidOperationException($"Unsupported display mode {mode}.")
        };
    }

    /// <summary>
    /// Leave any graphics MODE and return to the default BASIC environment
    /// without clearing the editor console.
    /// </summary>
    public void ReturnToBasicEnvironment()
    {
        _displayMode = CentauriDisplayMode.Console;
    }

    private static Keys? GetKey(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return null;

        var name = keyName.Trim().ToUpperInvariant();

        return name switch
        {
            "LEFT" => Keys.Left,
            "RIGHT" => Keys.Right,
            "UP" => Keys.Up,
            "DOWN" => Keys.Down,
            "SPACE" => Keys.Space,
            "ENTER" or "RETURN" => Keys.Enter,
            "ESC" or "ESCAPE" => Keys.Escape,
            "0" => Keys.D0,
            "1" => Keys.D1,
            "2" => Keys.D2,
            "3" => Keys.D3,
            "4" => Keys.D4,
            "5" => Keys.D5,
            "6" => Keys.D6,
            "7" => Keys.D7,
            "8" => Keys.D8,
            "9" => Keys.D9,
            _ when name.Length == 1 && name[0] is >= 'A' and <= 'Z'
                => Keys.A + (name[0] - 'A'),
            _ => null
        };
    }

    public bool IsKeyDown(string keyName)
    {
        var key = GetKey(keyName);

        return key.HasValue &&
            _keyboardState.IsKeyDown(key.Value);
    }

    public bool IsKeyPressed(string keyName)
    {
        var key = GetKey(keyName);

        if (!key.HasValue)
            return false;

        return _keyboardState.IsKeyDown(key.Value) && _previousKeyboardState.IsKeyUp(key.Value);
    }

    /// <summary>
    /// Text console used for PRINT/INPUT/INK/PAPER when in the default
    /// BASIC environment; otherwise the dedicated program console.
    /// </summary>
    private TextConsole ActiveTextConsole =>
        _displayMode == CentauriDisplayMode.Console
            ? _console
            : _programConsole;

    public void ClearScreen()
    {
        ActiveTextConsole.Clear();
        ActiveTextConsole.CancelInput();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();
        _circles.Clear();
        ClearImageBlits();
        ClearTileMap();
    }

    public void SetInk(int colour)
    {
        ValidateColour(colour);

        ActiveTextConsole.Foreground = colour;
    }

    public void SetPaper(int colour)
    {
        ValidateColour(colour);

        _paperColour = colour;
        ActiveTextConsole.Background = colour;
    }

    private static void ValidateColour(int colour)
    {
        if (colour < 0 ||
            colour >= CentauriPalette.MAX_COLORS)
        {
            throw new InvalidOperationException(
                $"Colour must be between 0 and {CentauriPalette.MAX_COLORS - 1}.");
        }
    }

    public void ResetDisplay()
    {
        _console.Foreground = DEFAULT_INK;
        _console.Background = DEFAULT_PAPER;

        _console.Clear();
    }

    public void ResetProgramDisplay()
    {
        _paperColour = DEFAULT_PAPER;
        _programConsole.Foreground  = DEFAULT_INK;
        _programConsole.Background  = DEFAULT_PAPER;

        _programConsole.Clear();
        _programConsole.CancelInput();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();
        _circles.Clear();

        ClearTiles();
        ClearImageLayers();

        _displayMode = CentauriDisplayMode.Console;
    }

    public void UpdateInput()
    {
        _previousKeyboardState = _keyboardState;
        _keyboardState = Keyboard.GetState();
    }

}