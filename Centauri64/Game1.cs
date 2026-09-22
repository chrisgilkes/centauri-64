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

    private RenderTarget2D _developmentRenderTarget = null!;

    private RenderTarget2D _gameRenderTarget = null!;

    private BitmapFont _font = null!;

    private TextConsole _console = null!;
    private TextConsole _programConsole = null!;

    private Texture2D _pixel = null!;

    private BasicMachine _basicMachine = null!;

    private CentauriMachine _machine = null!;

    private KeyboardState _previousKeyboardState;

    private GameMode _gameMode = GameMode.ComputerRoom;

    private bool _waitForInputRelease;

    private bool _computerPoweringOn;
    private double _powerOnTimer;

    private const double POWER_ON_DELAY = 0.5;

    private ComputerRoom _computerRoom;

    private ProgrammingManual _programmingManual = null!;

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

        _developmentRenderTarget =new RenderTarget2D(
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

        _console        = new TextConsole();
        _programConsole = new TextConsole();

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

        _machine.UpdateInput();

        if (KeyPressed(keyboardState, Keys.F12))
        {
            _gameMode = GameMode.ComputerRoom;

            _previousKeyboardState = keyboardState;

            base.Update(gameTime);
            return;
        }

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

        if (_basicMachine.IsRunning)
        {
            if (keyboardState.IsKeyDown(Keys.Escape) &&
                _previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                _basicMachine.Stop();
            }
        }
        else if (!_machine.SpriteEditor.IsActive)
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

            _machine.UpdateSpriteEditor(virtualMouse,keyboardState,_previousKeyboardState);
        }

        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    private void DrawComputerRoom()
    {
        GraphicsDevice.SetRenderTarget(
            _developmentRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _computerRoom.Draw(_spriteBatch);

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _developmentRenderTarget);
    }

    private void DrawProgrammingManual()
    {
        GraphicsDevice.SetRenderTarget(
            _developmentRenderTarget);

        GraphicsDevice.Clear(Color.Black);

        _programmingManual.Draw(_spriteBatch);

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _developmentRenderTarget);
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

        if (_basicMachine.IsRunning)
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
            DrawDevelopment();
        }

        base.Draw(gameTime);
    }

    private void DrawTextMode()
    {
        GraphicsDevice.SetRenderTarget(
            _developmentRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.BorderColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        _programConsole.Draw(
            _spriteBatch,
            _font,
            _pixel,
            Color.White,
            new Color(40, 40, 160), true, !_basicMachine.IsRunning);

        _machine.DrawGraphics(
            _spriteBatch,
            _pixel);

        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        _machine.DrawText(
            _spriteBatch,
            _font);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(
            _developmentRenderTarget);
    }

    private void DrawDevelopment()
    {
        GraphicsDevice.SetRenderTarget(
            _developmentRenderTarget);

        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.BorderColour));

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
            _console.Draw(
                _spriteBatch,
                _font,
                _pixel,
                Color.White,
                new Color(40, 40, 160));

            _machine.DrawSprites(
                _spriteBatch,
                _pixel);
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        DrawRenderTargetToWindow(_developmentRenderTarget);
    }

    private void DrawGame()
    {
        GraphicsDevice.SetRenderTarget(
            _gameRenderTarget);

        // BORDER
        GraphicsDevice.Clear(
            CentauriPalette.Get(_machine.BorderColour));

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        // PAPER
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(
                CentauriMachine.BORDER_SIZE,
                CentauriMachine.BORDER_SIZE,
                CentauriMachine.ARCADE_WIDTH,
                CentauriMachine.ARCADE_HEIGHT),
            CentauriPalette.Get(_programConsole.Background));

        _programConsole.Draw(_spriteBatch,_font,_pixel,Color.White,CentauriPalette.Get(_programConsole.Background),drawBackground: false, drawCursor: !_basicMachine.IsRunning);

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
}
