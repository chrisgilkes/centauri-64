using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Machine.Sprites;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    public const int MAX_SPRITES = 16;

    private readonly CentauriSprite[] _sprites = new CentauriSprite[MAX_SPRITES];

    public IReadOnlyList<CentauriSprite> Sprites => _sprites;

    private readonly SpriteRenderer _spriteRenderer = new();

    private readonly SpriteAssetStore _spriteAssets = new();

    private readonly SpriteEditor _spriteEditor;

    public SpriteEditor SpriteEditor => _spriteEditor;

    public SpriteAssetStore SpriteAssets => _spriteAssets;


    public void SetSpritePosition(int index,int x,int y)
    {
        ValidateSpriteIndex(index);

        _sprites[index].X = x;
        _sprites[index].Y = y;
    }

    public void ShowSprite(int index)
    {
        ValidateSpriteIndex(index);

        _sprites[index].Visible = true;
    }

    public void HideSprite(int index)
    {
        ValidateSpriteIndex(index);

        _sprites[index].Visible = false;
    }

    public void HideAllSprites()
    {
        foreach (var sprite in _sprites)
        {
            sprite.Visible = false;
        }
    }

     public void UpdateSpriteEditor(MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        _spriteEditor.Update(mouse,keyboard,previousKeyboard);
    }

    public void DrawSpriteEditor(SpriteBatch spriteBatch,BitmapFont font, Texture2D pixel)
    {
        _spriteEditor.Draw(spriteBatch,font, pixel);
    }
    public void DrawSprites(SpriteBatch spriteBatch,Texture2D pixel)
    {
        _spriteRenderer.Draw(spriteBatch,pixel,_sprites);
    }

    public void SetSprite(int index,string assetName)
    {
        ValidateSpriteIndex(index);

        var asset = _spriteAssets.Get(assetName);

        var animation = asset.GetAnimation("DEFAULT");

        if (animation.Frames.Count == 0)
        {
            throw new InvalidOperationException($"Sprite {assetName} has no frames.");
        }

        var frame = animation.Frames[0];

        var sprite = _sprites[index];

        for (var y = 0;y < CentauriSprite.HEIGHT;y++)
        {
            for (var x = 0;x < CentauriSprite.WIDTH;x++)
            {
                sprite.Pixels[y, x] = frame.Pixels[y, x];
            }
        }

        sprite.Visible = true;
    }

    public bool SpritesCollide(int firstIndex, int secondIndex)
    {
        ValidateSpriteIndex(firstIndex);
        ValidateSpriteIndex(secondIndex);

        var first = _sprites[firstIndex];
        var second = _sprites[secondIndex];

        if (!first.Visible || !second.Visible)
            return false;

        return
            first.X < second.X + CentauriSprite.WIDTH &&
            first.X + CentauriSprite.WIDTH > second.X &&
            first.Y < second.Y + CentauriSprite.HEIGHT &&
            first.Y + CentauriSprite.HEIGHT > second.Y;
    }

    private void CreateBuiltInSpriteAssets()
    {
        var player = new SpriteAsset("PLAYER");

        var animation = player.AddAnimation("DEFAULT");

        var frame = animation.AddFrame();

        for (var y = 0;y < CentauriSprite.HEIGHT;y++)
        {
            for (var x = 0;x < CentauriSprite.WIDTH;x++)
            {
                if (x == y ||x == CentauriSprite.WIDTH - 1 - y)
                {
                    frame.Pixels[y, x] = 7;
                }
            }
        }

        _spriteAssets.Add(player);
    }

    private static void ValidateSpriteIndex(int index)
    {
        if (index < 0 || index >= MAX_SPRITES)
        {
            throw new InvalidOperationException($"Sprite must be between 0 and {MAX_SPRITES - 1}.");
        }
    }

}