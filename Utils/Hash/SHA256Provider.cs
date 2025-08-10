using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.Utils.Hash;

public class SHA256Provider : IHashProvider
{
    public static SHA256Provider Instance { get; } = new SHA256Provider();
    
    public string ComputeHash(Stream input)
    {
        using var hash = SHA256.Create();
        var res = hash.ComputeHash(input);
        var sb = new StringBuilder(64);
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