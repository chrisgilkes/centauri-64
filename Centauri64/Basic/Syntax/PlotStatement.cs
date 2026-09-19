namespace Centauri64.Basic.Syntax;

public sealed class PlotStatement : Statement
{
    public Expression X { get; }
    public Expression Y { get; }
    public Expression Colour { get; }

    public PlotStatement(
        Expression x,
        Expression y,
        Expression colour)
    {
        X = x;
        Y = y;
        Colour = colour;
    }
}