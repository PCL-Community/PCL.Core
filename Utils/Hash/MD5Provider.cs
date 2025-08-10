using System.IO;
using System.Text;

namespace PCL.Core.Utils.Hash;

public class MD5Provider : IHashProvider
{
    public static MD5Provider Instance { get; } = new MD5Provider();
    
    public string ComputeHash(Stream input)
    {
        using var hash = System.Security.Cryptography.MD5.Create();
        var res = hash.ComputeHash(input);
        var sb = new StringBuilder(32);
        foreach (var b in res)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
    public string ComputeHash(byte[] input) => ComputeHash(new MemoryStream(input));
    public string ComputeHash(string input, Encoding? en = null) => ComputeHash(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));
}