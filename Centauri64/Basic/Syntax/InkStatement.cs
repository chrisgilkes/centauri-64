namespace Centauri64.Basic.Syntax;

public sealed class InkStatement : Statement
{
    public Expression Colour { get; }

    public InkStatement(Expression colour)
    {
        Colour = colour;
    }
}