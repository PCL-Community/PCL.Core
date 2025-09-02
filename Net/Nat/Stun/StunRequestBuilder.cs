using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net.Nat.Stun;

public class StunRequestBuilder
{
    public static StunRequestBuilder Create(StunClient client) => new(client);

    private StunClient _client;
    private StunMessageType _messageType = StunMessageType.BindingRequest;
    private readonly List<(StunAttributes attributes, byte[] content)> _attributesList = [];
    private readonly byte[] _transactionId = new byte[12];
    private const uint MagicCookie = 0x2112A442; // RFC 5389 定义的魔术字

    public StunRequestBuilder(StunClient client)
    {
        _client = client;
        Random.Shared.NextBytes(_transactionId);
    }

    public StunRequestBuilder WithMessageType(StunMessageType messageType)
    {
        _messageType = messageType;
        return this;
    }

    public StunRequestBuilder WithAttributes(StunAttributes attributes, byte[] content)
    {
        _attributesList.Add((attributes, content));
        return this;
    }

    public async Task<StunResponse?> GetResponseAsync(CancellationToken cancellationToken)
    {
        var dataLength = _attributesList.Count * 4 + // Attribute header length (4 bytes for each header)
                         _attributesList.Sum(x => x.content.Length); // Attribute value length
        var dataLengthWithHeader = dataLength + 20; // 20 bytes for stun header
        var requestData = new byte[dataLengthWithHeader];
        var requestDataSpan = requestData.AsSpan();

        /*
       0                   1                   2                   3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |0 0|     STUN Message Type     |         Message Length        |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                         Magic Cookie                          |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                                                               |
      |                     Transaction ID (96 bits)                  |
      |                                                               |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         */

        BinaryPrimitives.WriteUInt16BigEndian(requestDataSpan[..2], (ushort)_messageType);
        BinaryPrimitives.WriteUInt16BigEndian(requestDataSpan[2..4], (ushort)dataLength);
        BinaryPrimitives.WriteUInt32BigEndian(requestDataSpan[4..8], MagicCookie);
        _transactionId.AsSpan().CopyTo(requestData.AsSpan(8));
        var offset = 20;
        foreach (var tuple in _attributesList)
        {
            var attributeData = _BuildAttributesStruct(tuple.attributes, tuple.content).AsSpan();
            attributeData.CopyTo(requestData.AsSpan(offset));
            offset += attributeData.Length;
        }
        await _client.SendDataAsync(requestData.ToArray());
        var stunResponse = await _client.ReceiveDataAsync(cancellationToken);
        var ret = _PraseResponse(stunResponse.Buffer);
        if (ret is null) return null;
        ret.RemoteEndpoint = stunResponse.RemoteEndPoint;
        return ret;
    }

    private StunResponse? _PraseResponse(byte[] data)
    {
        var dataSpan = data.AsSpan();
        var rMessageType = (StunMessageType)BinaryPrimitives.ReadUInt16BigEndian(dataSpan[..2]);
        var rMessageLength = BinaryPrimitives.ReadUInt16BigEndian(dataSpan[2..4]);
        var rMagicCookie = BinaryPrimitives.ReadUInt32BigEndian(dataSpan[4..8]);
        var rTransactionId = data[8..20];
        if (!rTransactionId.SequenceEqual(_transactionId)) return null;
        if (rMagicCookie != MagicCookie) return null;
        var attributeOffset = 20;
        var rAttributes = new List<StunAttributesResponse>();
        while (attributeOffset < rMessageLength)
        {
            var addedAttributes = new StunAttributesResponse();
            var rAttributeType = BinaryPrimitives.ReadUInt16BigEndian(dataSpan[attributeOffset..(attributeOffset + 2)]);
            addedAttributes.Type = (StunAttributes)rAttributeType;
            addedAttributes.Length =
                BinaryPrimitives.ReadUInt16BigEndian(dataSpan[(attributeOffset + 2)..(attributeOffset + 4)]);
            addedAttributes.Data = data[(attributeOffset + 4)..(attributeOffset + 4 + addedAttributes.Length)];
            rAttributes.Add(addedAttributes);
            attributeOffset += addedAttributes.Length + 4;
        }
        return new StunResponse
        {
            MessageType = rMessageType,
            MessageLength = rMessageLength,
            MagicCookie = rMagicCookie,
            Attributes = rAttributes.ToArray(),
            Transaction = rTransactionId,
            RemoteEndpoint = null!,
        };
    }

    private static byte[] _BuildAttributesStruct(StunAttributes attributes, byte[] content)
    {
        /*
       0                   1                   2                   3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |         Type                  |            Length             |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                         Value (variable)                ....
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         */
        const int headerSize = 4;
        var capacity = headerSize + content.Length;
        var data = new byte[capacity];
        var dataSpan = data.AsSpan();
        BinaryPrimitives.WriteUInt16BigEndian(dataSpan[..2], (ushort)attributes);
        BinaryPrimitives.WriteUInt16BigEndian(dataSpan[2..4], (ushort)content.Length);
        content.AsSpan().CopyTo(data.AsSpan(headerSize));
        return data;
    }
}