namespace Centauri64.Basic.Syntax;

public sealed class RectStatement : Statement
{
    public Expression X { get; }
    public Expression Y { get; }
    public Expression Width { get; }
    public Expression Height { get; }
    public Expression Colour { get; }
    public bool Filled { get; }

    public RectStatement(
        Expression x,
        Expression y,
        Expression width,
        Expression height,
        Expression colour,
        bool filled)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Colour = colour;
        Filled = filled;
    }
}