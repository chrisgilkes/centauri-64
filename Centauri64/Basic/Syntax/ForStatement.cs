namespace Centauri64.Basic.Syntax;

public sealed class ForStatement : Statement
{
    public string VariableName { get; }

    public Expression Start { get; }

    public Expression End { get; }

    public Expression? Step { get; }

    public ForStatement(
        string variableName,
        Expression start,
        Expression end,
        Expression? step)
    {
        VariableName = variableName;
        Start = start;
        End = end;
        Step = step;
    }
}