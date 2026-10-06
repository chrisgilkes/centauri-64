namespace Centauri64.Basic.Syntax;

public sealed class FgStatement : Statement
{
    public Expression? Name { get; }
    public bool Off { get; }

    private FgStatement(Expression? name, bool off)
    {
        Name = name;
        Off = off;
    }

    public static FgStatement Show(Expression name) => new(name, false);

    public static FgStatement Hide() => new(null, true);
}
