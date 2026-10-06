using System;

using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private ExecutionResult ExecuteImage(ImageStatement statement)
    {
        if (statement.Off)
        {
            _machine.ClearImageBlits();
            return ExecutionResult.Continue();
        }

        var name = Evaluate(statement.Name!);
        if (!name.IsString)
            throw new InvalidOperationException("IMAGE NAME MUST BE A STRING");

        var x = 0;
        var y = 0;
        var frame = 0;

        if (statement.X != null)
        {
            var xv = Evaluate(statement.X);
            if (!xv.IsInteger)
                throw new InvalidOperationException("IMAGE X MUST BE NUMERIC");
            x = xv.Integer;
        }

        if (statement.Y != null)
        {
            var yv = Evaluate(statement.Y);
            if (!yv.IsInteger)
                throw new InvalidOperationException("IMAGE Y MUST BE NUMERIC");
            y = yv.Integer;
        }

        if (statement.Frame != null)
        {
            var fv = Evaluate(statement.Frame);
            if (!fv.IsInteger)
                throw new InvalidOperationException("IMAGE FRAME MUST BE NUMERIC");
            frame = fv.Integer;
        }

        _machine.BlitImage(name.String!, x, y, frame);
        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteBg(BgStatement statement)
    {
        var layer = Evaluate(statement.Layer);
        if (!layer.IsInteger)
            throw new InvalidOperationException("BACKGROUND LAYER MUST BE NUMERIC");

        if (statement.Off)
        {
            _machine.HideBackground(layer.Integer);
            return ExecutionResult.Continue();
        }

        var name = Evaluate(statement.Name!);
        if (!name.IsString)
            throw new InvalidOperationException("BACKGROUND NAME MUST BE A STRING");

        _machine.SetBackground(layer.Integer, name.String!);
        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteFg(FgStatement statement)
    {
        if (statement.Off)
        {
            _machine.HideForeground();
            return ExecutionResult.Continue();
        }

        var name = Evaluate(statement.Name!);
        if (!name.IsString)
            throw new InvalidOperationException("FOREGROUND NAME MUST BE A STRING");

        _machine.SetForeground(name.String!);
        return ExecutionResult.Continue();
    }
}
