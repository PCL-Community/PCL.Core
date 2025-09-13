namespace PCL.Core.Utils.Encryption;

public interface IEncryptionProvider
{
    public byte[] Encrypt(byte[] data, string key);
    public byte[] Decrypt(byte[] data, string key);
}