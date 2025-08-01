using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.Helper;

public static class EncryptHelper
{
    public static string SecretEncrypt(string data) => AESEncrypt(data, Identify.EncryptKey);

    public static string SecretDecrypt(string data) => AESDecrypt(data, Identify.EncryptKey);

    public static string SecretDecryptOld(string data)
    {
        const string key = "00000000";
        const string iv = "87160295";
        var btKey = Encoding.UTF8.GetBytes(key);
        var btIV = Encoding.UTF8.GetBytes(iv);
        using var des = new DESCryptoServiceProvider();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateDecryptor(btKey, btIV), CryptoStreamMode.Write);
        var inData = Convert.FromBase64String(data);
        cs.Write(inData, 0, inData.Length);
        cs.FlushFinalBlock();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// 使用特定的 AES 算法加密数据
    /// </summary>
    /// <param name="input">需要加密的数据</param>
    /// <param name="key">密钥</param>
    /// <returns>Base64 编码的加密数据</returns>
    /// <exception cref="ArgumentNullException">如果 key 为 null 或者空</exception>
    public static string AESEncrypt(string input, string key)
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

        byte[] salt = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, 1000))
        {
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.GenerateIV();
        }

        using (var ms = new MemoryStream())
        {
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] data = Encoding.UTF8.GetBytes(input);
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
    public static string AESDecrypt(string input, string key)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));


        using Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        byte[] encryptedData = Convert.FromBase64String(input);

        byte[] salt = new byte[32];
        Array.Copy(encryptedData, 0, salt, 0, salt.Length);

        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(encryptedData, salt.Length, iv, 0, iv.Length);
        aes.IV = iv;

        if (encryptedData.Length < salt.Length + iv.Length)
        {
            throw new ArgumentException("加密数据格式无效或已损坏");
        }

        using (var deriveBytes = new Rfc2898DeriveBytes(key, salt, 1000))
        {
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
        }

        int cipherTextLength = encryptedData.Length - salt.Length - iv.Length;
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
}