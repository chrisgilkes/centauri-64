using System;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private string? _pendingInputName;
    private string? _pendingInputPrompt;

    public bool IsWaitingForInput => _pendingInputName != null;

    public void SubmitInput(string text)
    {
        if (_pendingInputName == null ||
            _pendingInputPrompt == null)
        {
            return;
        }

        if (IsStringVariable(_pendingInputName))
        {
            AssignVariable(
                _pendingInputName,
                new BasicValue(text));
        }
        else if (!int.TryParse(text, out var number))
        {
            _machine.Print("?REDO FROM START");
            _machine.BeginInput(_pendingInputPrompt);
            return;
        }
        else
        {
            AssignVariable(
                _pendingInputName,
                new BasicValue(number));
        }

        _pendingInputName = null;
        _pendingInputPrompt = null;
        _programCounter++;
    }

    private ExecutionResult ExecuteInput(InputStatement statement)
    {
        var prompt = "? ";

        if (statement.Prompt != null)
        {
            var value = Evaluate(statement.Prompt);

            if (!value.IsString)
            {
                throw new InvalidOperationException("INPUT prompt must be a string.");
            }

            prompt = value.String + "? ";
        }

        _pendingInputName = statement.VariableName;
        _pendingInputPrompt = prompt;

        _machine.BeginInput(prompt);

        return ExecutionResult.Input();
    }

    private void ClearPendingInput()
    {
        _pendingInputName = null;
        _pendingInputPrompt = null;
    }

    private void AssignVariable(string name, BasicValue value)
    {
        if (IsStringVariable(name))
        {
            if (!value.IsString)
            {
                throw new InvalidOperationException("Type mismatch.");
            }
        }
        else if (!value.IsInteger)
        {
            throw new InvalidOperationException("Type mismatch.");
        }

        _variables[name] = value;
    }

    private int GetNumericVariable(string name)
    {
        var value = GetVariable(name);

        if (!value.IsInteger)
        {
            throw new InvalidOperationException("Expected numeric value.");
        }

        return value.Integer;
    }

    private static bool IsStringVariable(string name)
    {
        return name.EndsWith('$');
    }

    private BasicValue EvaluateStringOperation(
        TokenType op,
        BasicValue left,
        BasicValue right)
    {
        if (op == TokenType.Plus)
        {
            return new BasicValue(left.ToString() + right.ToString());
        }

        if (!left.IsString || !right.IsString)
        {
            throw new InvalidOperationException("Type mismatch.");
        }

        var compare = string.CompareOrdinal(
            left.String ?? string.Empty,
            right.String ?? string.Empty);

        var matches = op switch
        {
            TokenType.Equals => compare == 0,
            TokenType.NotEqual => compare != 0,
            TokenType.LessThan => compare < 0,
            TokenType.GreaterThan => compare > 0,
            TokenType.LessThanOrEqual => compare <= 0,
            TokenType.GreaterThanOrEqual => compare >= 0,
            _ => throw new InvalidOperationException("Type mismatch.")
        };

        return new BasicValue(matches ? 1 : 0);
    }

    private BasicValue EvaluateLenFunction(FunctionCallExpression function)
    {
        var text = RequireString(function, "LEN");

        return new BasicValue(text.Length);
    }

    private BasicValue EvaluateLeftFunction(FunctionCallExpression function)
    {
        var count = RequireCount(function, "LEFT$");
        var text = RequireStringArgument(function, 0, "LEFT$");

        if (count >= text.Length)
        {
            return new BasicValue(text);
        }

        return new BasicValue(text[..count]);
    }

    private BasicValue EvaluateRightFunction(FunctionCallExpression function)
    {
        var count = RequireCount(function, "RIGHT$");
        var text = RequireStringArgument(function, 0, "RIGHT$");

        if (count >= text.Length)
        {
            return new BasicValue(text);
        }

        return new BasicValue(text[^count..]);
    }

    private BasicValue EvaluateMidFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count < 2 ||
            function.Arguments.Count > 3)
        {
            throw new InvalidOperationException(
                "MID$ expects two or three arguments.");
        }

        var text = RequireStringArgument(
            function,
            0,
            "MID$");

        var start = RequireIntegerArgument(
            function,
            1,
            "MID$");

        if (start < 1)
        {
            throw new InvalidOperationException(
                "MID$ start must be 1 or greater.");
        }

        var index = start - 1;

        if (index >= text.Length)
        {
            return new BasicValue(string.Empty);
        }

        var length = text.Length - index;

        if (function.Arguments.Count == 3)
        {
            length = RequireIntegerArgument(
                function,
                2,
                "MID$");

            if (length < 0)
            {
                throw new InvalidOperationException(
                    "MID$ length cannot be negative.");
            }
        }

        if (index + length > text.Length)
        {
            length = text.Length - index;
        }

        return new BasicValue(text.Substring(index, length));
    }

    private BasicValue EvaluateUpperFunction(FunctionCallExpression function)
    {
        var text = RequireString(function, "UPPER$");

        return new BasicValue(text.ToUpperInvariant());
    }

    private string RequireString(
        FunctionCallExpression function,
        string name)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException(
                $"{name} expects one argument.");
        }

        return RequireStringArgument(function, 0, name);
    }

    private int RequireCount(
        FunctionCallExpression function,
        string name)
    {
        if (function.Arguments.Count != 2)
        {
            throw new InvalidOperationException(
                $"{name} expects two arguments.");
        }

        var count = RequireIntegerArgument(function, 1, name);

        if (count < 0)
        {
            throw new InvalidOperationException(
                $"{name} count cannot be negative.");
        }

        return count;
    }

    private string RequireStringArgument(
        FunctionCallExpression function,
        int index,
        string name)
    {
        var value = Evaluate(function.Arguments[index]);

        if (!value.IsString)
        {
            throw new InvalidOperationException(
                $"{name} expects a string.");
        }

        return value.String ?? string.Empty;
    }

    private int RequireIntegerArgument(
        FunctionCallExpression function,
        int index,
        string name)
    {
        var value = Evaluate(function.Arguments[index]);

        if (!value.IsInteger)
        {
            throw new InvalidOperationException(
                $"{name} expects a number.");
        }

        return value.Integer;
    }
}
