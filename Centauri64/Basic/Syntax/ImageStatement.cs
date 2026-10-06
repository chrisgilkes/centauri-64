namespace Centauri64.Basic.Syntax;

public sealed class ImageStatement : Statement
{
    public Expression? Name { get; }
    public Expression? X { get; }
    public Expression? Y { get; }
    public Expression? Frame { get; }
    public bool Off { get; }

    private ImageStatement(
        Expression? name,
        Expression? x,
        Expression? y,
        Expression? frame,
        bool off)
    {
        Name = name;
        X = x;
        Y = y;
        Frame = frame;
        Off = off;
    }

    public static ImageStatement Show(
        Expression name,
        Expression? x = null,
        Expression? y = null,
        Expression? frame = null) =>
        new(name, x, y, frame, false);

    /// <summary>
    /// Clears IMAGE blits. Prefer CLS for a full screen clear.
    /// </summary>
    public static ImageStatement Hide() => new(null, null, null, null, true);
}
