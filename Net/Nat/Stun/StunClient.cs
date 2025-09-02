using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net.Nat.Stun;

public class StunClient(string serverAddress = "stun.miwifi.com", int serverPort = 3478)
{
    private readonly UdpClient _client = new(serverAddress, serverPort);

    public async Task SendDataAsync(byte[] data)
    {
        await _client.SendAsync(data, data.Length);
    }

    public async Task<UdpReceiveResult> ReceiveDataAsync(CancellationToken cancellationToken)
    {
        return await _client.ReceiveAsync(cancellationToken);
    }
}