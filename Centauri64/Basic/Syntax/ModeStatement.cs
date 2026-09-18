namespace Centauri64.Basic.Syntax;

public sealed class ModeStatement : Statement
{
    public Expression Mode { get; }

    public ModeStatement(Expression mode)
    {
        Mode = mode;
    }
}