namespace Centauri64.Basic.Syntax;

public sealed class UnaryExpression : Expression
{
    public TokenType Operator { get; }
    public Expression Operand { get; }

    public UnaryExpression(
        TokenType @operator,
        Expression operand)
    {
        Operator = @operator;
        Operand = operand;
    }
}