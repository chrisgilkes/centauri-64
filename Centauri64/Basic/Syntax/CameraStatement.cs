namespace Centauri64.Basic.Syntax;

public sealed class CameraStatement : Statement
{
    public bool Follow { get; }

    public Expression? X { get; }

    public Expression? Y { get; }

    public Expression? SpriteIndex { get; }

    private CameraStatement(
        bool follow,
        Expression? x,
        Expression? y,
        Expression? spriteIndex)
    {
        Follow = follow;
        X = x;
        Y = y;
        SpriteIndex = spriteIndex;
    }

    public static CameraStatement Manual(Expression x, Expression y)
    {
        return new CameraStatement(false, x, y, null);
    }

    public static CameraStatement FollowSprite(Expression spriteIndex)
    {
        return new CameraStatement(true, null, null, spriteIndex);
    }
}
