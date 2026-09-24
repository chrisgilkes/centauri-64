namespace Centauri64.Basic.Syntax;

public sealed class SpriteAnimationStatement : Statement
{
    public Expression SpriteIndex { get; }

    public Expression AnimationName { get; }

    public Expression Loop { get; }

    public SpriteAnimationStatement(
        Expression spriteIndex,
        Expression animationName,
        Expression loop)
    {
        SpriteIndex = spriteIndex;
        AnimationName = animationName;
        Loop = loop;
    }
}