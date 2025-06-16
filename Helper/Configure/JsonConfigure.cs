using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PCL.Core.Helper.Configure;

public class JsonConfigure : IConfigure
{
    private ConcurrentDictionary<string, string> _content;
    
    private readonly string _filePath;
    private readonly object _fileOpLock = new object();
    public JsonConfigure(string filePath)
    {
        _filePath  = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _load();
        _content ??= new();
    }

    private void _load()
    {
        lock (_fileOpLock)
        {
            var folder = _filePath.Substring(0, _filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            using var reader = new StreamReader(fs, Encoding.UTF8);
            var ctx = reader.ReadToEnd();
            if (string.IsNullOrEmpty(ctx))
            {
                _content = new();
            }
            try
            {
                var jObject = JsonSerializer.Deserialize<ConcurrentDictionary<string, String>>(ctx);
                _content = jObject ?? new();
            }
            catch
            {
                _content = new();
            }
        }
    }

    public void Set(string key, object value)
    {
        _content.AddOrUpdate(key, _ => value.ToString(),(_, _) => value.ToString());
        Flush();
    }

    public TValue? Get<TValue>(string key)
    {
        try
        {
            if (_content.TryGetValue(key, out string? ret))
            {
                return (TValue)Convert.ChangeType(ret, typeof(TValue));
            }
        }
        catch { }
        return default;
    }

    public bool Contains(string key)
    {
        return _content.ContainsKey(key);
    }

    public void Clear()
    {
        _content.Clear();
        Flush();
    }

    public void Remove(string key)
    {
        _content.TryRemove(key, out _);
        Flush();
    }

    public void Flush()
    {
        lock (_fileOpLock)
        {
            var res = JsonSerializer.Serialize(_content);
            using var fs = new FileStream($"{_filePath}.temp",FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write(res);
            writer.Close();
            fs.Close();
            File.Replace($"{_filePath}.temp", _filePath, $"{_filePath}.bak");
        }
    }

    public void Reload()
    {
        _load();
    }
}
