using System;
using System.Collections.Generic;
using System.Diagnostics;

using Centauri64.Basic.Syntax;
using Centauri64.Console;
using Centauri64.Machine;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteSprite(SpriteStatement statement)
    {
        var spriteIndex = Evaluate(statement.SpriteIndex);
        var assetName = Evaluate(statement.AssetName);

        if (!spriteIndex.IsInteger)
        {
            throw new InvalidOperationException("SPRITE index must be numeric.");
        }

        if (!assetName.IsString)
        {
            throw new InvalidOperationException("SPRITE name must be a string.");
        }

        _machine.SetSprite(spriteIndex.Integer,assetName.String!);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteSpriteAnimation(SpriteAnimationStatement statement)
    {
        var spriteIndex =
            Evaluate(statement.SpriteIndex);

        var animationName =
            Evaluate(statement.AnimationName);

        var loop =
            Evaluate(statement.Loop);

        if (!spriteIndex.IsInteger)
        {
            throw new InvalidOperationException(
                "SPRITEANIM index must be numeric.");
        }

        if (!animationName.IsString)
        {
            throw new InvalidOperationException(
                "SPRITEANIM name must be a string.");
        }

        if (!loop.IsInteger)
        {
            throw new InvalidOperationException(
                "SPRITEANIM loop must be numeric.");
        }

        if (loop.Integer != 0 &&
            loop.Integer != 1)
        {
            throw new InvalidOperationException(
                "SPRITEANIM loop must be 0 or 1.");
        }

        _machine.SetSpriteAnimation(
            spriteIndex.Integer,
            animationName.String!,
            loop.Integer != 0);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteSpritePosition(SpritePositionStatement statement)
    {
        var spriteIndex = Evaluate(statement.SpriteIndex);
        var x = Evaluate(statement.X);
        var y = Evaluate(statement.Y);

        if (!spriteIndex.IsInteger ||!x.IsInteger ||!y.IsInteger)
        {
            throw new InvalidOperationException("SPRITEPOS expects numeric values.");
        }

        _machine.SetSpritePosition(spriteIndex.Integer,x.Integer,y.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteSpriteShow(SpriteShowStatement statement)
    {
        var spriteIndex = Evaluate(statement.SpriteIndex);

        if (!spriteIndex.IsInteger)
        {
            throw new InvalidOperationException("SPRITESHOW expects a numeric sprite index.");
        }

        _machine.ShowSprite(spriteIndex.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteSpriteHide(SpriteHideStatement statement)
    {
        var spriteIndex = Evaluate(statement.SpriteIndex);

        if (!spriteIndex.IsInteger)
        {
            throw new InvalidOperationException("SPRITEHIDE expects a numeric sprite index.");
        }

        _machine.HideSprite(spriteIndex.Integer);

        return ExecutionResult.Continue();
    }
}