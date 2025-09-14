using System.Security;

namespace PCL.Core.Utils.Encryption;

public interface IEncryptionProvider
{
    public byte[] Encrypt(byte[] data, SecureString key);
    public byte[] Decrypt(byte[] data, SecureString key);
}