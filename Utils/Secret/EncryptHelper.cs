using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Utils.Encryption;

namespace PCL.Core.Utils.Secret;

public static class EncryptHelper
{
    public static string SecretEncrypt(string data)
    {
        var rawData = Encoding.UTF8.GetBytes(data);
        var encryptedData = ChaCha20.Instance.Encrypt(rawData, Identify.EncryptionKey.Value);
        return Convert.ToBase64String(encryptedData);
    }

    public static string SecretDecrypt(string data)
    {
        var rawData = Convert.FromBase64String(data);
        var decryptedData = ChaCha20.Instance.Decrypt(rawData, Identify.EncryptionKey.Value);
        return Encoding.UTF8.GetString(decryptedData);
    }

    #region "加密存储信息数据"


    public struct EncryptionData
    {
        public uint Verison;
        public byte[] Data;

        private const uint MagicNumber = 0x454E4321;

        public static EncryptionData FromBase64(string base64)
        {
            return FromBytes(Convert.FromBase64String(base64));
        }

        public static EncryptionData FromBytes(ReadOnlySpan<byte> bytes)
        {
            // 4 bytes MagicNumber + 4 bytes version + 4 bytes rData length + n bytes rData
            if (bytes.Length < 12)
                throw new ArgumentException("No enough data for EncryptionData", nameof(bytes));

            if (BinaryPrimitives.ReadUInt32BigEndian(bytes[..4]) != MagicNumber)
                throw new ArgumentException("Unknown data for EncryptionData", nameof(bytes));

            var dataLength = BinaryPrimitives.ReadInt32BigEndian(bytes[8..12]);
            var rData = bytes[12..(12 + dataLength)];

            return new EncryptionData
            {
                Verison = BinaryPrimitives.ReadUInt32BigEndian(bytes[4..8]),
                Data = rData.ToArray()
            };
        }

        public static byte[] ToBytes(EncryptionData encryptionData)
        {
            var length = 12 + encryptionData.Data.Length;
            var bytes = new byte[length];
            var bytesSpan = bytes.AsSpan();
            BinaryPrimitives.WriteUInt32BigEndian(bytesSpan[..4], MagicNumber);
            BinaryPrimitives.WriteUInt32BigEndian(bytesSpan[4..8], encryptionData.Verison);
            BinaryPrimitives.WriteInt32BigEndian(bytesSpan[8..12], encryptionData.Data.Length);
            encryptionData.Data.CopyTo(bytesSpan[12..]);

            return bytes;
        }
    }

    #endregion

    #region "旧版本兼容"

    [Obsolete]
    public static string SecretDecryptOld(string data) => AesDecrypt(data, IdentifyOld.EncryptKey);

    [Obsolete]
    public static string SecretEncryptOld(string data) => AesEncrypt(data, IdentifyOld.EncryptKey);

    /// <summary>
    /// 使用特定的 AES 算法加密数据
    /// </summary>
    /// <param name="input">需要加密的数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Base64 编码的加密数据</returns>
    /// <exception cref="ArgumentNullException">如果 key 为 null 或者空</exception>
    [Obsolete]
    public static string AesEncrypt(string input, string key)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var salt = new byte[32];
#if NET6_0_OR_GREATER
        using (var rng = RandomNumberGenerator.Create())
#else
        using (var rng = new RNGCryptoServiceProvider())
#endif
        {
            rng.GetBytes(salt);
        }

#pragma warning disable SYSLIB0041
        using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, 1000))
        {
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.GenerateIV();
        }
#pragma warning restore SYSLIB0041

        using (var ms = new MemoryStream())
        {
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                var data = Encoding.UTF8.GetBytes(input);
                cs.Write(data, 0, data.Length);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
    }

    /// <summary>
    /// 使用特定的 AES 算法解密数据
    /// </summary>
    /// <param name="input">Base64 编码的加密数据</param>
    /// <param name="key">密钥</param>
    /// <returns>返回解密文本</returns>
    /// <exception cref="ArgumentNullException">如果 Key 为 null 或空</exception>
    /// <exception cref="ArgumentException">如果 input 数据错误</exception>
    [Obsolete]
    public static string AesDecrypt(string input, string key)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));


        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var encryptedData = Convert.FromBase64String(input);

        var salt = new byte[32];
        Array.Copy(encryptedData, 0, salt, 0, salt.Length);

        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(encryptedData, salt.Length, iv, 0, iv.Length);
        aes.IV = iv;

        if (encryptedData.Length < salt.Length + iv.Length)
        {
            throw new ArgumentException("加密数据格式无效或已损坏");
        }

#pragma warning disable SYSLIB0041
        using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, 1000))
        {
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
        }
#pragma warning restore SYSLIB0041

        var cipherTextLength = encryptedData.Length - salt.Length - iv.Length;
        using (var ms = new MemoryStream(encryptedData, salt.Length + iv.Length, cipherTextLength))
        {
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }

    #endregion
}