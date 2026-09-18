public readonly struct ExecutionResult
{
    public ExecutionAction Action { get; }
    public int? JumpToLine { get; }

    private ExecutionResult(
        ExecutionAction action,
        int? jumpToLine = null)
    {
        Action = action;
        JumpToLine = jumpToLine;
    }

    public static ExecutionResult Continue()
    {
        return new ExecutionResult(
            ExecutionAction.Continue);
    }

    public static ExecutionResult Jump(int lineNumber)
    {
        return new ExecutionResult(
            ExecutionAction.Jump,
            lineNumber);
    }

    public static ExecutionResult Yield()
    {
        return new ExecutionResult(
            ExecutionAction.Yield);
    }

    public static ExecutionResult Return()
    {
        return new ExecutionResult(
            ExecutionAction.Return);
    }
}