using System;
using System.Buffers.Binary;
using PCL.Core.Logging;

namespace PCL.Core.Link.Protocols.Scaffolding.Packet;

public class ServerPacket
{
    public byte StatusCode { get; set; }
    public uint BodyLength { get; private set; }
    public required byte[] Body { get; set; }

    public static ServerPacket From(byte[] data) => From(data.AsSpan());

    public static ServerPacket From(Span<byte> data)
    {
        if (data.Length == 0) throw new ArgumentException("The data is an empty packet.", nameof(data));

        var packetStatus = data[0];

        // Ensure we have: 1 byte (status code) + 4 bytes (body length) + body
        if (data.Length <= 5)
            throw new ArgumentException("Insufficient data for status code and body length header.", nameof(data));

        if (!BinaryPrimitives.TryReadUInt32BigEndian(data.Slice(1, 4), out var bodyLength))
            throw new ArgumentException("The body length is invalid.", nameof(data));

        var totalExpectedLength = 1 + 4 + (int)bodyLength;
        if (data.Length < totalExpectedLength)
            throw new ArgumentException("Packet has insufficient data for the body.", nameof(data));

        if (data.Length > totalExpectedLength)
            LogWrapper.Warn("Scaffolding", "Received a packet with trailing data.");

        if (bodyLength > int.MaxValue)
            throw new ArgumentException("Body length is larger than the maximum allowed length.", nameof(data));

        var body = data.Slice(5, (int)bodyLength);

        return new ServerPacket
        {
            StatusCode = packetStatus,
            BodyLength = bodyLength,
            Body = body.ToArray()
        };
    }

    public byte[] To()
    {
        // Ensure the property is correct
        BodyLength = (uint)Body.Length;

        var capacity = 5L + BodyLength;
        var packet = new byte[capacity].AsSpan();

        packet[0] = StatusCode;

        BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(1,4), BodyLength);
        Body.AsSpan().CopyTo(packet[5..]);

        return packet.ToArray();
    }
}