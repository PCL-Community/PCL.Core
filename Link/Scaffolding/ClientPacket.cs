using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;
using PCL.Core.Logging;

namespace PCL.Core.Link.Scaffolding;

public class ClientPacket
{
    public byte PacketTypeLength {get; private set;}
    public required string PacketType {get; set;}

    public uint BodyLength {get; private set;}
    public required byte[] Body {get; set;}

    public static ClientPacket From(byte[] data) => From(data.AsSpan());

    public static ClientPacket From(Span<byte> data)
    {
        if (data.Length == 0)
            throw new ArgumentException("The data is an empty packet.", nameof(data));

        // Read type length
        var typeLength = data[0];

        // Check we have: 1 byte (typeLen) + typeLength bytes (typeData) + 4 bytes (bodyLen)
        if (data.Length < 1 + typeLength + 4)
            throw new ArgumentException("Insufficient data for type and body length header.", nameof(data));

        // Parse type data
        var typeData = Encoding.UTF8.GetString(data.Slice(1, typeLength));
        if (typeData.Any(char.IsControl))
            throw new ArgumentException("Control characters are not allowed in type data.", nameof(data));

        // Parse body length
        if (!BinaryPrimitives.TryReadUInt32BigEndian(data.Slice(1 + typeLength, 4), out var bodyLength))
            throw new ArgumentException("The body length is invalid.", nameof(data));

        if (bodyLength > int.MaxValue)
            throw new ArgumentException("Body length is larger than the maximum allowed length.", nameof(data));

        var totalExpectedLength = 1 + typeLength + 4 + (int)bodyLength;
        if (data.Length < totalExpectedLength)
            throw new ArgumentException("Packet has insufficient data for the body.", nameof(data));

        if (data.Length > totalExpectedLength)
            LogWrapper.Warn("Scaffolding", "Received a packet with trailing data.");

        var body = data.Slice(1 + typeLength + 4, (int)bodyLength);
        return new ClientPacket()
        {
            BodyLength = bodyLength,
            Body = body.ToArray(),
            PacketTypeLength = typeLength,
            PacketType = typeData
        };
    }

    public byte[] To()
    {
        if (PacketType.Length > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(PacketType));

        // Ensure length property is correct
        PacketTypeLength = (byte)PacketType.Length;
        BodyLength = (uint)Body.Length;

        // build binary packet
        var capacity = 1 + PacketTypeLength + 4 + BodyLength;
        var packet = new byte[capacity].AsSpan();

        // packet type
        packet[0] = PacketTypeLength;
        Encoding.UTF8.GetBytes(PacketType).AsSpan().CopyTo(packet.Slice(1, PacketTypeLength));

        // packet body
        BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(1 + PacketTypeLength, 4), BodyLength);
        Body.AsSpan().CopyTo(packet.Slice(1 + PacketTypeLength + 4));

        return packet.ToArray();
    }
}