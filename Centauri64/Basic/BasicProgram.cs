using System.Linq;
using System.Collections.Generic;
using Centauri64.Basic.Syntax;

namespace Centauri64.Basic;

public sealed class BasicProgram
{
    private readonly SortedDictionary<int, ProgramLine> _lines = new();

    public void StoreLine(ProgramLine line)
    {
        _lines[line.LineNumber] = line;
    }

    public IEnumerable<ProgramLine> Lines => _lines.Values;

    public IEnumerable<string> SourceLines => _lines.Values.Select(line => line.Source);

}