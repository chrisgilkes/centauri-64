using System;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    public int BasicMemoryFree
    {
        get
        {
            var used = _program.GetMemoryUsage();

            return Math.Max(0, _machine.BasicMemoryBytes - used);
        }
    }

    private void ShowMemory()
    {
        var programBytes = _program.GetMemoryUsage();
        var freeBytes = BasicMemoryFree;

        _console.WriteLine("");
        _console.WriteLine("BASIC MEMORY");
        _console.WriteLine("");
        _console.WriteLine($"PROGRAM  {programBytes} BYTES");
        _console.WriteLine($"FREE     {freeBytes} BYTES");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void NewProgram()
    {
        _program.Clear();

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void ClearScreen()
    {
        _console.Clear();

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }
}