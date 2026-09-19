namespace Centauri64.Basic.Syntax;

public sealed class ArrayAccessExpression : Expression
{
    public string Name { get; }
    public Expression Index { get; }

    public ArrayAccessExpression(
        string name,
        Expression index)
    {
        Name = name;
        Index = index;
    }
}