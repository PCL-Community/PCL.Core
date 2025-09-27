using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace PCL.Core.Utils.Hash;

public class MurmurHash2Provider : IHashProvider
{
    public static readonly MurmurHash2Provider Instance = new();
    private const int Size = 1024 * 1024;
    private const uint M = 0x5BD1E995u;

    /// <summary>
    /// 计算给定流的 Murmur Hash 2
    /// </summary>
    /// <param name="input">数据流</param>
    /// <returns>该对象对应的 Murmur Hash 2</returns>
    public string ComputeHash(Stream input)
    {
        // 会自动截断溢出
        unchecked
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            using var ms = new MemoryStream();
            input.CopyTo(ms);
            byte[] src = ms.ToArray();

            // 这些字节不能列入计算
            var filtered = new List<byte>(src.Length);
            foreach (var b in src)
            {
                if (b == 9 || b == 10 || b == 13 || b == 32) continue;
                filtered.Add(b);
            }

            int length = filtered.Count;
            uint h = 1u ^ (uint)length;

            int i = 0;

            while (i + 4 <= length)
            {
                uint k = (uint)filtered[i]
                         | (uint)filtered[i + 1] << 8
                         | (uint)filtered[i + 2] << 16
                         | (uint)filtered[i + 3] << 24;

                k *= M;
                k ^= k >> 24;
                k *= M;

                h *= M;
                h ^= k;

                i += 4;
            }

            int rem = length - i;
            switch (rem)
            {
                case 3:
                    h ^= (uint)filtered[i] | (uint)filtered[i + 1] << 8;
                    h ^= (uint)filtered[i + 2] << 16;
                    h *= M;
                    break;
                case 2:
                    h ^= (uint)filtered[i] | (uint)filtered[i + 1] << 8;
                    h *= M;
                    break;
                case 1:
                    h ^= (uint)filtered[i];
                    h *= M;
                    break;
            }

            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;

            return h.ToString();
        }
    }

    /// <summary>
    /// 计算给定字节的 Murmur Hash 2
    /// </summary>
    /// <param name="input">字节</param>
    /// <returns>该对象的 Murmur Hash2</returns>
    public string ComputeHash(byte[] input)
    {
        using MemoryStream stream = new(input);
        return ComputeHash(stream);
    }

    /// <summary>
    /// 计算给定字符串的 Murmru Hash 2
    /// </summary>
    /// <param name="input">字符串</param>
    /// <param name="en">编码</param>
    /// <returns>该对象的 Murmur Hash 2</returns>
    public string ComputeHash(string input, Encoding? en = null)
    {
        using MemoryStream stream = new((en ?? Encoding.UTF8).GetBytes(input));
        return ComputeHash(stream);
    }

    public string ComputeHash(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;
        using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 16384, true);
        return ComputeHash(fs);
    }

}