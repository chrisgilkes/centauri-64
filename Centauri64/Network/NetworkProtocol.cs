using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Centauri64.Network;

internal static class NetworkProtocol
{
    public const int ProtocolVersion = 1;

    public const byte Hello = 1;
    public const byte Accept = 2;
    public const byte Reject = 3;
    public const byte State = 4;

    public static byte[] Frame(byte[] payload)
    {
        var framed = new byte[4 + payload.Length];
        BitConverter.TryWriteBytes(framed.AsSpan(0, 4), payload.Length);
        Buffer.BlockCopy(payload, 0, framed, 4, payload.Length);
        return framed;
    }

    public static byte[] BuildHello(string programId, int programVersion)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Hello);
        writer.Write(ProtocolVersion);
        writer.Write(programId ?? string.Empty);
        writer.Write(programVersion);
        return stream.ToArray();
    }

    public static bool TryReadHello(
        byte[] payload,
        out string programId,
        out int programVersion)
    {
        programId = string.Empty;
        programVersion = 0;

        try
        {
            using var stream = new MemoryStream(payload);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            if (reader.ReadByte() != Hello)
                return false;

            var protocol = reader.ReadInt32();
            if (protocol != ProtocolVersion)
                return false;

            programId = reader.ReadString();
            programVersion = reader.ReadInt32();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] BuildAccept() => new[] { Accept };

    public static byte[] BuildReject() => new[] { Reject };

    public static byte[] BuildState(IReadOnlyDictionary<string, int> values)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(State);
        writer.Write(values.Count);

        foreach (var pair in values)
        {
            writer.Write(pair.Key);
            writer.Write(pair.Value);
        }

        return stream.ToArray();
    }

    public static bool TryReadState(
        byte[] payload,
        Dictionary<string, int> target)
    {
        try
        {
            using var stream = new MemoryStream(payload);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            if (reader.ReadByte() != State)
                return false;

            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var value = reader.ReadInt32();
                target[name] = value;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
