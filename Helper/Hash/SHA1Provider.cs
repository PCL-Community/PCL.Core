using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.Helper.Hash;

public class SHA1Provider : IHashProvider
{
    public static SHA1Provider Instance { get; } = new SHA1Provider();

    public string ComputeHash(Stream input)
    {
        using var hash = SHA1.Create();
        var res = hash.ComputeHash(input);
        var sb = new StringBuilder(40);
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