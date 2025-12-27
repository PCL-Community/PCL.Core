using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.Encryption;

namespace PCL.Core.Utils.Secret;

public static class EncryptHelper
{
    public static string SecretEncrypt(string data)
    {
        var rawData = Encoding.UTF8.GetBytes(data);
        byte[] encryptedData;
        uint version;
        if (ChaCha20.Instance.IsSupported)
        {
            encryptedData = ChaCha20.Instance.Encrypt(rawData, EncryptionKey);
            version = 1;
        }
        else if (AesGcmProvider.Instance.IsSupported)
        {
            encryptedData = AesGcmProvider.Instance.Encrypt(rawData, EncryptionKey);
            version = 2;
        }
        else
        {
            LogWrapper.Warn("Encryption", "No available encryption method");
            encryptedData = rawData;
            version = 0;
        }

        return Convert.ToBase64String(EncryptionData.ToBytes(new EncryptionData { Version = version, Data = encryptedData })); ;
    }

    public static string SecretDecrypt(string data)
    {
        var rawData = Convert.FromBase64String(data);
        if (rawData.Length == 0) return string.Empty;
        var errors = new List<Exception>();
        try
        {
            var encryptionData = EncryptionData.FromBytes(rawData);
            var decryptedData = encryptionData.Version switch
            {
                0 => rawData,
                1 => ChaCha20.Instance.Decrypt(encryptionData.Data, EncryptionKey),
                2 => AesGcmProvider.Instance.Decrypt(encryptionData.Data, EncryptionKey),
                _ => throw new NotSupportedException("Unsupported encryption version")
            };
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch(Exception ex) { errors.Add(ex); }

        try
        {
            var decryptedData = AesCbc.Instance.Decrypt(rawData, Encoding.UTF8.GetBytes(IdentifyOld.EncryptKey));
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch(Exception ex) { errors.Add(ex); }

        throw new AggregateException($"Unknown Encryption data, the data may broken", errors);
    }

    #region "加密存储信息数据"


    public struct EncryptionData
    {
        public uint Version;
        public byte[] Data;

        private const uint MagicNumber = 0x454E4321;

        public static EncryptionData FromBase64(string base64)
        {
            return FromBytes(Convert.FromBase64String(base64));
        }

        public static EncryptionData FromBytes(ReadOnlySpan<byte> bytes)
        {
            // 0 - 4  MagicNumber |  4 - 8 version || 8 - 12 bytes rData length | n bytes rData
            if (bytes.Length < 12)
                throw new ArgumentException("No enough data for EncryptionData", nameof(bytes));

            if (BinaryPrimitives.ReadUInt32BigEndian(bytes[..4]) != MagicNumber)
                throw new ArgumentException("Unknown data for EncryptionData", nameof(bytes));

            var dataLength = BinaryPrimitives.ReadInt32BigEndian(bytes[8..12]);
            if (dataLength > bytes.Length - 12)
                throw new ArgumentException("No enough data for EncryptionData", nameof(bytes));
            if (dataLength < 0)
                throw new ArgumentException("Invalid data length for EncryptionData", nameof(bytes));

            var rData = bytes[12..(12 + dataLength)];

            return new EncryptionData
            {
                Version = BinaryPrimitives.ReadUInt32BigEndian(bytes[4..8]),
                Data = rData.ToArray()
            };
        }

        public static byte[] ToBytes(EncryptionData encryptionData)
        {
            var length = 12 + encryptionData.Data.Length;
            var bytes = new byte[length];
            var bytesSpan = bytes.AsSpan();
            BinaryPrimitives.WriteUInt32BigEndian(bytesSpan[..4], MagicNumber);
            BinaryPrimitives.WriteUInt32BigEndian(bytesSpan[4..8], encryptionData.Version);
            BinaryPrimitives.WriteInt32BigEndian(bytesSpan[8..12], encryptionData.Data.Length);
            encryptionData.Data.CopyTo(bytesSpan[12..]);

            return bytes;
        }
    }

    #endregion

    #region "密钥存储和获取"

    private static readonly byte[] _IdentifyEntropy = Encoding.UTF8.GetBytes("PCL CE Encryption Key");
    internal static byte[] EncryptionKey { get => _EncryptionKey.Value; }
    private static readonly Lazy<byte[]> _EncryptionKey = new(_GetKey);

    private static byte[] _GetKey()
    {
        var keyFile = Path.Combine(FileService.SharedDataPath, "UserKey.bin");
        if (File.Exists(keyFile))
        {
            var buf = File.ReadAllBytes(keyFile);
            var data = EncryptionData.FromBytes(buf);
            return data.Version switch
            {
                1 => ProtectedData.Unprotect(data.Data, _IdentifyEntropy, DataProtectionScope.CurrentUser),
                _ => throw new NotSupportedException("Unsupported encryption version")
            };
        }
        else
        {
            var randomKey = new byte[32];
            RandomNumberGenerator.Fill(randomKey);
            var storeData = EncryptionData.ToBytes(new EncryptionData
            {
                Version = 1,
                Data = ProtectedData.Protect(randomKey, _IdentifyEntropy, DataProtectionScope.CurrentUser)
            });

            var tmpFile = $"{keyFile}.tmp";
            using (var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Write(storeData);
                fs.Flush(true);
            }

            File.Move(tmpFile, keyFile, true);

            return randomKey;
        }
    }

    #endregion
}