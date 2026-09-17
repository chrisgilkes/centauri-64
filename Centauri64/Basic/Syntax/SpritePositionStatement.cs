namespace Centauri64.Basic.Syntax;

public sealed class SpritePositionStatement : Statement
{
    public Expression SpriteIndex { get; }
    public Expression X { get; }
    public Expression Y { get; }

    public SpritePositionStatement(
        Expression spriteIndex,
        Expression x,
        Expression y)
    {
        SpriteIndex = spriteIndex;
        X = x;
        Y = y;
    }
}