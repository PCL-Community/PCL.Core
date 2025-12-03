using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Net;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.IO;

public class NetFile
{
    public required string Path { get; set; }
    public int Size = -1;
    public HashAlgorithm Algorithm = HashAlgorithm.sha1;
    public string Hash = "";
    public required string[] Url { get; set; }
    
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
    // 这个方法存在的意义就是为了让 Downloader 支持换源重试
    /// <summary>
    /// 获取当前对象的 DownloadItem 列表。
    /// </summary>
    /// <returns></returns>
    public List<DownloadItem> GetDownloadItem()
    {
        var list = new List<DownloadItem>();
        foreach (var url in Url)
        {
            var item = new DownloadItem(url.ToUri(), Path);
            item.Finished += () => CheckFile();
            list.Add(item);
        }
        return list;
    }
}

public enum HashAlgorithm {
    md5,
    sha1,
    sha256,
    sha512
}