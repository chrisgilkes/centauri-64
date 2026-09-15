namespace Centauri64.Basic.Syntax;

public sealed class PrintStatement : Statement
{
    public Expression Expression { get; }

    public PrintStatement(Expression expression)
    {
        Expression = expression;
    }
}