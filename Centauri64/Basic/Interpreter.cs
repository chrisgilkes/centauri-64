using System;
using System.Collections.Generic;
using System.Diagnostics;

using Centauri64.Basic.Syntax;
using Centauri64.Console;
using Centauri64.Machine;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private readonly TextConsole _console;
    private readonly Dictionary<string, int> _variables = new();
    private readonly Dictionary<string, int[]> _arrays = new();
    private readonly Random _random = new();

    private const int MAX_INSTRUCTIONS_WITHOUT_YIELD  = 100_000;

    private IReadOnlyList<ProgramLine> _lines = [];
    private int _programCounter;
    private int _instructionCount;
    private long? _waitUntil;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    private readonly CentauriMachine _machine;

    private readonly Stack<int> _returnStack = new();

    private sealed class ForLoop
    {
        public string VariableName { get; }
        public int End { get; }
        public int Step { get; }
        public int LoopStartProgramCounter { get; }

        public ForLoop(
            string variableName,
            int end,
            int step,
            int loopStartProgramCounter)
        {
            VariableName = variableName;
            End = end;
            Step = step;
            LoopStartProgramCounter = loopStartProgramCounter;
        }
    }

    private readonly Stack<ForLoop> _forStack = new();

    public Interpreter(TextConsole console, CentauriMachine machine)
    {
        _console = console;
        _machine = machine;
    }

    public void Start(BasicProgram program)
    {
        _waitUntil = null;
        _machine.ResetProgramDisplay();

        _variables.Clear();
        _returnStack.Clear();
        _forStack.Clear();
        _arrays.Clear();

        _lines = program.GetLines();
        _programCounter = 0;
        _instructionCount = 0;

        _isRunning = _lines.Count > 0;
    }

    public ExecutionAction  ExecuteNextInstruction()
    {
        if (!_isRunning)
            return ExecutionAction.Continue;

        if (_waitUntil.HasValue)
        {
            if (Stopwatch.GetTimestamp() < _waitUntil.Value)
            {
                return ExecutionAction.Wait;
            }

            _waitUntil = null;
        }

        if (_programCounter >= _lines.Count)
        {
            Stop();
            return ExecutionAction.Continue;
        }

        if (++_instructionCount > MAX_INSTRUCTIONS_WITHOUT_YIELD)
        {
            Stop();

            throw new InvalidOperationException("Program execution limit exceeded.");
        }

        var line = _lines[_programCounter];

        var result = Execute(line.Statement);

        switch (result.Action)
        {
            case ExecutionAction.Continue:
                _programCounter++;
                break;

            case ExecutionAction.Jump:
                _programCounter = FindLine(_lines,result.JumpToLine!.Value);
                break;

            case ExecutionAction.Yield:
                _programCounter++;
                _instructionCount = 0;
                break;
                
            case ExecutionAction.Wait:
                _programCounter++;
                _instructionCount = 0;
                break;

            case ExecutionAction.Return:
                _programCounter = _returnStack.Pop();
                break;

            case ExecutionAction.JumpToProgramCounter:
                _programCounter =
                result.ProgramCounter!.Value;
            break;
        }

        if (_programCounter >= _lines.Count &&
            !_waitUntil.HasValue)
        {
            Stop();
        }

        return result.Action;
    }

    public void Stop()
    {
        _isRunning = false;
        _waitUntil = null;
    }

    public void Run(BasicProgram program)
    {
        _returnStack.Clear();
        _variables.Clear();
        _forStack.Clear();
        _arrays.Clear();

        var lines = program.GetLines();

        var programCounter = 0;
        var instructionCount = 0;

        while(programCounter < lines.Count)
        {
            if(++instructionCount > MAX_INSTRUCTIONS_WITHOUT_YIELD)
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
        if (statement is PrintStatement print)
        {
            var value = Evaluate(print.Expression);

            _machine.Print(value.ToString());

            return ExecutionResult.Continue();
        }

        if (statement is PrintAtStatement printAt)
        {
            return ExecutePrintAt(printAt);
        }

        if (statement is ClsStatement cls)
        {
            return ExecuteCls(cls);
        }

        if (statement is InkStatement ink)
        {
            return ExecuteInk(ink);
        }

        if (statement is PaperStatement paper)
        {
            return ExecutePaper(paper);
        }

        if (statement is BorderStatement border)
        {
            return ExecuteBorder(border);
        }

        if (statement is ModeStatement mode)
        {    
            return ExecuteMode(mode);
        }

        if (statement is PlotStatement plot)
        {
            return ExecutePlot(plot);
        }

        if (statement is LineStatement line)
        {
            return ExecuteLine(line);
        }

        if (statement is RectStatement rect)
        {    
            return ExecuteRect(rect);
        }

        if (statement is CircleStatement circle)
        {    
            return ExecuteCircle(circle);
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
                throw new InvalidOperationException("IF condition must be numeric.");
            }

            if (condition.Integer != 0)
            {
                return Execute(ifStatement.ThenStatement);
            }

            return ExecutionResult.Continue();
        }

        if (statement is YieldStatement)
        {
            return ExecutionResult.Yield();
        }

        if (statement is SpriteStatement sprite)
        {
            return ExecuteSprite(sprite);
        }

        if (statement is SpritePositionStatement spritePosition)
        {
            return ExecuteSpritePosition(spritePosition);
        }

        if (statement is SpriteShowStatement spriteShow)
        {
            return ExecuteSpriteShow(spriteShow);
        }

        if (statement is SpriteHideStatement spriteHide)
        {
            return ExecuteSpriteHide(spriteHide);
        }

        if( statement is ResetStatement reset)
        {
            _machine.ResetDisplay();
        }

        if (statement is GosubStatement gosubStatement)
        {
            _returnStack.Push(_programCounter + 1);

            return ExecutionResult.Jump(gosubStatement.LineNumber);
        }

        if (statement is ReturnStatement)
        {
            if (_returnStack.Count == 0)
            {
                throw new InvalidOperationException("RETURN without GOSUB.");
            }

            return ExecutionResult.Return();
        }

        if (statement is BeepStatement beep)
        {
            var frequency = Evaluate(beep.Frequency);

            var duration = Evaluate(beep.Duration);

            if (!frequency.IsInteger ||
                !duration.IsInteger)
            {
                throw new InvalidOperationException("BEEP expects numeric values.");
            }

            _machine.Beep(frequency.Integer,duration.Integer);

            return ExecutionResult.Continue();
        }

        if (statement is WaitStatement wait)
        {
            var duration = Evaluate(wait.Duration);

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

        if (statement is ForStatement forStatement)
        {
            var start = Evaluate(forStatement.Start);
            var end = Evaluate(forStatement.End);

            if (!start.IsInteger || !end.IsInteger)
            {
                throw new InvalidOperationException("FOR expects numeric values.");
            }

            var step = 1;

            if (forStatement.Step != null)
            {
                var stepValue = Evaluate(forStatement.Step);

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

            _variables[forStatement.VariableName] = start.Integer;

            var shouldRun = step > 0 ? start.Integer <= end.Integer : start.Integer >= end.Integer;

            if (!shouldRun)
            {
                var depth = 0;

                for (var i = _programCounter + 1; i < _lines.Count; i++)
                {
                    if (_lines[i].Statement is ForStatement)
                    {
                        depth++;
                    }
                    else if (_lines[i].Statement is NextStatement)
                    {
                        if (depth == 0)
                        {
                            return ExecutionResult.JumpToProgramCounter(i + 1);
                        }

                        depth--;
                    }
                }

                throw new InvalidOperationException($"FOR {forStatement.VariableName} without NEXT.");
            }

            _forStack.Push(
                new ForLoop(
                    forStatement.VariableName,
                    end.Integer,
                    step,
                    _programCounter + 1));

            return ExecutionResult.Continue();
        }

        if (statement is NextStatement next)
        {
            if (_forStack.Count == 0)
            {
                throw new InvalidOperationException("NEXT without FOR.");
            }

            var loop = _forStack.Peek();

            if (loop.VariableName != next.VariableName)
            {
                throw new InvalidOperationException($"NEXT {next.VariableName} does not match FOR {loop.VariableName}.");
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

        if (statement is DimStatement dim)
        {
            var size = Evaluate(dim.Size);

            if (!size.IsInteger)
            {
                throw new InvalidOperationException("DIM size must be numeric.");
            }

            if (size.Integer <= 0)
            {
                throw new InvalidOperationException("DIM size must be greater than zero.");
            }

            _arrays[dim.Name] = new int[size.Integer];

            return ExecutionResult.Continue();
        }

        if (statement is ArrayAssignmentStatement arrayAssignment)
        {
            var index = Evaluate(arrayAssignment.Index);
            var value = Evaluate(arrayAssignment.Value);

            if (!index.IsInteger || !value.IsInteger)
            {
                throw new InvalidOperationException("Array assignment expects numeric values.");
            }

            if (!_arrays.TryGetValue(arrayAssignment.Name, out var array))
            {
                throw new InvalidOperationException($"Array {arrayAssignment.Name} has not been DIMensioned.");
            }

            if (index.Integer < 0 || index.Integer >= array.Length)
            {
                throw new InvalidOperationException($"Array index out of range: {arrayAssignment.Name}({index.Integer}).");
            }

            array[index.Integer] = value.Integer;

            return ExecutionResult.Continue();
        }

        if (statement is RemStatement)
        {
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
            return new BasicValue(GetVariable(variable.Name));
        }

        if (expression is UnaryExpression unary)
        {
            var value = Evaluate(unary.Operand);

            if (!value.IsInteger)
            {
                throw new InvalidOperationException("Unary minus requires a numeric value.");
            }

            if (unary.Operator == TokenType.Minus)
            {
                return new BasicValue(-value.Integer);
            }

            throw new InvalidOperationException($"Unsupported unary operator: {unary.Operator}");
        }

        if (expression is BinaryExpression binary)
        {
            return EvaluateBinary(binary);
        }

        if (expression is FunctionCallExpression function)
        {
            return EvaluateFunction(function);
        }

        if (expression is ArrayAccessExpression arrayAccess)
        {
            var index = Evaluate(arrayAccess.Index);

            if (!index.IsInteger)
            {
                throw new InvalidOperationException("Array index must be numeric.");
            }

            if (!_arrays.TryGetValue(arrayAccess.Name, out var array))
            {
                throw new InvalidOperationException($"Array {arrayAccess.Name} has not been DIMensioned.");
            }

            if (index.Integer < 0 || index.Integer >= array.Length)
            {
                throw new InvalidOperationException($"Array index out of range: {arrayAccess.Name}({index.Integer}).");
            }

            return new BasicValue(array[index.Integer]);
        }

        throw new InvalidOperationException($"Unsupported expression: {expression.GetType().Name}");
    }

    private BasicValue EvaluateFunction(FunctionCallExpression function)
    {
        return function.Name switch
        {
            "KEY" => EvaluateKeyFunction(function),
            "RND" => EvaluateRandomFunction(function),
            "COLLIDE" => EvaluateCollideFunction(function),
            "SWIDTH" => EvaluateScreenWidthFunction(function),
            "SHEIGHT" => EvaluateScreenHeightFunction(function),
            "KEYPRESSED" => EvaluateKeyPressedFunction(function),
            _ => throw new InvalidOperationException($"Unknown function {function.Name}.")
        };
    }

    private BasicValue EvaluateKeyPressedFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException("KEYPRESSED expects one argument.");
        }

        var argument = Evaluate(function.Arguments[0]);

        if (!argument.IsString)
        {
            throw new InvalidOperationException("KEYPRESSED expects a string.");
        }

        var pressed = _machine.IsKeyPressed(argument.String!);

        return new BasicValue(pressed ? 1 : 0);
    }

    private BasicValue EvaluateScreenWidthFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 0)
        {
            throw new InvalidOperationException("SWIDTH expects no arguments.");
        }

        return new BasicValue(_machine.ScreenWidth);
    }

    private BasicValue EvaluateScreenHeightFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 0)
        {
            throw new InvalidOperationException("SHEIGHT expects no arguments.");
        }

        return new BasicValue(_machine.ScreenHeight);
    }


    private BasicValue EvaluateCollideFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 2)
        {
            throw new InvalidOperationException("COLLIDE expects two arguments.");
        }

        var first = Evaluate(function.Arguments[0]);

        var second = Evaluate(function.Arguments[1]);

        if (!first.IsInteger ||
            !second.IsInteger)
        {
            throw new InvalidOperationException("COLLIDE expects sprite numbers.");
        }

        var collided =
            _machine.SpritesCollide(
                first.Integer,
                second.Integer);

        return new BasicValue(
            collided ? 1 : 0);
    }

    private BasicValue EvaluateKeyFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException("KEY expects one argument.");
        }

        var argument = Evaluate(function.Arguments[0]);

        if (!argument.IsString)
        {
            throw new InvalidOperationException("KEY expects a string.");
        }

        var pressed = _machine.IsKeyDown(argument.String!);

        return new BasicValue(pressed ? 1 : 0);
    }

    private BasicValue EvaluateRandomFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException("RND expects one argument.");
        }

        var argument = Evaluate(function.Arguments[0]);

        if (!argument.IsInteger)
        {
            throw new InvalidOperationException("RND expects a number.");
        }

        if (argument.Integer <= 0)
        {
            throw new InvalidOperationException("RND expects a positive number.");
        }

        return new BasicValue(_random.Next(argument.Integer));
    }

    private BasicValue EvaluateBinary(BinaryExpression expression)
    {
        var left = Evaluate(expression.Left);
        var right = Evaluate(expression.Right);

        if (!left.IsInteger || !right.IsInteger)
        {
            throw new InvalidOperationException("Arithmetic requires numeric values.");
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