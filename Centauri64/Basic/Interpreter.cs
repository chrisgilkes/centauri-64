using System;
using System.Collections.Generic;
using Centauri64.Basic.Syntax;
using Centauri64.Console;

namespace Centauri64.Basic;

public sealed class Interpreter
{
    private readonly TextConsole _console;
    private readonly Dictionary<string, int> _variables = new();

    private const int MAX_INSTRUCTIONS_PER_RUN = 100_000;

    public Interpreter(TextConsole console)
    {
        _console = console;
    }


    public void Run(BasicProgram program)
    {
        _variables.Clear();

        var lines = program.GetLines();

        var programCounter = 0;
        var instructionCount = 0;

        while(programCounter < lines.Count)
        {
            if(++instructionCount > MAX_INSTRUCTIONS_PER_RUN)
            {
                throw new InvalidOperationException("Program execution limit exceeded.");    
            }

            var line   = lines[programCounter];

            var result = Execute(line.Statement);

            if (result.JumpToLine.HasValue)
            {
                programCounter = FindLine(lines,result.JumpToLine.Value);
            }
            else
            {
                programCounter++;
            }
        }
    }

    public int FindLine(IReadOnlyList<ProgramLine> lines,int lineNumber)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].LineNumber == lineNumber)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Undefined line {lineNumber}.");
    }

    private ExecutionResult Execute(Statement statement)
    {
        if(statement is PrintStatement print)
        {
            var value = Evaluate(print.Expression);

            _console.WriteLine(value.ToString());

            return ExecutionResult.Continue();
        }

        if (statement is GotoStatement gotoStatement)
        {
            return ExecutionResult.Jump(gotoStatement.LineNumber);
        }

        if (statement is AssignmentStatement assignment)
        {
            var value = Evaluate(assignment.Value);

            if (!value.IsInteger)
            {
                throw new InvalidOperationException("Expected numeric value.");
            }

            _variables[assignment.VariableName] = value.Integer;

            return ExecutionResult.Continue();
        }

        throw new InvalidOperationException($"Unsupported statement: {statement.GetType().Name}");
    }

    private BasicValue Evaluate(Expression expression)
    {
        if (expression is NumberExpression number)
        {
            return new BasicValue(number.Value);
        }

        if (expression is StringExpression text)
        {
            return new BasicValue(text.Value);
        }

        if (expression is VariableExpression variable)
        {
            return new BasicValue(
                GetVariable(variable.Name));
        }

        if (expression is BinaryExpression binary)
        {
            return EvaluateBinary(binary);
        }

        throw new InvalidOperationException(
            $"Unsupported expression: {expression.GetType().Name}");
    }

    private BasicValue EvaluateBinary(BinaryExpression expression)
    {
        var left = Evaluate(expression.Left);
        var right = Evaluate(expression.Right);

        if (!left.IsInteger || !right.IsInteger)
        {
            throw new InvalidOperationException(
                "Arithmetic requires numeric values.");
        }

        var result = expression.Operator switch
        {
            TokenType.Plus =>
                left.Integer + right.Integer,

            TokenType.Minus =>
                left.Integer - right.Integer,

            TokenType.Multiply =>
                left.Integer * right.Integer,

           TokenType.Divide =>
                right.Integer == 0
                ? throw new InvalidOperationException("Division by zero.")
                : left.Integer / right.Integer,

            _ => throw new InvalidOperationException(
                $"Unsupported operator: {expression.Operator}")
        };

        return new BasicValue(result);
    }

    private int GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out var value))
        {
            return value;
        }

        return 0;
    }
}