namespace Centauri64.Basic.Syntax;
public sealed class SpriteShowStatement : Statement
{
    public Expression SpriteIndex { get; }

    public SpriteShowStatement(Expression spriteIndex)
    {
        SpriteIndex = spriteIndex;
    }
}