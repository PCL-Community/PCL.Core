using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Link.Scaffolding.Network;

public class ClientPacketHandler : IDisposable
{
    private readonly Stream _stream;
    private readonly byte[] _lengthBuffer = new byte[5]; // 1 (typeLen) + 4 (bodyLen)
    private bool _disposed;

    public ClientPacketHandler(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));
    }

    public async Task<ClientPacket> ReceivePacketAsync()
    {
        _ThrowIfDisposed();

        // Step 1: Read the first byte -> PacketTypeLength
        int read = await _ReadExactlyAsync(_lengthBuffer, 0, 1).ConfigureAwait(false);
        if (read == 0)
            throw new EndOfStreamException("Cannot read packet type length: stream ended unexpectedly.");

        byte typeLength = _lengthBuffer[0];

        if (typeLength == 0)
            throw new InvalidDataException("Packet type length cannot be zero.");

        // Step 2: Read the type string (UTF-8 bytes of length 'typeLength')
        var typeBuffer = new byte[typeLength];
        await _ReadExactlyAsync(typeBuffer, 0, typeLength).ConfigureAwait(false);

        string packetType;
        try
        {
            packetType = Encoding.UTF8.GetString(typeBuffer);
            if (packetType.Length == 0 || packetType.Any(char.IsControl))
                throw new InvalidDataException("Invalid or control character in packet type.");
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("Packet type is not valid UTF-8.", ex);
        }

        // Step 3: Read body length (4 bytes, big-endian uint)
        await _ReadExactlyAsync(_lengthBuffer, 0, 4).ConfigureAwait(false);
        if (!BinaryPrimitives.TryReadUInt32BigEndian(_lengthBuffer.AsSpan(0, 4), out uint bodyLength))
            throw new InvalidDataException("Failed to read valid body length.");

        if (bodyLength > int.MaxValue)
            throw new InvalidDataException("Body length exceeds maximum allowed size (int.MaxValue).");

        // Step 4: Read the body data
        var body = new byte[bodyLength];
        if (bodyLength > 0)
        {
            await _ReadExactlyAsync(body, 0, (int)bodyLength).ConfigureAwait(false);
        }

        // Construct and return the packet
        return new ClientPacket
        {
            PacketTypeLength = typeLength,
            PacketType = packetType,
            BodyLength = bodyLength,
            Body = body
        };
    }

    private async Task<int> _ReadExactlyAsync(byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await _stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead)).ConfigureAwait(false);
            if (read == 0)
            {
                if (totalRead == 0)
                    return 0; // EOF at expected position
                throw new EndOfStreamException($"Expected {count} bytes but only read {totalRead} before stream ended.");
            }
            totalRead += read;
        }
        return totalRead;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void _ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ClientPacketHandler));
    }
}