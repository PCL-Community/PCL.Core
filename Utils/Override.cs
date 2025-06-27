namespace PCL.Core.Utils;
using System.Text;

public static class Override
{
    public static byte[] GetBytes(this string content,Encoding? encode = null)
    {
        return (encode ?? Encoding.UTF8).GetBytes(content);
    }
    public static string GetString(this byte[] content,Encoding? encode = null)
    {
        return (encode ?? Encoding.UTF8).GetString(content);
    }
}