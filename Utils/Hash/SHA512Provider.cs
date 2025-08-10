using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.Utils.Hash;

public class SHA512Provider : IHashProvider
{
    public static SHA512Provider Instance { get; } = new SHA512Provider();
    
    public string ComputeHash(Stream input)
    {
        using var hash = SHA512.Create();
        var res = hash.ComputeHash(input);
        var sb = new StringBuilder(128);
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