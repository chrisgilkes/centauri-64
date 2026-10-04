namespace Centauri64.Basic.Syntax;

public sealed class LoadMapStatement : Statement
{
    public Expression Name { get; }

    public LoadMapStatement(Expression name)
    {
        Name = name;
    }
}
