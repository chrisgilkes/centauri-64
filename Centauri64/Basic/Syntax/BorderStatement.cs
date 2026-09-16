namespace Centauri64.Basic.Syntax;

public sealed class BorderStatement : Statement
{
    public Expression Colour { get; }

    public BorderStatement(Expression colour)
    {
        Colour = colour;
    }
}