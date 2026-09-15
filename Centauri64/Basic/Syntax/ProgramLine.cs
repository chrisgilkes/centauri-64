namespace Centauri64.Basic.Syntax;

public sealed class ProgramLine
{
    public int LineNumber { get; }
    public Statement Statement { get; }
    public string Source { get; }

    public ProgramLine(int lineNumber,Statement statement, string source)
    {
        LineNumber = lineNumber;
        Statement = statement;
        Source = source;
    }
}