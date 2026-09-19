namespace Centauri64.Basic.Syntax;

public sealed class ArrayAssignmentStatement : Statement
{
    public string Name { get; }
    public Expression Index { get; }
    public Expression Value { get; }

    public ArrayAssignmentStatement(
        string name,
        Expression index,
        Expression value)
    {
        Name = name;
        Index = index;
        Value = value;
    }
}