using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Centauri64.Console;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;

using Centauri64.Machine.Sprites;
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

    private readonly BasicMachine _basic;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    public const int SCREEN_WIDTH = 640;
    public const int SCREEN_HEIGHT = 400;

    public const int BORDER_SIZE = 8;

    public const int DEVELOPMENT_WIDTH = SCREEN_WIDTH + BORDER_SIZE * 2;
    public const int DEVELOPMENT_HEIGHT = SCREEN_HEIGHT + BORDER_SIZE * 2;

    public const int ARCADE_WIDTH = 320;
    public const int ARCADE_HEIGHT = 180;

    public const int GAME_WIDTH = ARCADE_WIDTH + BORDER_SIZE * 2;

    public const int GAME_HEIGHT =ARCADE_HEIGHT + BORDER_SIZE * 2;

    public int BorderColour { get; private set; } = DEFAULT_BORDER;

    public const int DEFAULT_INK = 1;
    public const int DEFAULT_PAPER = 0;
    public const int DEFAULT_BORDER = 6;

    private CentauriDisplayMode _displayMode = CentauriDisplayMode.HighResolution;

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

    public CentauriMachine(TextConsole console, TextConsole programConsole)
    {
        _console        = console;
        _programConsole = programConsole;

        for (var i = 0; i < MAX_SPRITES; i++)
        {
            _sprites[i] = new CentauriSprite();
        }

        _spriteEditor = new SpriteEditor(_spriteAssets);

        CreateBuiltInSpriteAssets();
    }

    public void SetDisplayMode(int mode)
    {
        if (!Enum.IsDefined(typeof(CentauriDisplayMode), mode))
        {
            throw new InvalidOperationException($"Unsupported display mode {mode}.");
        }

        _displayMode = (CentauriDisplayMode)mode;
    }

    private static Keys? GetKey(string keyName)
    {
        return keyName switch
        {
            "LEFT" => Keys.Left,
            "RIGHT" => Keys.Right,
            "UP" => Keys.Up,
            "DOWN" => Keys.Down,
            "SPACE" => Keys.Space,
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

    public void ClearScreen()
    {
        _programConsole.Clear();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();
        _circles.Clear();
    }

    public void SetInk(int colour)
    {
        ValidateColour(colour);

        _programConsole.Foreground = colour;
    }

    public void SetBorder(int colour)
    {
        ValidateColour(colour);

        BorderColour = colour;
    }

    public void SetPaper(int colour)
    {
        ValidateColour(colour);

        _programConsole.Background = colour;
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
        BorderColour        = DEFAULT_BORDER;

        _console.Clear();
    }

    public void ResetProgramDisplay()
    {
        _programConsole.Foreground  = DEFAULT_INK;
        _programConsole.Background  = DEFAULT_PAPER;
        BorderColour                = DEFAULT_BORDER;

        _programConsole.Clear();
        _positionedText.Clear();
        _plotPoints.Clear();
        _lines.Clear();
        _rectangles.Clear();
        _circles.Clear();

        _displayMode = CentauriDisplayMode.HighResolution;
    }

    public void UpdateInput()
    {
        _previousKeyboardState = _keyboardState;
        _keyboardState = Keyboard.GetState();
    }

}