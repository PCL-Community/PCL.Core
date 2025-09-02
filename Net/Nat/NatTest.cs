using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Net.Nat.Stun;

namespace PCL.Core.Net.Nat;

public class NatTest(StunClient stun)
{
    public async Task<IPEndPoint?> GetPublicEndPointAsync(CancellationToken cancellationToken = default)
    {
        var response = await StunRequestBuilder.Create(stun)
            .WithMessageType(StunMessageType.BindingRequest)
            .GetResponseAsync(cancellationToken)
            .ConfigureAwait(false);
        if (response is null) return null;
        var xorIpAttribute =
            response.Attributes.FirstOrDefault(x => x.Type == StunAttributes.XorMappedAddressAttribute);
        if (xorIpAttribute?.Data is not null)
            return _PraseXorIpAddress(xorIpAttribute.Data, BitConverter.GetBytes(response.MagicCookie), response.Transaction);
        var mapIpAttribute = response.Attributes.FirstOrDefault(x => x.Type == StunAttributes.MappedAddress);
        if (mapIpAttribute?.Data is not null)
            return _PraseMappedIpAddress(mapIpAttribute.Data);
        return null;
    }

    private IPEndPoint _PraseXorIpAddress(byte[] data, byte[] magicCookie, byte[] transaction)
    {
        var ipFamily = data[1];
        var dataSpan = data.AsSpan();
        var port = BinaryPrimitives.ReadUInt16BigEndian(dataSpan[2..4]);
        IPAddress ip;
        switch (ipFamily)
        {
            case 0x01:
                ip = new IPAddress(BinaryPrimitives.ReadUInt32BigEndian(dataSpan[4..]) ^ BitConverter.ToUInt32(magicCookie));
                break;
            case 0x02:
                byte[] xorKey = [..magicCookie, ..transaction];
                for (var i = 0; i < xorKey.Length; i++)
                {
                    data[i] ^= xorKey[i];
                }

                ip = new IPAddress(BinaryPrimitives.ReadInt64BigEndian(dataSpan[4..]));
                break;
            default:
                throw new Exception("Unknown IPFamily " + ipFamily);
        }
        return new IPEndPoint(ip, port);
    }

    private IPEndPoint _PraseMappedIpAddress(byte[] data)
    {
        var ipFamily = data[1];
        var dataSpan = data.AsSpan();
        var port = BinaryPrimitives.ReadUInt16BigEndian(dataSpan[2..4]);
        var ipData = data[4..];
        return new IPEndPoint(new IPAddress(ipData), port);
    }
}
