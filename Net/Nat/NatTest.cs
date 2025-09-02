using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Net.Nat;
using PCL.Core.Net.Nat.Stun;
using PCL.Core.Utils;

namespace PCL.Core.Net;

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
        var port = BinaryUtils.ToUInt16FromLittleEndian(data, 2);
        IPAddress ip;
        switch (ipFamily)
        {
            case 0x01:
                ip = new IPAddress(BinaryUtils.ToUInt32FromBigEndian(data, 4) ^ BitConverter.ToUInt32(magicCookie));
                break;
            case 0x02:
                ip = new IPAddress(BinaryUtils.ToInt64FromBigEndian(data, 4));
                break;
            default:
                throw new Exception("Unknown IPFamily " + ipFamily);
        }
        return new IPEndPoint(ip, port);
    }

    private IPEndPoint _PraseMappedIpAddress(byte[] data)
    {
        var ipFamily = data[1];
        var port = BinaryUtils.ToUInt16FromBigEndian(data, 2);
        var ipData = new byte[data.Length - 4];
        Buffer.BlockCopy(data, 4, ipData, 0, data.Length - 4);
        return new IPEndPoint(new IPAddress(ipData), port);
    }
}
