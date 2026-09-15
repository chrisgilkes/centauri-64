namespace Centauri64.Basic.Syntax;

public sealed class BinaryExpression : Expression
{
    public Expression Left { get; }
    public TokenType Operator { get; }
    public Expression Right { get; }

    public BinaryExpression(
        Expression left,
        TokenType @operator,
        Expression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }
}