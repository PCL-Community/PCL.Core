using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;

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
        var ms = new MemoryStream(dataLengthWithHeader);
        var writer = new BinaryWriter(ms);

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

        writer.Write(BinaryUtils.GetBigEndianBytes((ushort)_messageType));
        writer.Write(BinaryUtils.GetBigEndianBytes((ushort)dataLength));
        writer.Write(BinaryUtils.GetBigEndianBytes(MagicCookie));
        writer.Write(_transactionId);
        foreach (var tuple in _attributesList)
        {
            writer.Write(_BuildAttributesStruct(tuple.attributes, tuple.content));
        }
        writer.Flush();
        await _client.SendDataAsync(ms.ToArray());
        var stunResponse = await _client.ReceiveDataAsync(cancellationToken);
        var ret = _PraseResponse(stunResponse.Buffer);
        if (ret is null) return null;
        ret.RemoteEndpoint = stunResponse.RemoteEndPoint;
        return ret;
    }

    private StunResponse? _PraseResponse(byte[] data)
    {
        var rMessageType = (StunMessageType)BinaryUtils.ToUInt16FromBigEndian(data);
        var rMessageLength = BinaryUtils.ToUInt16FromBigEndian(data, 2);
        var rMagicCookie = BinaryUtils.ToUInt32FromBigEndian(data, 4);
        var rTransactionId = new byte[12];
        Buffer.BlockCopy(data, 8, rTransactionId, 0, rTransactionId.Length);
        if (!rTransactionId.SequenceEqual(_transactionId)) return null;
        if (rMagicCookie != MagicCookie) return null;
        var attributeOffset = 20;
        var rAttributes = new List<StunAttributesResponse>();
        while (attributeOffset < rMessageLength)
        {
            var addedAttributes = new StunAttributesResponse();
            var rAttributeType = BinaryUtils.ToUInt16FromBigEndian(data, attributeOffset);
            addedAttributes.Type = (StunAttributes)rAttributeType;
            addedAttributes.Length = BinaryUtils.ToUInt16FromBigEndian(data, attributeOffset + 2);
            addedAttributes.Data = new byte[addedAttributes.Length];
            Buffer.BlockCopy(data, attributeOffset + 4, addedAttributes.Data, 0, addedAttributes.Length);
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
        var capacity = 4 + content.Length;
        var data = new byte[capacity];
        Buffer.BlockCopy(BinaryUtils.GetBigEndianBytes((ushort)attributes), 0, data, 0, 2);
        Buffer.BlockCopy(BinaryUtils.GetBigEndianBytes((ushort)content.Length), 0, data, 2, 2);
        Buffer.BlockCopy(content, 0, data, 4, content.Length);
        return data;
    }
}