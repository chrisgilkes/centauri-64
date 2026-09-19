namespace Centauri64.Basic.Syntax;

public sealed class NextStatement : Statement
{
    public string VariableName { get; }

    public NextStatement(string variableName)
    {
        VariableName = variableName;
    }
}