namespace Centauri64.Basic.Syntax;

public sealed class LineStatement : Statement
{
    public Expression X1 { get; }
    public Expression Y1 { get; }
    public Expression X2 { get; }
    public Expression Y2 { get; }
    public Expression Colour { get; }

    public LineStatement(
        Expression x1,
        Expression y1,
        Expression x2,
        Expression y2,
        Expression colour)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        Colour = colour;
    }
}