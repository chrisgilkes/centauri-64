using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;

namespace Centauri64;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const int VIRTUAL_WIDTH  = 640;
    private const int VIRTUAL_HEIGHT = 400;
    private const int WINDOW_SCALE   = 2;

    private RenderTarget2D _renderTarget;

    private BitmapFont _font = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth  = VIRTUAL_WIDTH * WINDOW_SCALE;
        _graphics.PreferredBackBufferHeight = VIRTUAL_HEIGHT * WINDOW_SCALE;
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

        _renderTarget   = new RenderTarget2D(GraphicsDevice,VIRTUAL_WIDTH,VIRTUAL_HEIGHT);

        var fontTexture = Content.Load<Texture2D>("Fonts/centauri64-font");
        _font           = new BitmapFont(fontTexture);

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);

        GraphicsDevice.Clear(new Color(40, 40, 160));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _font.Draw(_spriteBatch,"CENTAURI64",new Vector2(8, 8),Color.White);

        _font.Draw(_spriteBatch,"READY.",new Vector2(8, 24),Color.White);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(_renderTarget,
                            new Rectangle(
                                0,
                                0,
                                VIRTUAL_WIDTH * WINDOW_SCALE,
                                VIRTUAL_HEIGHT * WINDOW_SCALE),
                            Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
