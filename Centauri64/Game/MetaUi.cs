using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Centauri64.Game;

/// <summary>
/// Host/meta UI logical space for physical objects (magazines, catalogues).
/// Not a Centauri64 display mode. 4:3, 2× the 640×480 bedroom layout.
/// </summary>
public static class MetaUi
{
    public const int Width = 1280;
    public const int Height = 960;

    public static Rectangle Destination(int viewportWidth, int viewportHeight)
    {
        var scale = Scale(viewportWidth, viewportHeight);
        var width = (int)(Width * scale);
        var height = (int)(Height * scale);
        return new Rectangle(
            (viewportWidth - width) / 2,
            (viewportHeight - height) / 2,
            width,
            height);
    }

    public static float Scale(int viewportWidth, int viewportHeight) =>
        MathF.Min(viewportWidth / (float)Width, viewportHeight / (float)Height);

    public static Matrix Transform(Rectangle destination) =>
        Matrix.CreateScale(destination.Width / (float)Width, destination.Height / (float)Height, 1f) *
        Matrix.CreateTranslation(destination.X, destination.Y, 0f);

    public static MouseState ToLogical(MouseState mouse, Rectangle destination)
    {
        var scaleX = destination.Width / (float)Width;
        var scaleY = destination.Height / (float)Height;
        var x = (int)((mouse.X - destination.X) / scaleX);
        var y = (int)((mouse.Y - destination.Y) / scaleY);
        return new MouseState(
            x,
            y,
            mouse.ScrollWheelValue,
            mouse.LeftButton,
            mouse.MiddleButton,
            mouse.RightButton,
            mouse.XButton1,
            mouse.XButton2);
    }
}
