using System;
using System.Linq;
namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private void ListProgram(string source)
    {
        var argument = source["LIST".Length..].Trim();

        if (string.IsNullOrEmpty(argument))
        {
            foreach (var line in _program.Lines)
            {
                _sourceRenderer.WriteLine(_console,line.Source);
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        if (int.TryParse(argument, out var lineNumber))
        {
            foreach (var line in _program.Lines)
            {
                if (line.LineNumber == lineNumber)
                {
                    _sourceRenderer.WriteLine(_console,line.Source);
                    break;
                }
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        var parts = argument.Split('-', 2);

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var startLine) &&
            int.TryParse(parts[1], out var endLine))
        {
            if (startLine > endLine)
            {
                throw new InvalidOperationException("BAD LIST RANGE");
            }

            foreach (var line in _program.Lines)
            {
                if (line.LineNumber >= startLine &&
                    line.LineNumber <= endLine)
                {
                    _sourceRenderer.WriteLine(_console,line.Source);
                }
            }

            _console.WriteLine("");
            _console.WriteLine("READY.");
            return;
        }

        throw new InvalidOperationException("BAD LIST RANGE");
    }

    private bool TryDeleteLine(string source)
    {
        if (!int.TryParse(source, out var lineNumber))
            return false;

        _program.DeleteLine(lineNumber);

        return true;
    }

    private void UpdateInputHighlighting()
    {
        var source = _console.GetCurrentLine();

        _sourceRenderer.ColourExistingLine(_console,source,_console.CursorRow,0);
    }

    private void EditLine(string source)
    {
        var argument = source["EDIT ".Length..].Trim();

        if (!int.TryParse(argument, out var lineNumber))
        {
            throw new InvalidOperationException("EXPECTED LINE NUMBER");
        }

        var line = _program.Lines
            .FirstOrDefault(line => line.LineNumber == lineNumber);

        if (line == null)
        {
            throw new InvalidOperationException(
                $"LINE {lineNumber} NOT FOUND");
        }

        _console.SetCurrentLine(line.Source);
        UpdateInputHighlighting();
    }
}