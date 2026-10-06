using System;

using Centauri64.Graphics;
using Centauri64.Machine;
using Centauri64.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

public sealed class BundleSelectScreen
{
    private readonly BitmapFont _font;
    private readonly Texture2D _pixel;
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private int _index;

    public event Action<string>? BundleChosen;
    public event Action? Cancelled;

    public BundleSelectScreen(BitmapFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Open()
    {
        _index = 0;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void Update(GameTime gameTime, MouseState mouse)
    {
        var keyboard = Keyboard.GetState();
        var count = ComputerBundleCatalog.Bundles.Length;

        if (Pressed(keyboard, Keys.Escape))
        {
            Cancelled?.Invoke();
            _previous = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (Pressed(keyboard, Keys.Left))
            _index = (_index + count - 1) % count;

        if (Pressed(keyboard, Keys.Right))
            _index = (_index + 1) % count;

        HandleMouse(mouse);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);

        _previous = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        BootUi.DrawBox(spriteBatch, _pixel,
            new Rectangle(0, 0, CentauriMachine.DEVELOPMENT_WIDTH, CentauriMachine.DEVELOPMENT_HEIGHT),
            BootUi.Background);
        BootUi.DrawBox(spriteBatch, _pixel, new Rectangle(16, 16, 608, 40), BootUi.Header);
        BootUi.DrawText(_font, spriteBatch, "CENTAURI COMPUTER CENTRE  1986", 160, 28, Color.White);
        BootUi.DrawText(_font, spriteBatch, "CHOOSE YOUR CENTAURI64 PACK", 176, 72, BootUi.Yellow);

        var bundle = ComputerBundleCatalog.Bundles[_index];
        var panel = new Rectangle(80, 104, 480, 272);
        BootUi.DrawBox(spriteBatch, _pixel, panel, BootUi.Panel);
        BootUi.DrawBorder(spriteBatch, _pixel, panel, BootUi.Yellow);

        BootUi.DrawText(_font, spriteBatch, bundle.Name, 112, 124, Color.White);
        BootUi.DrawText(_font, spriteBatch, bundle.Price, 480, 124, BootUi.Yellow);
        BootUi.DrawText(_font, spriteBatch, bundle.Tagline, 112, 148, BootUi.Cyan);
        BootUi.DrawText(_font, spriteBatch, "INCLUDED:", 112, 180, BootUi.Muted);

        var y = 204;
        foreach (var item in bundle.Included)
        {
            BootUi.DrawText(_font, spriteBatch, "* " + item, 128, y, BootUi.Cream);
            y += 20;
        }

        BootUi.DrawText(_font, spriteBatch, "<  PREVIOUS PACK          NEXT PACK  >", 136, 392, BootUi.Cyan);
        BootUi.DrawText(_font, spriteBatch, "ENTER SELECT PACK    ESC BACK", 184, 432, BootUi.Muted);
        spriteBatch.End();
    }

    private void HandleMouse(MouseState mouse)
    {
        var clicked = mouse.LeftButton == ButtonState.Pressed &&
                      _previousMouse.LeftButton == ButtonState.Released;
        if (!clicked)
            return;

        var count = ComputerBundleCatalog.Bundles.Length;
        if (new Rectangle(80, 380, 200, 24).Contains(mouse.X, mouse.Y))
            _index = (_index + count - 1) % count;
        else if (new Rectangle(360, 380, 200, 24).Contains(mouse.X, mouse.Y))
            _index = (_index + 1) % count;
        else if (new Rectangle(80, 104, 480, 272).Contains(mouse.X, mouse.Y))
            BundleChosen?.Invoke(ComputerBundleCatalog.Bundles[_index].Id);
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
