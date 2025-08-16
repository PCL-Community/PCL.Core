using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Logging;

namespace PCL.Core.Utils.Hash;

public class MD5Provider : IHashProvider
{
    public static MD5Provider Instance { get; } = new MD5Provider();
    
    public string ComputeHash(Stream input)
    {
        var originalPos = input.Position;
        try
        {
            using var hash = MD5.Create();
            var res = hash.ComputeHash(input);
            var sb = new StringBuilder(Length);
            foreach (var b in res)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "Hash", "Compute hash failed");
            throw;
        }
        finally
        {
            input.Position = originalPos;
        }
    }
    public string ComputeHash(byte[] input) => ComputeHash(new MemoryStream(input));
    public string ComputeHash(string input, Encoding? en = null) => ComputeHash(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));

    public int Length => 32;
}