using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace PCL.Core.Net.Nat.Stun;

public class StunResponse
{
    public required IPEndPoint RemoteEndpoint { get; set; }
    public StunMessageType MessageType { get; set; }
    public ushort MessageLength { get; set; }
    public uint MagicCookie { get; set; }
    public required byte[] Transaction { get; set; }
    public required StunAttributesResponse[] Attributes {get; set;}

    public override string ToString()
    {
        var sb = new StringBuilder(MessageLength + 40);
        sb.Append("From Remote:")
            .Append(RemoteEndpoint)
            .Append('\n')
            .Append("Stun Message Type:")
            .Append(MessageType.ToString())
            .Append('\n')
            .Append("Stun Message Length:")
            .Append(MessageLength)
            .Append('\n')
            .Append("Attributes:")
            .Append('\n');
        foreach (var attribute in Attributes)
        {
            Debug.Assert(attribute.Data != null, "attribute.Data != null");
            sb.Append("Stun Attribute Type:")
                .Append(attribute.Type)
                .Append('\n')
                .Append("Stun Attribute Length:")
                .Append(attribute.Length)
                .Append('\n')
                .Append("Stun Attribute Data:")
                .Append(string.Join(' ', attribute.Data.Select(x => $"0x{x:x2}")))
                .Append('\n');
        }
        return sb.ToString();
    }
}