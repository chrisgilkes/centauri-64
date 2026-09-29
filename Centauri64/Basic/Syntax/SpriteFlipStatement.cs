namespace Centauri64.Basic.Syntax;

public sealed class SpriteFlipStatement : Statement
{
    public Expression SpriteIndex { get; }
    public Expression Facing { get; }

    public SpriteFlipStatement(
        Expression spriteIndex,
        Expression facing)
    {
        SpriteIndex = spriteIndex;
        Facing = facing;
    }
}
