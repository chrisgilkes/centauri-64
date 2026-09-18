namespace Centauri64.Basic.Syntax;

public sealed class BeepStatement : Statement
{
    public Expression Frequency { get; }
    public Expression Duration { get; }

    public BeepStatement(
        Expression frequency,
        Expression duration)
    {
        Frequency = frequency;
        Duration = duration;
    }
}