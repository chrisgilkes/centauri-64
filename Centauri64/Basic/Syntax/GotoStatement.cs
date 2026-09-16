namespace Centauri64.Basic.Syntax;

public sealed class GotoStatement : Statement
{
    public int LineNumber { get; }

    public GotoStatement(int lineNumber)
    {
        LineNumber = lineNumber;
    }
}