namespace Centauri64.Basic.Syntax;

public sealed class NumberExpression : Expression
{
    public int Value { get; }

    public NumberExpression(int value)
    {
        Value = value;
    }
}