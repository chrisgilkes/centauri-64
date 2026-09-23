using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Console;
using Centauri64.Basic;
using Centauri64.Machine;
using Centauri64.Game;

namespace Centauri64;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const int WINDOW_SCALE   = 2;

    private RenderTarget2D _codeEditorRenderTarget = null!;

    private RenderTarget2D _gameRenderTarget = null!;

    private BitmapFont _font = null!;

    private TextConsole _console = null!;
    private TextConsole _programConsole = null!;

    private Texture2D _pixel = null!;

    private BasicMachine _basicMachine = null!;

    private CentauriMachine _machine = null!;

    private KeyboardState _previousKeyboardState;

    private bool _finishedProgramInputArmed;

    private GameMode _gameMode = GameMode.ComputerRoom;

    private bool _waitForInputRelease;

    private bool _computerPoweringOn;
    private double _powerOnTimer;

    private const double POWER_ON_DELAY = 0.5;

    private ComputerRoom _computerRoom;

    private ProgrammingManual _programmingManual = null!;

    private static readonly Color EditorBackground = new(205, 198, 170);

    private static readonly Color EditorFrame = CentauriPalette.Get(17); // Navy

    private static readonly Color EditorText = CentauriPalette.Get(16); // Midnight Blue

    private static readonly Color EditorAccent = CentauriPalette.Get(3); // Teal

    private const int CODE_EDITOR_ROWS = 55;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = CentauriMachine.DEVELOPMENT_WIDTH * WINDOW_SCALE;

        _graphics.PreferredBackBufferHeight = CentauriMachine.DEVELOPMENT_HEIGHT * WINDOW_SCALE;

        _graphics.ApplyChanges();

        Window.Title = "Centauri64";

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch    = new SpriteBatch(GraphicsDevice);

        _codeEditorRenderTarget =new RenderTarget2D(
        GraphicsDevice,
        CentauriMachine.DEVELOPMENT_WIDTH,
        CentauriMachine.DEVELOPMENT_HEIGHT);

        _gameRenderTarget =new RenderTarget2D(
        GraphicsDevice,
        CentauriMachine.GAME_WIDTH,
        CentauriMachine.GAME_HEIGHT);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        var fontTexture = Content.Load<Texture2D>("Fonts/centauri64-font");

        _font           = new BitmapFont(fontTexture);

        _console        = new TextConsole(80, CODE_EDITOR_ROWS);
        _programConsole = new TextConsole(80, 60);

        // Development editor palette.
        _console.Foreground = 16; // Midnight Blue
        _console.Background = 31; // Warm White
        _console.Clear();


        _machine = new CentauriMachine(_console,_programConsole);

        _basicMachine = new BasicMachine(_console,_machine);

        _computerRoom = new ComputerRoom(_font,_pixel);

        _programmingManual = new ProgrammingManual(_font,_pixel);

        _computerRoom.ComputerSelected += OnComputerSelected;

        _computerRoom.ManualSelected += () =>
        {
            _gameMode = GameMode.ProgrammingManual;
        };

        _programmingManual.ExitSelected += () =>
        {
            _gameMode = GameMode.ComputerRoom;
        };

    }

    private void OnComputerSelected()
    {
        _gameMode = GameMode.Computer;
        _waitForInputRelease = true;

        // Already powered on - simply return to the computer.
        if (_computerRoom.ComputerPoweredOn)
        {
            return;
        }

        // First use - perform the real power-on sequence.
        _computerRoom.SetComputerPoweredOn();

        _computerPoweringOn = true;
        _powerOnTimer = 0.0;

        _machine.ResetDisplay();

        _machine.Beep(440, 100);
    }

    private void SetWindowScale(int scale)
    {
        if (_graphics.IsFullScreen)
            _graphics.ToggleFullScreen();

        _graphics.PreferredBackBufferWidth =
            CentauriMachine.DEVELOPMENT_WIDTH * scale;

        _graphics.PreferredBackBufferHeight =
            CentauriMachine.DEVELOPMENT_HEIGHT * scale;

        _graphics.ApplyChanges();
    }

    private void ToggleFullscreen()
    {
        _graphics.ToggleFullScreen();
    }

    private bool KeyPressed(KeyboardState current,Keys key)
    {
        return current.IsKeyDown(key) &&
            _previousKeyboardState.IsKeyUp(key);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (_gameMode == GameMode.ComputerRoom)
        {
            _computerRoom.Update();

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_gameMode == GameMode.ProgrammingManual)
        {
            _programmingManual.Update();

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_computerPoweringOn)
        {
            _powerOnTimer +=
                gameTime.ElapsedGameTime.TotalSeconds;

            if (_powerOnTimer >= POWER_ON_DELAY)
            {
                _computerPoweringOn = false;

                _basicMachine.ShowBootMessage();
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_waitForInputRelease)
        {
            if (keyboardState.GetPressedKeyCount() == 0)
            {
                _waitForInputRelease = false;
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (KeyPressed(keyboardState, Keys.F12))
        {
            _gameMode = GameMode.ComputerRoom;

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_basicMachine.IsRunning &&
            keyboardState.IsKeyDown(Keys.Escape))
        {
            _basicMachine.Stop();

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        if (_basicMachine.ProgramFinished)
        {
            // The key that launched RUN may still be held.
            // Don't allow dismissal until every key has been released.
            if (!_finishedProgramInputArmed)
            {
                if (keyboardState.GetPressedKeyCount() == 0)
                {
                    _finishedProgramInputArmed = true;
                }

                _previousKeyboardState = keyboardState;

                base.Update(gameTime);
                return;
            }

            // We've seen a completely released keyboard.
            // The next key press dismisses the finished program.
            if (keyboardState.GetPressedKeyCount() > 0)
            {
                _basicMachine.DismissFinishedProgram();

                _finishedProgramInputArmed = false;
                _waitForInputRelease = true;

                _previousKeyboardState = keyboardState;

                base.Update(gameTime);
                return;
            }

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

        _machine.UpdateInput();

        if (KeyPressed(keyboardState, Keys.F1))
            SetWindowScale(1);

        if (KeyPressed(keyboardState, Keys.F2))
            SetWindowScale(2);

        if (KeyPressed(keyboardState, Keys.F3))
            SetWindowScale(3);

        if (KeyPressed(keyboardState, Keys.F4))
            SetWindowScale(4);

        if ( KeyPressed(keyboardState, Keys.F5))
            _machine.SpriteEditor.Open("PLAYER");

        if (KeyPressed(keyboardState, Keys.F11))
            ToggleFullscreen();

        var altDown =
            keyboardState.IsKeyDown(Keys.LeftAlt) ||
            keyboardState.IsKeyDown(Keys.RightAlt);

        if (altDown &&
            KeyPressed(keyboardState, Keys.Enter))
        {
            ToggleFullscreen();
        }

        if (!_basicMachine.IsRunning && !_machine.SpriteEditor.IsActive)
        {
            _console.Update(gameTime);
        }

        _basicMachine.Update();

        if (_machine.SpriteEditor.IsActive)
        {
            var mouse = Mouse.GetState();

           var scaleX = GraphicsDevice.Viewport.Width / (float)CentauriMachine.DEVELOPMENT_WIDTH;

            var scaleY = GraphicsDevice.Viewport.Height / (float)CentauriMachine.DEVELOPMENT_HEIGHT;

            var virtualMouse =
                new MouseState(
                    (int)(mouse.X / scaleX),
                    (int)(mouse.Y / scaleY),
                    mouse.ScrollWheelValue,
                    mouse.LeftButton,
                    mouse.MiddleButton,
                    mouse.RightButton,
                    mouse.XButton1,
                    mouse.XButton2);

            _machine.UpdateSpriteEditor(gameTime, virtualMouse,keyboardState,_previousKeyboardState);
        }

        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    private void DrawComputerRoom()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _computerRoom.Draw(_spriteBatch);

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    private void DrawProgrammingManual()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _programmingManual.Draw(_spriteBatch);

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_computerPoweringOn)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ComputerRoom)
        {
            DrawComputerRoom();

            base.Draw(gameTime);
            return;
        }

        if (_gameMode == GameMode.ProgrammingManual)
        {
            DrawProgrammingManual();

            base.Draw(gameTime);
            return;
        }

        if (_basicMachine.IsRunning || _basicMachine.ProgramFinished)
        {
            switch (_machine.DisplayMode)
            {
                case CentauriDisplayMode.HighResolution:
                    DrawTextMode();
                    break;

                case CentauriDisplayMode.Arcade:
                    DrawGame();
                    break;
            }
        }
        else
        {
            DrawCodeEditor();
        }

        base.Draw(gameTime);
    }

    private void DrawTextMode()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _programConsole.Draw(
                                _spriteBatch,
                                _font,
                                _pixel,
                                Color.White,
                                new Color(40, 40, 160),
                                true,
                                !_basicMachine.IsRunning,
                                visibleRows: 60);

        _machine.DrawGraphics(
            _spriteBatch,
            _pixel);

        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        _machine.DrawText(
            _spriteBatch,
            _font);

        if (_basicMachine.ProgramFinished)
        {
            DrawProgramFinishedPrompt();
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _codeEditorRenderTarget);
    }

    private void DrawCodeEditor()
    {
        GraphicsDevice.SetRenderTarget(
            _codeEditorRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        if (_machine.SpriteEditor.IsActive)
        {
            _machine.DrawSpriteEditor(
                _spriteBatch,
                _font,
                _pixel);
        }
        else
        {
            DrawEditorChrome();

           _console.Draw(_spriteBatch,_font,_pixel,EditorText,EditorBackground,drawBackground: false,drawCursor: true,offsetY: 24,  visibleRows: CODE_EDITOR_ROWS );

            _machine.DrawSprites(
                _spriteBatch,
                _pixel);
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(_codeEditorRenderTarget);
    }

    private void DrawGame()
    {
        GraphicsDevice.SetRenderTarget(
            _gameRenderTarget);

        // BORDER
        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.PaperColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        // PAPER
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,0,
                CentauriMachine.ARCADE_WIDTH,
                CentauriMachine.ARCADE_HEIGHT),
            CentauriPalette.Get(_programConsole.Background));

        _programConsole.Draw(_spriteBatch,_font,_pixel,Color.White,CentauriPalette.Get(_programConsole.Background),drawBackground: false, drawCursor: !_basicMachine.IsRunning, visibleRows: 30);

        // GRAPHICS
        _machine.DrawGraphics(
            _spriteBatch,
            _pixel);

        // SPRITES
        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        // HUD / PRINTAT
        _machine.DrawText(
            _spriteBatch,
            _font);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _gameRenderTarget);
    }

    private void DrawRenderTargetToWindow(RenderTarget2D renderTarget)
    {
        GraphicsDevice.Clear(Color.Black);

        var viewportWidth =
            GraphicsDevice.Viewport.Width;

        var viewportHeight =
            GraphicsDevice.Viewport.Height;

        var scaleX =
            viewportWidth / (float)renderTarget.Width;

        var scaleY =
            viewportHeight / (float)renderTarget.Height;

        var scale =
            MathF.Min(scaleX, scaleY);

        var destinationWidth =
            (int)(renderTarget.Width * scale);

        var destinationHeight =
            (int)(renderTarget.Height * scale);

        var destinationX =
            (viewportWidth - destinationWidth) / 2;

        var destinationY =
            (viewportHeight - destinationHeight) / 2;

        var destinationRectangle =
            new Rectangle(
                destinationX,
                destinationY,
                destinationWidth,
                destinationHeight);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(
            renderTarget,
            destinationRectangle,
            Color.White);

        _spriteBatch.End();
    }

    private void DrawProgramFinishedPrompt()
    {
        const string prompt = "[PRESS ANY KEY TO RETURN]";

        _font.Draw(
            _spriteBatch,
            prompt,
            new Vector2(224, 376),
            new Color(160, 160, 160));
    }

    private void DrawEditorChrome()
    {
        // Main editor background.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                CentauriMachine.DEVELOPMENT_HEIGHT),
            EditorBackground);

        // Header.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                0,
                CentauriMachine.DEVELOPMENT_WIDTH,
                24),
            EditorFrame);

       _font.Draw(
            _spriteBatch,
            "CENTAURI64 BASIC",
            new Vector2(16, 8),
            Color.White);

        var memoryText =
            $"{_basicMachine.BasicMemoryFree} BASIC BYTES FREE";

        var memoryX =
            (CentauriMachine.DEVELOPMENT_WIDTH -
            memoryText.Length * 8) / 2;

        _font.Draw(
            _spriteBatch,
            memoryText,
            new Vector2(memoryX, 8),
            EditorAccent);

        _font.Draw(
            _spriteBatch,
            "64K",
            new Vector2(
                CentauriMachine.DEVELOPMENT_WIDTH - 40,
                8),
            EditorAccent);

        // Footer.
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                0,
                CentauriMachine.DEVELOPMENT_HEIGHT - 16,
                CentauriMachine.DEVELOPMENT_WIDTH,
                16),
            EditorFrame);

        _font.Draw(
            _spriteBatch,
            "EDIT",
            new Vector2(
                16,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            EditorAccent);

        _font.Draw(
            _spriteBatch,
            "F5 SPRITES",
            new Vector2(
                248,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            Color.White);

        _font.Draw(
            _spriteBatch,
            "F12 BEDROOM",
            new Vector2(
                520,
                CentauriMachine.DEVELOPMENT_HEIGHT - 12),
            Color.White);
    }
}
