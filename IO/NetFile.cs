using System;
using System.IO;
using PCL.Core.Utils.Hash;

namespace PCL.Core.IO;

public class NetFile
{
    public required string Path { get; set; }
    public int Size = -1;
    public HashAlgorithm Algorithm = HashAlgorithm.sha1;
    public string Hash = "";

    public bool CheckFile()
    {
        if (!File.Exists(Path)) return false;
        if (!string.IsNullOrEmpty(Hash))
        {
            using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, 16384, true);
            var hash = Algorithm switch
            {
                HashAlgorithm.md5 => MD5Provider.Instance.ComputeHash(fs),
                HashAlgorithm.sha1 => SHA1Provider.Instance.ComputeHash(fs),
                HashAlgorithm.sha256 => SHA256Provider.Instance.ComputeHash(fs),
                HashAlgorithm.sha512 => SHA512Provider.Instance.ComputeHash(fs),
                _ => throw new NotSupportedException($"Unsupport algorithm: {Algorithm}")
            };
            return hash == Hash;
        }
        return true;
    }
}

public enum HashAlgorithm {
    md5,
    sha1,
    sha256,
    sha512
}