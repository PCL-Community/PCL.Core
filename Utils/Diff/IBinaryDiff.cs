using System.Threading.Tasks;

namespace PCL.Core.Utils.Diff;

public interface IBinaryDiff
{
    public Task<byte[]> Make(byte[] originData, byte[] newData);
    public Task<byte[]> Apply(byte[] originData, byte[] diffData);
}