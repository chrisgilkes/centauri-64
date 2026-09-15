using System;
using System.Runtime.InteropServices.Marshalling;
using Centauri64.Basic.Syntax;
using Centauri64.Console;

namespace Centauri64.Basic;

public sealed class Interpreter
{
    private readonly TextConsole _console;

    public Interpreter(TextConsole console)
    {
        _console = console;
    }


    public void Run(BasicProgram program)
    {
        foreach(var line in program.Lines)
        {
            Execute(line.Statement);
        }
    }

    private void Execute(Statement statement)
    {
        if(statement is PrintStatement print)
        {
            _console.WriteLine(print.Text);
            return;
        }

        throw new InvalidOperationException($"Unsupported statement: {statement.GetType().Name}");
    }
}