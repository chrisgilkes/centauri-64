using System;
using System.Collections.Generic;
using Centauri64.Basic.Syntax;
using Centauri64.Console;
using Centauri64.Machine;

namespace Centauri64.Basic;

public sealed class Interpreter
{
    private readonly TextConsole _console;
    private readonly Dictionary<string, int> _variables = new();

    private const int MAX_INSTRUCTIONS_PER_RUN = 100_000;

    private IReadOnlyList<ProgramLine> _lines = [];
    private int _programCounter;
    private int _instructionCount;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    private readonly CentauriMachine _machine;

    public Interpreter(TextConsole console, CentauriMachine machine)
    {
        _console = console;
        _machine = machine;
    }

    public void Start(BasicProgram program)
    {
        _variables.Clear();

        _lines = program.GetLines();
        _programCounter = 0;
        _instructionCount = 0;

        _isRunning = _lines.Count > 0;
    }

    public bool ExecuteNextInstruction()
    {
        if (!_isRunning)
            return false;

        if (_programCounter >= _lines.Count)
        {
            Stop();
            return false;
        }

        if (++_instructionCount > MAX_INSTRUCTIONS_PER_RUN)
        {
            Stop();

            throw new InvalidOperationException(
                "Program execution limit exceeded.");
        }

        var line = _lines[_programCounter];
        var result = Execute(line.Statement);

        switch (result.Action)
        {
            case ExecutionAction.Continue:
                _programCounter++;
                break;

            case ExecutionAction.Jump:
                _programCounter = FindLine(
                    _lines,
                    result.JumpToLine!.Value);
                break;

            case ExecutionAction.Yield:
                _programCounter++;
                break;
        }

        if (_programCounter >= _lines.Count)
        {
            Stop();
        }

        return result.Action == ExecutionAction.Yield;
    }

    public void Stop()
    {
        _isRunning = false;
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

        if (statement is IfStatement ifStatement)
        {
            var condition = Evaluate(ifStatement.Condition);

            if (!condition.IsInteger)
            {
                throw new InvalidOperationException(
                    "IF condition must be numeric.");
            }

            if (condition.Integer != 0)
            {
                return Execute(
                    ifStatement.ThenStatement);
            }

            return ExecutionResult.Continue();
        }

        if (statement is YieldStatement)
        {
            return ExecutionResult.Yield();
        }

        if (statement is TextAtStatement textAt)
        {
            var x = Evaluate(textAt.X);
            var y = Evaluate(textAt.Y);
            var text = Evaluate(textAt.Text);

            if (!x.IsInteger || !y.IsInteger)
            {
                throw new InvalidOperationException(
                    "TEXTAT coordinates must be numeric.");
            }

            _machine.WriteText(
                x.Integer,
                y.Integer,
                text.ToString());

            return ExecutionResult.Continue();
        }

        if (statement is ClsStatement)
        {
            _machine.ClearScreen();

            return ExecutionResult.Continue();
        }

        if (statement is InkStatement ink)
        {
            var colour = Evaluate(ink.Colour);

            if (!colour.IsInteger)
            {
                throw new InvalidOperationException(
                    "INK expects a number.");
            }

            _machine.SetInk(colour.Integer);

            return ExecutionResult.Continue();
        }

        if (statement is PaperStatement paper)
        {
            var colour = Evaluate(paper.Colour);

            if (!colour.IsInteger)
            {
                throw new InvalidOperationException(
                    "PAPER expects a number.");
            }

            _machine.SetPaper(colour.Integer);

            return ExecutionResult.Continue();
        }

        if (statement is BorderStatement border)
        {
            var colour = Evaluate(border.Colour);

            if (!colour.IsInteger)
            {
                throw new InvalidOperationException(
                    "BORDER expects a number.");
            }

            _machine.SetBorder(colour.Integer);

            return ExecutionResult.Continue();
        }

        if (statement is SpritePositionStatement spritePosition)
        {
            var spriteIndex =
                Evaluate(spritePosition.SpriteIndex);

            var x =
                Evaluate(spritePosition.X);

            var y =
                Evaluate(spritePosition.Y);

            if (!spriteIndex.IsInteger ||
                !x.IsInteger ||
                !y.IsInteger)
            {
                throw new InvalidOperationException(
                    "SPRITEPOS expects numeric values.");
            }

            _machine.SetSpritePosition(
                spriteIndex.Integer,
                x.Integer,
                y.Integer);

            return ExecutionResult.Continue();
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

        if (expression is FunctionCallExpression function)
        {
            return EvaluateFunction(function);
        }

        throw new InvalidOperationException(
            $"Unsupported expression: {expression.GetType().Name}");
    }

    private BasicValue EvaluateFunction(FunctionCallExpression function)
    {
        return function.Name switch
        {
            "KEY" => EvaluateKeyFunction(function),

            _ => throw new InvalidOperationException(
                $"Unknown function {function.Name}.")
        };
    }

    private BasicValue EvaluateKeyFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException(
                "KEY expects one argument.");
        }

        var argument =
            Evaluate(function.Arguments[0]);

        if (!argument.IsString)
        {
            throw new InvalidOperationException(
                "KEY expects a string.");
        }

        var pressed =
            _machine.IsKeyDown(argument.String!);

        return new BasicValue(pressed ? 1 : 0);
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

            TokenType.Equals =>
                left.Integer == right.Integer ? 1 : 0,

            TokenType.NotEqual =>
                left.Integer != right.Integer ? 1 : 0,

            TokenType.LessThan =>
                left.Integer < right.Integer ? 1 : 0,

            TokenType.GreaterThan =>
                left.Integer > right.Integer ? 1 : 0,

            TokenType.LessThanOrEqual =>
                left.Integer <= right.Integer ? 1 : 0,

            TokenType.GreaterThanOrEqual =>
                left.Integer >= right.Integer ? 1 : 0,

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