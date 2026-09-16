namespace Centauri64.Basic.Syntax;

public sealed class IfStatement : Statement
{
    public Expression Condition { get; }
    public Statement ThenStatement { get; }

    public IfStatement(
        Expression condition,
        Statement thenStatement)
    {
        Condition = condition;
        ThenStatement = thenStatement;
    }
}