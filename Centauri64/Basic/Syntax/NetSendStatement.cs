namespace Centauri64.Basic.Syntax;

public sealed class NetSendStatement : Statement
{
    public Expression Name { get; }
    public Expression Value { get; }

    public NetSendStatement(Expression name, Expression value)
    {
        Name = name;
        Value = value;
    }
}
