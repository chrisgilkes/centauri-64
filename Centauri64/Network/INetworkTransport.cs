using System;

namespace Centauri64.Network;

public interface INetworkTransport : IDisposable
{
    bool IsConnected { get; }
    bool IsHosting { get; }
    bool IsConnecting { get; }

    event Action<byte[]>? MessageReceived;
    event Action? Disconnected;

    void Host();
    void Join();
    void Disconnect();
    void Update();
    void Send(byte[] data);
}
