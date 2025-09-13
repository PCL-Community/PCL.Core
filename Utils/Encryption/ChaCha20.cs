using System;
using System.Text;
using System.Security.Cryptography;

namespace PCL.Core.Utils.Encryption;

public class ChaCha20 : IEncryptionProvider
{
    public static ChaCha20 Instance { get; } = new();

    private const int NonceSize = 12;    // 96-bit nonce for ChaCha20Poly1305
    private const int TagSize = 16;      // 128-bit authentication tag
    private const int KeySize = 32;      // 256-bit key
    private const int SaltSize = 16;     // 128-bit salt for HKDF

    public byte[] Encrypt(byte[] data, string key)
    {
        // Generate random salt, nonce and the tag
        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);
        RandomNumberGenerator.Fill(tag);

        // Derive key using the salt
        using var chacha = new ChaCha20Poly1305(_DeriveKey(key, salt));

        // Prepare output arrays
        var ciphertext = new byte[data.Length];

        // Perform encryption
        chacha.Encrypt(nonce, data, ciphertext, tag);

        // Make the encryption data: salt + nonce + tag + ciphertext
        var result = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
        var resultSpan = result.AsSpan();

        salt.CopyTo(resultSpan[..SaltSize]);
        nonce.CopyTo(resultSpan.Slice(SaltSize, NonceSize));
        tag.CopyTo(resultSpan.Slice(SaltSize + NonceSize, TagSize));
        ciphertext.CopyTo(resultSpan.Slice(SaltSize + NonceSize + TagSize, ciphertext.Length));

        return result;
    }

    public byte[] Decrypt(byte[] data, string key)
    {
        // Verify minimum data length
        if (data.Length < SaltSize + NonceSize + TagSize)
            throw new ArgumentException("Invalid encrypted data length");

        var dataSpan = data.AsSpan();
        // Encryption data: salt + nonce + tag + ciphertext
        var salt = dataSpan[..SaltSize];
        var nonce = dataSpan.Slice(SaltSize, NonceSize);
        var tag = dataSpan.Slice(SaltSize + NonceSize, TagSize);
        var ciphertext = dataSpan[(SaltSize + NonceSize + TagSize)..];

        // Derive key using the extracted salt
        using var chacha = new ChaCha20Poly1305(_DeriveKey(key, salt.ToArray()));

        // Perform decryption
        var plaintext = new byte[ciphertext.Length];
        chacha.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private static readonly byte[] _Info = Encoding.UTF8.GetBytes("PCL.Core.Utils.Encryption.ChaCha20");
    private static byte[] _DeriveKey(string key, byte[] salt)
    {
        var ikm = Encoding.UTF8.GetBytes(key);
        return HKDF.DeriveKey(
            HashAlgorithmName.SHA512,
            ikm,
            KeySize,
            salt,
            _Info);
    }
}