using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Centauri64.Network;

public sealed class NetworkService : IDisposable
{
    private enum SessionPhase
    {
        Idle,
        Hosting,
        HostHandshaking,
        Joining,
        JoinHandshaking,
        Connected
    }

    private readonly INetworkTransport _transport;
    private readonly Dictionary<string, int> _outgoing = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _dirty = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _incoming = new(StringComparer.OrdinalIgnoreCase);

    private SessionPhase _phase = SessionPhase.Idle;
    private string _programId = string.Empty;
    private int _programVersion = 1;
    private int _playerNumber;
    private long? _joinDeadline;
    private bool _disposed;

    public NetworkService(INetworkTransport? transport = null)
    {
        _transport = transport ?? new LocalNetworkTransport();
        _transport.MessageReceived += OnMessageReceived;
        _transport.Disconnected += OnTransportDisconnected;
    }

    public bool IsConnected => _phase == SessionPhase.Connected;

    public bool IsHosting =>
        _phase is SessionPhase.Hosting or SessionPhase.HostHandshaking ||
        (_phase == SessionPhase.Connected && _playerNumber == 1);

    public int PlayerNumber => IsConnected ? _playerNumber : 0;

    public bool IsWaitingForPeer =>
        _phase is SessionPhase.Hosting or SessionPhase.HostHandshaking;

    public bool IsJoinPending =>
        _phase is SessionPhase.Joining or SessionPhase.JoinHandshaking;

    /// <summary>
    /// True once a NET JOIN attempt has completed (connected or failed).
    /// </summary>
    public bool JoinAttemptComplete { get; private set; }

    public void Host(string programId, int programVersion)
    {
        Leave();

        _programId = programId ?? string.Empty;
        _programVersion = programVersion;
        _playerNumber = 1;
        JoinAttemptComplete = false;

        NetworkLog.Info("HOST requested");
        NetworkLog.Info($"Program ID: {_programId}");
        NetworkLog.Info($"Version: {_programVersion}");

        _transport.Host();
        _phase = SessionPhase.Hosting;
    }

    public void Join(string programId, int programVersion, int timeoutMs = 3000)
    {
        Leave();

        _programId = programId ?? string.Empty;
        _programVersion = programVersion;
        _playerNumber = 2;
        JoinAttemptComplete = false;
        _joinDeadline = Stopwatch.GetTimestamp() +
            (long)(timeoutMs / 1000.0 * Stopwatch.Frequency);

        NetworkLog.Info("JOIN requested");
        NetworkLog.Info($"Program ID: {_programId}");
        NetworkLog.Info($"Version: {_programVersion}");

        _transport.Join();
        _phase = SessionPhase.Joining;
    }

    public void Leave()
    {
        ClearSessionState();
        _transport.Disconnect();
        _phase = SessionPhase.Idle;
        _playerNumber = 0;
        _joinDeadline = null;
        JoinAttemptComplete = false;
        NetworkLog.Info("LEAVE");
    }

    public void SetValue(string name, int value)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var key = name.Trim();
        _outgoing[key] = value;
        _dirty[key] = value;
        NetworkLog.VerboseMessage($"SEND {key}={value}");
    }

    public int GetValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return 0;

        return _incoming.TryGetValue(name.Trim(), out var value)
            ? value
            : 0;
    }

    public void BeginFrame()
    {
        if (_disposed)
            return;

        _transport.Update();
        UpdateJoinTimeout();
        AdvanceHostPhase();
        AdvanceJoinPhase();
    }

    public void EndFrame()
    {
        if (_disposed || !IsConnected)
            return;

        FlushOutgoing();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _transport.MessageReceived -= OnMessageReceived;
        _transport.Disconnected -= OnTransportDisconnected;
        Leave();
        _transport.Dispose();
    }

    private void AdvanceHostPhase()
    {
        if (_phase != SessionPhase.Hosting)
            return;

        if (_transport.IsConnected)
            _phase = SessionPhase.HostHandshaking;
    }

    private void AdvanceJoinPhase()
    {
        if (_phase != SessionPhase.Joining)
            return;

        if (!_transport.IsConnected)
            return;

        _phase = SessionPhase.JoinHandshaking;
        _transport.Send(NetworkProtocol.BuildHello(_programId, _programVersion));
        NetworkLog.Info("HELLO sent");
    }

    private void UpdateJoinTimeout()
    {
        if (_phase is not (SessionPhase.Joining or SessionPhase.JoinHandshaking))
            return;

        if (!_joinDeadline.HasValue)
            return;

        if (Stopwatch.GetTimestamp() < _joinDeadline.Value)
            return;

        NetworkLog.Info("JOIN timed out");
        JoinAttemptComplete = true;
        ClearSessionState();
        _transport.Disconnect();
        _phase = SessionPhase.Idle;
        _playerNumber = 0;
        _joinDeadline = null;
    }

    private void FlushOutgoing()
    {
        if (_dirty.Count == 0)
            return;

        var snapshot = new Dictionary<string, int>(_dirty, StringComparer.OrdinalIgnoreCase);
        _dirty.Clear();
        _transport.Send(NetworkProtocol.BuildState(snapshot));
    }

    private void OnMessageReceived(byte[] payload)
    {
        if (payload.Length == 0)
            return;

        var type = payload[0];

        switch (type)
        {
            case NetworkProtocol.Hello:
                HandleHello(payload);
                break;

            case NetworkProtocol.Accept:
                HandleAccept();
                break;

            case NetworkProtocol.Reject:
                HandleReject();
                break;

            case NetworkProtocol.State:
                HandleState(payload);
                break;
        }
    }

    private void HandleHello(byte[] payload)
    {
        if (_phase is not (SessionPhase.Hosting or SessionPhase.HostHandshaking))
            return;

        if (!NetworkProtocol.TryReadHello(payload, out var programId, out var programVersion))
        {
            NetworkLog.Info("HELLO invalid");
            RejectPeer();
            return;
        }

        NetworkLog.Info("HELLO received");

        if (!string.Equals(programId, _programId, StringComparison.OrdinalIgnoreCase))
        {
            NetworkLog.Info("Program ID mismatch");
            RejectPeer();
            return;
        }

        if (programVersion != _programVersion)
        {
            NetworkLog.Info("Version mismatch");
            RejectPeer();
            return;
        }

        NetworkLog.Info("Program ID matched");
        NetworkLog.Info("Version matched");
        _transport.Send(NetworkProtocol.BuildAccept());
        _playerNumber = 1;
        _phase = SessionPhase.Connected;
        NetworkLog.Info("Player 2 accepted");
    }

    private void HandleAccept()
    {
        if (_phase != SessionPhase.JoinHandshaking)
            return;

        _playerNumber = 2;
        _phase = SessionPhase.Connected;
        JoinAttemptComplete = true;
        _joinDeadline = null;
        NetworkLog.Info("ACCEPT received — Player 2");
    }

    private void HandleReject()
    {
        NetworkLog.Info("REJECT received");
        JoinAttemptComplete = true;
        ClearSessionState();
        _transport.Disconnect();
        _phase = SessionPhase.Idle;
        _playerNumber = 0;
        _joinDeadline = null;
    }

    private void HandleState(byte[] payload)
    {
        if (_phase != SessionPhase.Connected)
            return;

        var received = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!NetworkProtocol.TryReadState(payload, received))
            return;

        foreach (var pair in received)
        {
            _incoming[pair.Key] = pair.Value;
            NetworkLog.VerboseMessage($"RECV {pair.Key}={pair.Value}");
        }
    }

    private void RejectPeer()
    {
        try
        {
            _transport.Send(NetworkProtocol.BuildReject());
        }
        catch
        {
            // ignored
        }

        ClearSessionState();
        _transport.Disconnect();
        _phase = SessionPhase.Idle;
        _playerNumber = 0;
    }

    private void OnTransportDisconnected()
    {
        if (_phase == SessionPhase.Idle)
            return;

        if (_phase is SessionPhase.Joining or SessionPhase.JoinHandshaking)
            JoinAttemptComplete = true;

        ClearValues();
        _phase = SessionPhase.Idle;
        _playerNumber = 0;
        _joinDeadline = null;
    }

    private void ClearSessionState()
    {
        ClearValues();
    }

    private void ClearValues()
    {
        _outgoing.Clear();
        _dirty.Clear();
        _incoming.Clear();
    }
}
