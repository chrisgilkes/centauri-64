namespace Centauri64.Basic.Syntax;

public sealed class GosubStatement : Statement
{
    public int LineNumber { get; }

    public GosubStatement(int lineNumber)
    {
        LineNumber = lineNumber;
    }
}