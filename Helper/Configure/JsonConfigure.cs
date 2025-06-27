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
        _content ??= new ConcurrentDictionary<string, string>();
    }

    private void _load()
    {
        lock (_fileOpLock)
        {
            try
            {
                var folder = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(folder))
                    Directory.CreateDirectory(folder);
                using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                var ctx = reader.ReadToEnd();
                if (string.IsNullOrEmpty(ctx))
                    _content = new ConcurrentDictionary<string, string>();
                var jObject = JsonSerializer.Deserialize<ConcurrentDictionary<string, String>>(ctx);
                _content = jObject ?? new ConcurrentDictionary<string, string>();
            }
            catch (JsonException)
            {
                _content = new ConcurrentDictionary<string, string>();
            }
            catch (Exception e)
            {
                LogWrapper.Warn(e, $"[Config] 初始化 {_filePath} 文件出现问题");
                throw;
            }
        }
    }

    public void Set(string key, object value)
    {
        _content.AddOrUpdate(key, _ => value?.ToString() ?? string.Empty,(_, _) => value?.ToString() ?? string.Empty);
        Flush();
    }

    public TValue? Get<TValue>(string key)
    {
        if (!_content.TryGetValue(key, out var ret) || string.IsNullOrEmpty(ret)) return default;
        if (typeof(TValue) == typeof(string))
            return (TValue)(object)ret;
        try
        {
            return (TValue)Convert.ChangeType(ret, typeof(TValue));
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
        {
            LogWrapper.Warn(ex, $"[Config] {_filePath} 尝试将参数值 {ret} 从 string 转到 {typeof(TValue)} 失败");
            return default;
        }
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
            string tempFile = $"{_filePath}.temp";
            string bakFile = $"{_filePath}.bak";
            using var fs = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write(res);
            writer.Close();
            fs.Close();
            File.Replace(tempFile, _filePath, bakFile);
        }
    }

    public void Reload()
    {
        _load();
    }
}
