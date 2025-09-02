using System.Net;

namespace PCL.Core.Net.Nat;

public class NatTestResult
{
    public bool IsBehindNat { get; set; }
    public IPEndPoint? PublicEndPoint { get; set; }
    public NatBehaviour MappingBehavior { get; set; }
    public NatBehaviour FilteringBehavior { get; set; }

    public override string ToString()
    {
        return
            $"Public IPEndPoint: {PublicEndPoint}, MappingBehavior: {MappingBehavior}, FilteringBehavior: {FilteringBehavior}";
    }
}