using System;

using Centauri64.Analysis;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private readonly SoftwareAnalyser _analyser = new();

    private void AnalyseCurrentProgram()
    {
        var analysis = _analyser.AnalyseProgram(_program, _tapeName ?? string.Empty);

        _console.WriteLine("");

        foreach (var line in AnalysisReportFormatter.FormatLines(analysis))
        {
            if (line.Length == 0)
            {
                _console.WriteLine("");
                continue;
            }

            _console.WriteLine(line, _editorTheme.SystemColour);
        }

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }
}
