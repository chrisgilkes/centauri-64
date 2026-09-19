namespace Centauri64.Basic.Syntax;

public sealed class DimStatement : Statement
{
    public string Name { get; }
    public Expression Size { get; }

    public DimStatement(string name, Expression size)
    {
        Name = name;
        Size = size;
    }
}