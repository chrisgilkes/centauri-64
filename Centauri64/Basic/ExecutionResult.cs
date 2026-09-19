public readonly struct ExecutionResult
{
    public ExecutionAction Action { get; }
    public int? JumpToLine { get; }
    public int? ProgramCounter { get; }

    private ExecutionResult(ExecutionAction action,int? jumpToLine = null,int? programCounter = null)
    {
        Action = action;
        JumpToLine = jumpToLine;
        ProgramCounter = programCounter;
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

    public static ExecutionResult Wait()
    {
        return new ExecutionResult(
            ExecutionAction.Wait);
    }

    public static ExecutionResult JumpToProgramCounter(int programCounter)
    {
        return new ExecutionResult(
            ExecutionAction.JumpToProgramCounter,
            programCounter: programCounter);
    }
}