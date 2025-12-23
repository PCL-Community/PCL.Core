using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using PCL.Core.Utils.Encryption;

namespace PCL.Core.Utils.Secret;

public static class EncryptHelper
{
    public static string SecretEncrypt(string data)
    {
        var rawData = Encoding.UTF8.GetBytes(data);
        var encryptedData = ChaCha20.Instance.Encrypt(rawData, Identify.EncryptionKey);
        var storeData = new EncryptionData()
        {
            Version = 1,
            Data = encryptedData
        };
        return Convert.ToBase64String(EncryptionData.ToBytes(storeData));
    }

    public static string SecretDecrypt(string data)
    {
        var rawData = Convert.FromBase64String(data);
        if (rawData.Length == 0) return string.Empty;
        try
        {
            var encryptionData = EncryptionData.FromBytes(rawData);
            var decryptedData = encryptionData.Version switch
            {
                1 => ChaCha20.Instance.Decrypt(encryptionData.Data, Identify.EncryptionKey),
                _ => throw new NotSupportedException("Unsupported encryption version")
            };
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch { /* Ignore to try old method */ }

        try
        {
            var decryptedData = AesCbc.Instance.Decrypt(rawData, Encoding.UTF8.GetBytes(IdentifyOld.EncryptKey));
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch { /* Ignore to try old method */ }

        throw new Exception($"Unknown Encryption data, the data may broken");
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
}