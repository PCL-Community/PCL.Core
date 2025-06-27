using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.Helper;

public static class HashHelper
{
    public static string ComputeMD5(Stream input)
    {
        using var hash = MD5.Create();
        var res = hash.ComputeHash(input);
        var sb = new StringBuilder(32);
        foreach (var b in res)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
    public static string ComputeMD5(byte[] input) => ComputeMD5(new MemoryStream(input));
    public static string ComputeMD5(string input, Encoding? en = null) => ComputeMD5(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));

    public static string ComputeSHA1(Stream input)
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
    public static string ComputeSHA1(byte[] input) => ComputeSHA1(new MemoryStream(input));
    public static string ComputeSHA1(string input, Encoding? en = null) => ComputeSHA1(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));

    public static string ComputeSHA256(Stream input)
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
    public static string ComputeSHA256(byte[] input) => ComputeSHA256(new MemoryStream(input));
    public static string ComputeSHA256(string input, Encoding? en = null) => ComputeSHA256(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));

    public static string ComputeSHA512(Stream input)
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
    public static string ComputeSHA512(byte[] input) => ComputeSHA512(new MemoryStream(input));
    public static string ComputeSHA512(string input, Encoding? en = null) => ComputeSHA512(
        en == null
            ? Encoding.UTF8.GetBytes(input)
            : en.GetBytes(input));
}