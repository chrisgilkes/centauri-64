using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    private sealed class PlotPoint
    {
        public int X { get; }
        public int Y { get; }
        public int Colour { get; }

        public PlotPoint(int x, int y, int colour)
        {
            X = x;
            Y = y;
            Colour = colour;
        }
    }

    private sealed class LinePrimitive
    {
        public int X1 { get; }
        public int Y1 { get; }
        public int X2 { get; }
        public int Y2 { get; }
        public int Colour { get; }

        public LinePrimitive(int x1,int y1,int x2,int y2,int colour)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Colour = colour;
        }
    }

    private sealed class RectPrimitive
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Colour { get; }
        public bool Filled { get; }

        public RectPrimitive(int x,int y,int width,int height,int colour,bool filled)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Colour = colour;
            Filled = filled;
        }
    }

    private sealed class CirclePrimitive
    {
        public int X { get; }
        public int Y { get; }
        public int Radius { get; }
        public int Colour { get; }
        public bool Filled { get; }

        public CirclePrimitive(int x,int y,int radius,int colour,bool filled)
        {
            X = x;
            Y = y;
            Radius = radius;
            Colour = colour;
            Filled = filled;
        }
    }
    
    private readonly List<PlotPoint> _plotPoints = new();
    private readonly List<LinePrimitive> _lines = new();

    private readonly List<RectPrimitive> _rectangles = new();
    private readonly List<CirclePrimitive> _circles = new();

    public void Plot(int x, int y, int colour)
    {
        ValidateColour(colour);

        _plotPoints.Add(new PlotPoint(x, y, colour));
    }

    public void Line(int x1,int y1,int x2,int y2,int colour)
    {
        ValidateColour(colour);

        _lines.Add(new LinePrimitive(x1,y1,x2,y2,colour));
    }

    public void Rect(int x,int y,int width,int height,int colour,bool filled)
    {
        ValidateColour(colour);

        _rectangles.Add(new RectPrimitive(x,y,width,height,colour,filled));
    }

    public void Circle(int x,int y,int radius,int colour,bool filled)
    {
        ValidateColour(colour);

        _circles.Add(new CirclePrimitive(x,y,radius,colour,filled));
    }

    public void DrawGraphics(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var point in _plotPoints)
        {
            spriteBatch.Draw(pixel,new Rectangle(point.X,point.Y,1,1),CentauriPalette.Get(point.Colour));
        }

        foreach (var line in _lines)
        {
            DrawLine(spriteBatch,pixel,line.X1,line.Y1,line.X2,line.Y2,CentauriPalette.Get(line.Colour));
        }

        foreach (var rect in _rectangles)
        {
            var colour = CentauriPalette.Get(rect.Colour);

            if (rect.Filled)
            {
                spriteBatch.Draw(pixel,new Rectangle(rect.X,rect.Y,rect.Width,rect.Height),colour);
            }
            else
            {
                var right = rect.X + rect.Width - 1;

                var bottom = rect.Y + rect.Height - 1;

                DrawLine(spriteBatch,pixel,rect.X,rect.Y,right,rect.Y,colour);

                DrawLine(spriteBatch,pixel,right,rect.Y,right,bottom,colour);

                DrawLine(spriteBatch,pixel,right,bottom,rect.X,bottom,colour);

                DrawLine(spriteBatch,pixel,rect.X,bottom,rect.X,rect.Y,colour);
            }
        }

        foreach (var circle in _circles)
        {
            var circleCol = CentauriPalette.Get(circle.Colour);

            if (circle.Filled)
            {
                DrawFilledCircle(spriteBatch,pixel,circle.X,circle.Y,circle.Radius,circleCol);
            }
            else
            {
                DrawCircle(spriteBatch,pixel,circle.X,circle.Y,circle.Radius,circleCol);
            }
        }
    }

    private static void DrawLine(SpriteBatch spriteBatch,Texture2D pixel,int x1,int y1,int x2,int y2,Color colour)
    {
        var dx = Math.Abs(x2 - x1);
        var sx = x1 < x2 ? 1 : -1;

        var dy = -Math.Abs(y2 - y1);
        var sy = y1 < y2 ? 1 : -1;

        var error = dx + dy;

        while (true)
        {
            spriteBatch.Draw(pixel,new Rectangle(x1,y1,1,1),colour);

            if (x1 == x2 && y1 == y2)
            {
                break;
            }

            var error2 = error * 2;

            if (error2 >= dy)
            {
                error += dy;
                x1 += sx;
            }

            if (error2 <= dx)
            {
                error += dx;
                y1 += sy;
            }
        }
    }

    private static void DrawPixel(SpriteBatch spriteBatch,Texture2D pixel,int x,int y,Color colour)
    {
        spriteBatch.Draw(pixel,new Rectangle(x,y,1,1),colour);
    }

    private static void DrawCircle(SpriteBatch spriteBatch,Texture2D pixel,int centreX,int centreY,int radius,Color colour)
    {
        var x = radius;
        var y = 0;
        var error = 1 - radius;

        while (x >= y)
        {
            DrawPixel(spriteBatch, pixel,centreX + x, centreY + y,colour);

            DrawPixel(spriteBatch, pixel,centreX + y, centreY + x,colour);

            DrawPixel(spriteBatch, pixel,centreX - y, centreY + x,colour);

            DrawPixel(spriteBatch, pixel,centreX - x, centreY + y,colour);

            DrawPixel(spriteBatch, pixel,centreX - x, centreY - y,colour);

            DrawPixel(spriteBatch, pixel,centreX - y, centreY - x,colour);

            DrawPixel(spriteBatch, pixel,centreX + y, centreY - x,colour);

            DrawPixel(spriteBatch, pixel,centreX + x, centreY - y,colour);

            y++;

            if (error < 0)
            {
                error += 2 * y + 1;
            }
            else
            {
                x--;
                error += 2 * (y - x) + 1;
            }
        }
    }

    private static void DrawHorizontalLine(SpriteBatch spriteBatch,Texture2D pixel,int x1,int x2,int y,Color colour)
    {
        if (x2 < x1)
        {
            (x1, x2) = (x2, x1);
        }

        spriteBatch.Draw(pixel,new Rectangle(x1,y,x2 - x1 + 1,1),colour);
    }

    private static void DrawFilledCircle(SpriteBatch spriteBatch,Texture2D pixel,int centreX,int centreY,int radius,Color colour)
    {
        var x = radius;
        var y = 0;
        var error = 1 - radius;

        while (x >= y)
        {
            DrawHorizontalLine(spriteBatch, pixel,centreX - x,centreX + x,centreY + y,colour);

            DrawHorizontalLine(spriteBatch, pixel,centreX - x,centreX + x,centreY - y,colour);

            DrawHorizontalLine(spriteBatch, pixel,centreX - y,centreX + y,centreY + x,colour);

            DrawHorizontalLine(spriteBatch, pixel,centreX - y,centreX + y,centreY - x,colour);

            y++;

            if (error < 0)
            {
                error += 2 * y + 1;
            }
            else
            {
                x--;
                error += 2 * (y - x) + 1;
            }
        }
    }
}