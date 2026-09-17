using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Console;
using Centauri64.Basic;
using Centauri64.Machine;

namespace Centauri64;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const int WINDOW_SCALE   = 2;

    private RenderTarget2D _renderTarget;

    private BitmapFont _font = null!;

    private TextConsole _console = null!;

    private Texture2D _pixel = null!;

    private BasicMachine _basicMachine = null!;

    private CentauriMachine _machine = null!;

    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth  = CentauriMachine.DISPLAY_WIDTH * WINDOW_SCALE;

        _graphics.PreferredBackBufferHeight = CentauriMachine.DISPLAY_HEIGHT * WINDOW_SCALE;

        _graphics.ApplyChanges();

        Window.Title = "Centauri64";

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch    = new SpriteBatch(GraphicsDevice);

        _renderTarget = new RenderTarget2D(GraphicsDevice,CentauriMachine.DISPLAY_WIDTH,CentauriMachine.DISPLAY_HEIGHT);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        var fontTexture = Content.Load<Texture2D>("Fonts/centauri64-font");
        _font           = new BitmapFont(fontTexture);

        _console = new TextConsole();

        _console.WriteLine("CENTAURI64");
        _console.WriteLine("");
        _console.WriteLine("READY.");

        _machine = new CentauriMachine(_console);
        _machine.CreateTestSprite();

        _basicMachine = new BasicMachine(_console, _machine);

    }

    private void SetWindowScale(int scale)
    {
        if (_graphics.IsFullScreen)
            _graphics.ToggleFullScreen();

        _graphics.PreferredBackBufferWidth =
            CentauriMachine.DISPLAY_WIDTH * scale;

        _graphics.PreferredBackBufferHeight =
            CentauriMachine.DISPLAY_HEIGHT * scale;

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
        /*if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();*/

        var keyboardState = Keyboard.GetState();

        if (KeyPressed(keyboardState, Keys.F1))
            SetWindowScale(1);

        if (KeyPressed(keyboardState, Keys.F2))
            SetWindowScale(2);

        if (KeyPressed(keyboardState, Keys.F3))
            SetWindowScale(3);

        if (KeyPressed(keyboardState, Keys.F4))
            SetWindowScale(4);

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
            if (keyboardState.IsKeyDown(Keys.Escape) && _previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                _basicMachine.Stop();
            }
        }
        else
        {
            _console.Update(gameTime);
        }

        _basicMachine.Update();

        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);

        GraphicsDevice.Clear(CentauriPalette.Get(_machine.BorderColour));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _console.Draw(_spriteBatch,_font,_pixel,Color.White,new Color(40, 40, 160));

        _machine.DrawSprites(
            _spriteBatch,
            _pixel);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(_renderTarget,
                            new Rectangle(
                                0,
                                0,
                                GraphicsDevice.Viewport.Width,
                                GraphicsDevice.Viewport.Height),
                            Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
