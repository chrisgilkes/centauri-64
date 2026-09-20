using System;
using System.Diagnostics;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteGoto(GotoStatement statement)
    {
        return ExecutionResult.Jump(statement.LineNumber);
    }

    private ExecutionResult ExecuteGosub(GosubStatement statement)
    {
        _returnStack.Push(_programCounter + 1);

        return ExecutionResult.Jump(statement.LineNumber);
    }

    private ExecutionResult ExecuteReturn(ReturnStatement statement)
    {
        if (_returnStack.Count == 0)
        {
            throw new InvalidOperationException("RETURN without GOSUB.");
        }

        return ExecutionResult.Return();
    }

    private ExecutionResult ExecuteIf(IfStatement statement)
    {
        var condition = Evaluate(statement.Condition);

        if (!condition.IsInteger)
        {
            throw new InvalidOperationException("IF condition must be numeric.");
        }

        if (condition.Integer != 0)
        {
            return Execute(statement.ThenStatement);
        }

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteYield(YieldStatement statement)
    {
        return ExecutionResult.Yield();
    }

    private ExecutionResult ExecuteWait(WaitStatement statement)
    {
        var duration = Evaluate(statement.Duration);

        if (!duration.IsInteger)
        {
            throw new InvalidOperationException("WAIT expects a numeric duration.");
        }

        if (duration.Integer < 0)
        {
            throw new InvalidOperationException("WAIT duration cannot be negative.");
        }

        var ticks = (long)(duration.Integer / 1000.0 * Stopwatch.Frequency);

        _waitUntil = Stopwatch.GetTimestamp() + ticks;

        return ExecutionResult.Wait();
    }

    private ExecutionResult ExecuteFor(ForStatement statement)
    {
        var start = Evaluate(statement.Start);
        var end = Evaluate(statement.End);

        if (!start.IsInteger || !end.IsInteger)
        {
            throw new InvalidOperationException("FOR expects numeric values.");
        }

        var step = 1;

        if (statement.Step != null)
        {
            var stepValue = Evaluate(statement.Step);

            if (!stepValue.IsInteger)
            {
                throw new InvalidOperationException("STEP expects a numeric value.");
            }

            step = stepValue.Integer;
        }

        if (step == 0)
        {
            throw new InvalidOperationException("STEP cannot be zero.");
        }

        _variables[statement.VariableName] = start.Integer;

        _forStack.Push(
            new ForLoop(
                statement.VariableName,
                end.Integer,
                step,
                _programCounter + 1));

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteNext(NextStatement statement)
    {
        if (_forStack.Count == 0)
        {
            throw new InvalidOperationException("NEXT without FOR.");
        }

        var loop = _forStack.Peek();

        if (loop.VariableName != statement.VariableName)
        {
            throw new InvalidOperationException($"NEXT {statement.VariableName} does not match FOR {loop.VariableName}.");
        }

        var value = GetVariable(loop.VariableName) + loop.Step;

        _variables[loop.VariableName] = value;

        var keepGoing = loop.Step > 0 ? value <= loop.End : value >= loop.End;

        if (keepGoing)
        {
            return ExecutionResult.JumpToProgramCounter(loop.LoopStartProgramCounter);
        }

        _forStack.Pop();

        return ExecutionResult.Continue();
    }
}