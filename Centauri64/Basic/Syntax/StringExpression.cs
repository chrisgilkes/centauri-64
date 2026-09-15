namespace Centauri64.Basic.Syntax;

public sealed class StringExpression : Expression
{
    public string Value { get; }

    public StringExpression(string value)
    {
        Value = value;
    }
}