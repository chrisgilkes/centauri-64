using System;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteCls(ClsStatement statement)
    {
        _machine.ClearScreen();

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteInk(InkStatement statement)
    {
        var colour = Evaluate(statement.Colour);

        if (!colour.IsInteger)
        {
            throw new InvalidOperationException("INK expects a number.");
        }

        _machine.SetInk(colour.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecutePaper(PaperStatement statement)
    {
        var colour = Evaluate(statement.Colour);

        if (!colour.IsInteger)
        {
            throw new InvalidOperationException("PAPER expects a number.");
        }

        _machine.SetPaper(colour.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteMode(ModeStatement statement)
    {
        var value = Evaluate(statement.Mode);

        if (!value.IsInteger)
        {
            throw new InvalidOperationException("MODE expects a numeric value.");
        }

        _machine.SetDisplayMode(value.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecutePlot(PlotStatement statement)
    {
        var x = Evaluate(statement.X);
        var y = Evaluate(statement.Y);
        var colour = Evaluate(statement.Colour);

        if (!x.IsInteger ||!y.IsInteger ||!colour.IsInteger)
        {
            throw new InvalidOperationException("PLOT expects numeric values.");
        }

        _machine.Plot(x.Integer,y.Integer,colour.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteLine(LineStatement statement)
    {
        var x1 = Evaluate(statement.X1);
        var y1 = Evaluate(statement.Y1);
        var x2 = Evaluate(statement.X2);
        var y2 = Evaluate(statement.Y2);
        var colour = Evaluate(statement.Colour);

        if (!x1.IsInteger ||!y1.IsInteger ||!x2.IsInteger ||!y2.IsInteger ||!colour.IsInteger)
        {
            throw new InvalidOperationException("LINE expects numeric values.");
        }

        _machine.Line(x1.Integer,y1.Integer,x2.Integer,y2.Integer,colour.Integer);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteRect(RectStatement statement)
    {
        var x = Evaluate(statement.X);
        var y = Evaluate(statement.Y);
        var width = Evaluate(statement.Width);
        var height = Evaluate(statement.Height);
        var colour = Evaluate(statement.Colour);

        if (!x.IsInteger ||!y.IsInteger ||!width.IsInteger ||!height.IsInteger ||!colour.IsInteger)
        {
            throw new InvalidOperationException("RECT expects numeric values.");
        }

        if (width.Integer <= 0 ||height.Integer <= 0)
        {
            throw new InvalidOperationException("RECT width and height must be greater than zero.");
        }

        _machine.Rect(
            x.Integer,
            y.Integer,
            width.Integer,
            height.Integer,
            colour.Integer,
            statement.Filled);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteCircle(CircleStatement statement)
    {
        var x = Evaluate(statement.X);
        var y = Evaluate(statement.Y);
        var radius = Evaluate(statement.Radius);
        var colour = Evaluate(statement.Colour);

        if (!x.IsInteger ||!y.IsInteger ||!radius.IsInteger ||!colour.IsInteger)
        {
            throw new InvalidOperationException(
                "CIRCLE expects numeric values.");
        }

        if (radius.Integer <= 0)
        {
            throw new InvalidOperationException(
                "CIRCLE radius must be greater than zero.");
        }

        _machine.Circle(
            x.Integer,
            y.Integer,
            radius.Integer,
            colour.Integer,
            statement.Filled);

        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecutePrintAt(PrintAtStatement statement)
    {
        var x = Evaluate(statement.X);
        var y = Evaluate(statement.Y);
        var text = Evaluate(statement.Text);

        if (!x.IsInteger || !y.IsInteger)
        {
            throw new InvalidOperationException("PRINTAT coordinates must be numeric.");
        }

        _machine.WriteText(
            x.Integer,
            y.Integer,
            text.ToString());

        return ExecutionResult.Continue();
    }
}