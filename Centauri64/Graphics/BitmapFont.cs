using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Centauri64.Graphics;

public sealed class BitmapFont
{
    private const int CharacterWidth = 8;
    private const int CharacterHeight = 8;
    private const int CharactersPerRow = 16;
    private const int FirstCharacter = 32;

    private readonly Texture2D _texture;

    public BitmapFont(Texture2D texture)
    {
        _texture = texture;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color color)
    {
        Draw(spriteBatch, text, position, color, 1f);
    }

    public void Draw(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color color,
        float scale)
    {
        var x = (int)position.X;
        var y = (int)position.Y;
        var step = Math.Max(1, (int)(CharacterWidth * scale));
        var size = Math.Max(1, (int)(CharacterHeight * scale));

        foreach (var character in text)
        {
            var index = character - FirstCharacter;

            if (index < 0 || index > 94)
            {
                x += step;
                continue;
            }

            var column = index % CharactersPerRow;
            var row = index / CharactersPerRow;

            var source = new Rectangle(
                column * CharacterWidth,
                row * CharacterHeight,
                CharacterWidth,
                CharacterHeight);

            spriteBatch.Draw(
                _texture,
                new Rectangle(x, y, size, size),
                source,
                color);

            x += step;
        }
    }

    public void DrawCharacter(SpriteBatch spriteBatch,char character,Vector2 position,Color color)
    {
        var index = character - FirstCharacter;

        if (index < 0 || index > 94)
            return;

        var column = index % CharactersPerRow;
        var row = index / CharactersPerRow;

        var source = new Rectangle(column * CharacterWidth,row * CharacterHeight,CharacterWidth,CharacterHeight);

        spriteBatch.Draw(_texture,position,source,color);
    }
}