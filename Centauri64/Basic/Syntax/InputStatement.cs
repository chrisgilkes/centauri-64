namespace Centauri64.Basic.Syntax;

public sealed class InputStatement : Statement
{
    public Expression? Prompt { get; }

    public string VariableName { get; }

    public InputStatement(
        Expression? prompt,
        string variableName)
    {
        Prompt = prompt;
        VariableName = variableName;
    }
}
