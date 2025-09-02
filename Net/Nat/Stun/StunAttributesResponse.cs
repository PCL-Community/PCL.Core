
namespace PCL.Core.Net.Nat.Stun;

public class StunAttributesResponse
{
    public StunAttributes Type {get; set;}
    public ushort Length {get; set;}
    public byte[]? Data {get; set;}
}