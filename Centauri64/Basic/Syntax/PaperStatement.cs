namespace Centauri64.Basic.Syntax;

public sealed class PaperStatement : Statement
{
    public Expression Colour { get; }

    public PaperStatement(Expression colour)
    {
        Colour = colour;
    }
}