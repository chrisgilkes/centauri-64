using System;

namespace Centauri64.Basic;

public sealed partial class BasicMachine
{
    private bool _programFinished;

    private string? _runtimeError;

    public bool ProgramFinished => _programFinished;

    public bool IsWaitingForInput => _interpreter.IsWaitingForInput;

    public void SubmitInput(string text)
    {
        if (!IsWaitingForInput)
            return;

        _interpreter.SubmitInput(text);
    }

    public void DismissFinishedProgram()
    {
        if (!_programFinished)
            return;

        _programFinished = false;

        _machine.HideAllSprites();
        _machine.ResetDisplay();

        if (_runtimeError != null)
        {
            _console.WriteLine(_runtimeError);
            _runtimeError = null;
        }

        _console.WriteLine("");
        _console.WriteLine("READY.");
    }

    private void RunProgram()
    {
        _programFinished = false;
        _runtimeError = null;

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

        _programFinished = false;

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
                 i < MAX_INSTRUCTIONS_PER_FRAME &&
                 _interpreter.IsRunning;
                 i++)
            {
                var action =
                    _interpreter.ExecuteNextInstruction();

                if (action == ExecutionAction.Yield ||
                    action == ExecutionAction.Wait ||
                    action == ExecutionAction.Input)
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

            _runtimeError =
                $"?{exception.Message.ToUpperInvariant()}";

            _machine.Print(_runtimeError);

            OnProgramFinished();
        }
    }

    private void OnProgramFinished()
    {
        _programFinished = true;
    }
    
}