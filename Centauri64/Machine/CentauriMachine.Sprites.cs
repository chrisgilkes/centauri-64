using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Centauri64.Graphics;
using Centauri64.Machine.Sprites;
using Microsoft.Xna.Framework;

namespace Centauri64.Machine;

public sealed partial class CentauriMachine
{
    public const int MAX_SPRITES = 32;

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

     public void UpdateSpriteEditor(GameTime gameTime, MouseState mouse,KeyboardState keyboard,KeyboardState previousKeyboard)
    {
        _spriteEditor.Update(gameTime, mouse,keyboard,previousKeyboard);
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

        var asset =
            _spriteAssets.Get(assetName);

        var animation =
            asset.GetAnimation("DEFAULT");

        if (animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sprite {assetName} has no frames.");
        }

        var sprite = _sprites[index];

        sprite.AssetName = assetName;

        sprite.AnimationName = "DEFAULT";

        sprite.AnimationFrame = 0;
        sprite.AnimationTimer = 0.0f;
        sprite.AnimationPlaying = true;
        sprite.AnimationLoop = true;
        sprite.PreviousAnimationName = null;

        CopyFrameToSprite(animation.Frames[0],sprite);

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

        if (!TryGetSpriteBounds(
                first,
                out var firstLeft,
                out var firstTop,
                out var firstRight,
                out var firstBottom))
        {
            return false;
        }

        if (!TryGetSpriteBounds(
                second,
                out var secondLeft,
                out var secondTop,
                out var secondRight,
                out var secondBottom))
        {
            return false;
        }

        var firstWorldLeft = first.X + firstLeft;
        var firstWorldTop = first.Y + firstTop;
        var firstWorldRight = first.X + firstRight;
        var firstWorldBottom = first.Y + firstBottom;

        var secondWorldLeft = second.X + secondLeft;
        var secondWorldTop = second.Y + secondTop;
        var secondWorldRight = second.X + secondRight;
        var secondWorldBottom = second.Y + secondBottom;

        return
            firstWorldLeft <= secondWorldRight &&
            firstWorldRight >= secondWorldLeft &&
            firstWorldTop <= secondWorldBottom &&
            firstWorldBottom >= secondWorldTop;
    }

    private static bool TryGetSpriteBounds(CentauriSprite sprite,out int left,out int top,out int right,out int bottom)
    {
        left = CentauriSprite.WIDTH;
        top = CentauriSprite.HEIGHT;
        right = -1;
        bottom = -1;

        for (var y = 0; y < CentauriSprite.HEIGHT; y++)
        {
            for (var x = 0; x < CentauriSprite.WIDTH; x++)
            {
                if (sprite.Pixels[y, x] == 0)
                    continue;

                if (x < left)
                    left = x;

                if (x > right)
                    right = x;

                if (y < top)
                    top = y;

                if (y > bottom)
                    bottom = y;
            }
        }

        return right >= left && bottom >= top;
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

    private static void CopyFrameToSprite(SpriteFrame frame,CentauriSprite sprite)
    {
        for (var y = 0;
            y < CentauriSprite.HEIGHT;
            y++)
        {
            for (var x = 0;
                x < CentauriSprite.WIDTH;
                x++)
            {
                sprite.Pixels[y, x] =
                    frame.Pixels[y, x];
            }
        }
    }

    public void SetSpriteAnimation(int index,string animationName,bool loop)
    {
        ValidateSpriteIndex(index);

        var sprite = _sprites[index];

        if (sprite.AssetName == null)
        {
            throw new InvalidOperationException($"Sprite {index} has no asset.");
        }

        var asset =_spriteAssets.Get(sprite.AssetName);

        var animation = asset.GetAnimation(animationName);

        if (animation.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Animation {animationName} has no frames.");
        }

        // Don't restart the same looping animation
        // every BASIC update.
        if (sprite.AnimationName == animationName &&
            sprite.AnimationPlaying &&
            sprite.AnimationLoop == loop)
        {
            return;
        }

        if (loop)
        {
            // A new looping animation cancels any
            // pending return from a one-shot.
            sprite.PreviousAnimationName = null;
        }
        else
        {
            // A one-shot returns to whatever
            // animation was playing before it.
            sprite.PreviousAnimationName =
                sprite.AnimationName;
        }

        sprite.AnimationName =
            animationName;

        sprite.AnimationFrame = 0;
        sprite.AnimationTimer = 0.0f;
        sprite.AnimationPlaying = true;
        sprite.AnimationLoop = loop;

        CopyFrameToSprite(animation.Frames[0],sprite);
    }

    public void UpdateSprites(GameTime gameTime)
    {
        const float frameTime = 0.125f;

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (var i = 0; i < _sprites.Length;i++)
        {
            var sprite = _sprites[i];

            if (!sprite.Visible ||
                !sprite.AnimationPlaying ||
                sprite.AssetName == null)
            {
                continue;
            }

            var asset = _spriteAssets.Get(sprite.AssetName);

            var animation = asset.GetAnimation(sprite.AnimationName);

            if (animation.Frames.Count <= 1)
            {
                continue;
            }

            sprite.AnimationTimer += deltaTime;

            while (sprite.AnimationTimer >=frameTime)
            {
                sprite.AnimationTimer -= frameTime;

                sprite.AnimationFrame++;

                if (sprite.AnimationFrame >=
                    animation.Frames.Count)
                {
                    // Looping animation.
                    if (sprite.AnimationLoop)
                    {
                        sprite.AnimationFrame = 0;
                    }
                    // One-shot animation.
                    else if (sprite.PreviousAnimationName != null)
                    {
                        var previousAnimation =
                            sprite.PreviousAnimationName;

                        sprite.PreviousAnimationName =
                            null;

                        SetSpriteAnimation(
                            i,
                            previousAnimation,
                            true);

                        break;
                    }
                    // One-shot with nowhere to return.
                    else
                    {
                        sprite.AnimationFrame =
                            animation.Frames.Count - 1;

                        sprite.AnimationPlaying =
                            false;
                    }
                }

                CopyFrameToSprite(
                    animation.Frames[
                        sprite.AnimationFrame],
                    sprite);
            }
        }
    }

}