using System;
using System.Security;
using System.Windows.Documents;

namespace PCL.Core.Utils.Encryption;

public interface IEncryptionProvider
{
    public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);
    public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);
}