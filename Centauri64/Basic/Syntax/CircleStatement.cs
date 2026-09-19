namespace Centauri64.Basic.Syntax;

public sealed class CircleStatement : Statement
{
    public Expression X { get; }
    public Expression Y { get; }
    public Expression Radius { get; }
    public Expression Colour { get; }
    public bool Filled { get; }

    public CircleStatement(
        Expression x,
        Expression y,
        Expression radius,
        Expression colour,
        bool filled)
    {
        X = x;
        Y = y;
        Radius = radius;
        Colour = colour;
        Filled = filled;
    }
}