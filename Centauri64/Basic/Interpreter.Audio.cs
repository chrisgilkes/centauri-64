using System;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteBeep(BeepStatement statement)
    {
        var frequency = Evaluate(statement.Frequency);
        var duration = Evaluate(statement.Duration);

        if (!frequency.IsInteger || !duration.IsInteger)
        {
            throw new InvalidOperationException("BEEP expects numeric values.");
        }

        if (frequency.Integer < 0)
        {
            throw new InvalidOperationException("BEEP frequency cannot be negative.");
        }

        if (duration.Integer < 0)
        {
            throw new InvalidOperationException("BEEP duration cannot be negative.");
        }

        if (duration.Integer == 0)
        {
            return ExecutionResult.Continue();
        }

        _machine.Beep(frequency.Integer, duration.Integer);

        return BeginWait(duration.Integer);
    }
}
