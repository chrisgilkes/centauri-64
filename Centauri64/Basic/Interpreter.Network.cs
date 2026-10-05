using System;

using Centauri64.Basic.Syntax;
using Centauri64.Network;

namespace Centauri64.Basic;

public sealed partial class Interpreter
{
    private enum NetworkWaitKind
    {
        None,
        WaitForPeer,
        Join
    }

    private NetworkWaitKind _networkWait = NetworkWaitKind.None;

    private ExecutionResult ExecuteNetHost()
    {
        _network.Host(_programId, _programVersion);
        _networkWait = NetworkWaitKind.None;
        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteNetJoin()
    {
        _network.Join(_programId, _programVersion);
        _networkWait = NetworkWaitKind.Join;
        return ExecutionResult.Wait();
    }

    private ExecutionResult ExecuteNetWait()
    {
        if (!_network.IsHosting && !_network.IsConnected)
        {
            throw new InvalidOperationException("NET WAIT requires an active host.");
        }

        if (_network.IsConnected)
            return ExecutionResult.Continue();

        _networkWait = NetworkWaitKind.WaitForPeer;
        return ExecutionResult.Wait();
    }

    private ExecutionResult ExecuteNetLeave()
    {
        _networkWait = NetworkWaitKind.None;
        _network.Leave();
        return ExecutionResult.Continue();
    }

    private ExecutionResult ExecuteNetSend(NetSendStatement statement)
    {
        var name = Evaluate(statement.Name);

        if (!name.IsString || string.IsNullOrWhiteSpace(name.String))
        {
            throw new InvalidOperationException("NET SEND expects a string name.");
        }

        var value = Evaluate(statement.Value);

        if (!value.IsInteger)
        {
            throw new InvalidOperationException("NET SEND expects a numeric value.");
        }

        _network.SetValue(name.String!, value.Integer);
        return ExecutionResult.Continue();
    }

    private BasicValue EvaluateNetFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 1)
        {
            throw new InvalidOperationException("NET expects one string argument.");
        }

        var name = Evaluate(function.Arguments[0]);

        if (!name.IsString || string.IsNullOrWhiteSpace(name.String))
        {
            throw new InvalidOperationException("NET expects a string name.");
        }

        return new BasicValue(_network.GetValue(name.String!));
    }

    private BasicValue EvaluateNetPlayerFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 0)
        {
            throw new InvalidOperationException("NETPLAYER expects no arguments.");
        }

        return new BasicValue(_network.PlayerNumber);
    }

    private BasicValue EvaluateNetConnectedFunction(FunctionCallExpression function)
    {
        if (function.Arguments.Count != 0)
        {
            throw new InvalidOperationException("NETCONNECTED expects no arguments.");
        }

        return new BasicValue(_network.IsConnected ? 1 : 0);
    }

    private bool IsNetworkWaiting()
    {
        return _networkWait switch
        {
            NetworkWaitKind.WaitForPeer => !_network.IsConnected,
            NetworkWaitKind.Join => !_network.JoinAttemptComplete,
            _ => false
        };
    }
}
