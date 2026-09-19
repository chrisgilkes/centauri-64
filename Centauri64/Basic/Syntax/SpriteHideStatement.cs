namespace Centauri64.Basic.Syntax;

public sealed class SpriteHideStatement : Statement
{
    public Expression SpriteIndex { get; }

    public SpriteHideStatement(Expression spriteIndex)
    {
        SpriteIndex = spriteIndex;
    }
}