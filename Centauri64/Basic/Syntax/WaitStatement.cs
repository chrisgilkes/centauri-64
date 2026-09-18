namespace Centauri64.Basic.Syntax;

public sealed class WaitStatement : Statement
{
    public Expression Duration { get; }

    public WaitStatement(Expression duration)
    {
        Duration = duration;
    }
}