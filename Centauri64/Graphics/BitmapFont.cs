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
        var x = (int)position.X;
        var y = (int)position.Y;

        foreach (var character in text)
        {
            var index = character - FirstCharacter;

            if (index < 0 || index > 94)
            {
                x += CharacterWidth;
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
                new Vector2(x, y),
                source,
                color);

            x += CharacterWidth;
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