namespace Centauri64.Basic;

public readonly struct ExecutionResult
{
    public int? JumpToLine { get; }

    private ExecutionResult(int? jumpToLine)
    {
        JumpToLine = jumpToLine;
    }

    public static ExecutionResult Continue()
    {
        return new ExecutionResult(null);
    }

    public static ExecutionResult Jump(int lineNumber)
    {
        return new ExecutionResult(lineNumber);
    }
}