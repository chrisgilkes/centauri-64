namespace Centauri64.Basic.Syntax;

public sealed class BgStatement : Statement
{
    public Expression Layer { get; }
    public Expression? Name { get; }
    public bool Off { get; }

    private BgStatement(Expression layer, Expression? name, bool off)
    {
        Layer = layer;
        Name = name;
        Off = off;
    }

    public static BgStatement Show(Expression layer, Expression name) =>
        new(layer, name, false);

    public static BgStatement Hide(Expression layer) =>
        new(layer, null, true);
}
