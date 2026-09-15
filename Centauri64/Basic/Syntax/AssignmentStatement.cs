namespace Centauri64.Basic.Syntax;

public sealed class AssignmentStatement : Statement
{
    public string VariableName { get; }
    public Expression Value { get; }

    public AssignmentStatement(
        string variableName,
        Expression value)
    {
        VariableName = variableName;
        Value = value;
    }
}