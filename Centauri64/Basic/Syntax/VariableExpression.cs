namespace Centauri64.Basic.Syntax;

public sealed class VariableExpression : Expression
{
    public string Name { get; }

    public VariableExpression(string name)
    {
        Name = name;
    }
}