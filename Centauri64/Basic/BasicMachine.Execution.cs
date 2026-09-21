using System;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private void RunProgram()
    {
        _interpreter.Start(_program);

        if (!_interpreter.IsRunning)
        {
            OnProgramFinished();
        }
    }

    public void Stop()
    {
        if (!_interpreter.IsRunning)
            return;

        _interpreter.Stop();

        _machine.HideAllSprites();
        _machine.ResetDisplay();

        _console.WriteLine("BREAK");
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    public void Update()
    {
        if (!_interpreter.IsRunning)
            return;

        try
        {
            for (var i = 0;
                 i < INSTRUCTIONS_PER_FRAME &&
                 _interpreter.IsRunning;
                 i++)
            {
                var action =
                    _interpreter.ExecuteNextInstruction();

                if (action == ExecutionAction.Yield ||
                    action == ExecutionAction.Wait)
                {
                    break;
                }
            }

            if (!_interpreter.IsRunning)
            {
                OnProgramFinished();
            }
        }
        catch (Exception exception)
        {
            _interpreter.Stop();

            _console.WriteLine(
                $"?{exception.Message.ToUpperInvariant()}");

            OnProgramFinished();
        }
    }

    private void OnProgramFinished()
    {
        _console.WriteLine("");
        _console.WriteLine("READY.");
    }
}