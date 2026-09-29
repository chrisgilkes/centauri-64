public readonly struct ExecutionResult
{
    public ExecutionAction Action { get; }
    public int? JumpToLine { get; }
    public int? ProgramCounter { get; }
    public ScreenPresentation Presentation { get; }

    private ExecutionResult(
        ExecutionAction action,
        int? jumpToLine = null,
        int? programCounter = null,
        ScreenPresentation presentation = ScreenPresentation.None)
    {
        Action = action;
        JumpToLine = jumpToLine;
        ProgramCounter = programCounter;
        Presentation = presentation;
    }

    public static ExecutionResult Continue(
        ScreenPresentation presentation = ScreenPresentation.None)
    {
        return new ExecutionResult(
            ExecutionAction.Continue,
            presentation: presentation);
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

    public static ExecutionResult Input()
    {
        return new ExecutionResult(
            ExecutionAction.Input);
    }

    public static ExecutionResult End()
    {
        return new ExecutionResult(
            ExecutionAction.End);
    }

    public static ExecutionResult JumpToProgramCounter(int programCounter)
    {
        return new ExecutionResult(
            ExecutionAction.JumpToProgramCounter,
            programCounter: programCounter);
    }
}