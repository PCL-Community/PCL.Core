using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace PCL.Core.Helper.Configure;
public class IniConfigure : IConfigure
{
    private ConcurrentDictionary<string, string> _content;
    
    private readonly string _filePath;
    private readonly bool _base64Encode;
    private readonly object _fileOpLock = new object();
    public IniConfigure(string path, bool base64Encode = true)
    {
        _filePath = path ?? throw new ArgumentNullException(nameof(path));
        _base64Encode = base64Encode;
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
                using var reader = new StreamReader(fs);
                _content = new ConcurrentDictionary<string, string>();
                while (reader.ReadLine() is { } line)
                {
                    var splitPos = line.IndexOf(':');
                    if (splitPos == -1)
                    {
                        LogWrapper.Warn($"[Config] {_filePath} 行数据找不到冒号分隔符，原始数据：{line}");
                        continue;
                    }
                    var key = line.Substring(0, splitPos);
                    var value = line.Substring(splitPos + 1);
                    _content.TryAdd(key, value);
                }
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
        if (key.Contains(Environment.NewLine))
            throw new ArgumentException(nameof(key));
        var wValue = _base64Encode
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty))
            : value?.ToString() ?? string.Empty;
        _content.AddOrUpdate(
            key,
            _ => wValue,
            (_, _) => wValue);
        Flush();
    }
    
    public TValue? Get<TValue>(string key)
    {
        if (!_content.TryGetValue(key, out var value) || string.IsNullOrEmpty(value)) return default;
        var ret = _base64Encode 
            ? Encoding.UTF8.GetString(Convert.FromBase64String(value))
            : value;

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
            string fileTemp = $"{_filePath}.temp";
            string fileBak = $"{_filePath}.bak";
            using var fs = new FileStream(fileTemp, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            using var writer = new StreamWriter(fs);
            foreach (var item in _content)
            {
                writer.WriteLine($"{item.Key}:{item.Value}");
            }
            writer.Close();
            fs.Close();
            File.Replace(fileTemp, _filePath, fileBak);
        }
    }

    public void Reload()
    {
        _load();
    }
}
