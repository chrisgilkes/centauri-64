using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Centauri64.Network;

public sealed class LocalNetworkTransport : INetworkTransport
{
    public const string Address = "127.0.0.1";
    public const int Port = 24640;

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Task? _connectTask;
    private readonly List<byte> _receiveBuffer = new();
    private readonly object _inboxLock = new();
    private readonly Queue<byte[]> _inbox = new();
    private bool _disconnectNotified;
    private bool _disposed;

    public bool IsConnected =>
        _client != null &&
        _client.Connected &&
        _stream != null;

    public bool IsHosting { get; private set; }

    public bool IsConnecting =>
        _connectTask != null &&
        !_connectTask.IsCompleted;

    public event Action<byte[]>? MessageReceived;
    public event Action? Disconnected;

    public void Host()
    {
        Disconnect();

        _listener = new TcpListener(IPAddress.Loopback, Port);
        _listener.Start();
        IsHosting = true;
        NetworkLog.Info($"Listening on {Address}:{Port}");
    }

    public void Join()
    {
        Disconnect();

        IsHosting = false;
        _client = new TcpClient();
        _connectTask = ConnectAsync(_client);
    }

    private static async Task ConnectAsync(TcpClient client)
    {
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, Port)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            NetworkLog.Info($"Connect failed: {exception.Message}");
            throw;
        }
    }

    public void Disconnect()
    {
        IsHosting = false;
        _connectTask = null;

        try
        {
            _stream?.Close();
        }
        catch
        {
            // ignored
        }

        try
        {
            _client?.Close();
        }
        catch
        {
            // ignored
        }

        try
        {
            _listener?.Stop();
        }
        catch
        {
            // ignored
        }

        _stream = null;
        _client = null;
        _listener = null;
        _receiveBuffer.Clear();

        lock (_inboxLock)
        {
            _inbox.Clear();
        }
    }

    public void Update()
    {
        if (_disposed)
            return;

        AcceptPendingClient();
        CompleteOutgoingConnect();
        PumpReceive();
        DeliverInbox();
        DetectDisconnect();
    }

    public void Send(byte[] data)
    {
        if (!IsConnected || _stream == null)
            return;

        try
        {
            var framed = NetworkProtocol.Frame(data);
            _stream.Write(framed, 0, framed.Length);
        }
        catch (Exception exception)
        {
            NetworkLog.Info($"Send failed: {exception.Message}");
            NotifyDisconnected();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disconnect();
    }

    private void AcceptPendingClient()
    {
        if (_listener == null || _client != null)
            return;

        try
        {
            if (!_listener.Pending())
                return;

            _client = _listener.AcceptTcpClient();
            _stream = _client.GetStream();
            _disconnectNotified = false;
            NetworkLog.Info("Client TCP connected");
        }
        catch (Exception exception)
        {
            NetworkLog.Info($"Accept failed: {exception.Message}");
            NotifyDisconnected();
        }
    }

    private void CompleteOutgoingConnect()
    {
        if (_connectTask == null || _client == null)
            return;

        if (!_connectTask.IsCompleted)
            return;

        var task = _connectTask;
        _connectTask = null;

        if (task.IsFaulted || task.IsCanceled || !_client.Connected)
        {
            NetworkLog.Info("Join connect did not succeed");
            Disconnect();
            NotifyDisconnected();
            return;
        }

        try
        {
            _stream = _client.GetStream();
            _disconnectNotified = false;
            NetworkLog.Info("Joined host TCP");
        }
        catch (Exception exception)
        {
            NetworkLog.Info($"Join stream failed: {exception.Message}");
            Disconnect();
            NotifyDisconnected();
        }
    }

    private void PumpReceive()
    {
        if (_stream == null || _client == null)
            return;

        try
        {
            while (_client.Available > 0)
            {
                var buffer = new byte[Math.Min(4096, _client.Available)];
                var read = _stream.Read(buffer, 0, buffer.Length);

                if (read <= 0)
                {
                    NotifyDisconnected();
                    return;
                }

                for (var i = 0; i < read; i++)
                    _receiveBuffer.Add(buffer[i]);

                ExtractFrames();
            }
        }
        catch (Exception exception)
        {
            NetworkLog.Info($"Receive failed: {exception.Message}");
            NotifyDisconnected();
        }
    }

    private void ExtractFrames()
    {
        while (_receiveBuffer.Count >= 4)
        {
            var length = BitConverter.ToInt32(_receiveBuffer.GetRange(0, 4).ToArray(), 0);

            if (length < 0 || length > 1024 * 64)
            {
                NetworkLog.Info("Invalid frame length");
                NotifyDisconnected();
                return;
            }

            if (_receiveBuffer.Count < 4 + length)
                return;

            var payload = _receiveBuffer.GetRange(4, length).ToArray();
            _receiveBuffer.RemoveRange(0, 4 + length);

            lock (_inboxLock)
            {
                _inbox.Enqueue(payload);
            }
        }
    }

    private void DeliverInbox()
    {
        while (true)
        {
            byte[]? payload;

            lock (_inboxLock)
            {
                if (_inbox.Count == 0)
                    return;

                payload = _inbox.Dequeue();
            }

            MessageReceived?.Invoke(payload);
        }
    }

    private void DetectDisconnect()
    {
        if (_client == null || _disconnectNotified)
            return;

        if (!_client.Connected)
        {
            NotifyDisconnected();
            return;
        }

        try
        {
            if (_client.Client.Poll(0, SelectMode.SelectRead) &&
                _client.Available == 0)
            {
                NotifyDisconnected();
            }
        }
        catch
        {
            NotifyDisconnected();
        }
    }

    private void NotifyDisconnected()
    {
        if (_disconnectNotified)
            return;

        _disconnectNotified = true;
        NetworkLog.Info("Connection lost");
        Disconnect();
        Disconnected?.Invoke();
    }
}
