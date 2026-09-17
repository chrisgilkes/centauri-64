namespace Centauri64.Basic.Syntax;

public sealed class SpriteStatement : Statement
{
    public Expression SpriteIndex { get; }
    public Expression AssetName { get; }

    public SpriteStatement(
        Expression spriteIndex,
        Expression assetName)
    {
        SpriteIndex = spriteIndex;
        AssetName = assetName;
    }
}