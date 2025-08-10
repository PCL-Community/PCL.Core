using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using PCL.Core.IO;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup.SourceManage;
using PCL.Core.Utils.Secret;

namespace PCL.Core.ProgramSetup;

[LifecycleService(LifecycleState.Loading, Priority = 1111)]
public sealed class SetupService : GeneralService
{
    #region 对外接口

    public static event SetupChangedHandler? SetupChanged;

    public delegate void SetupChangedHandler(SetupEntry entry, object? oldValue, object? newValue, string? gamePath);

    public static void RaiseSetupChanged(SetupEntry entry, object? oldValue, object? newValue, string? gamePath) =>
        SetupChanged?.Invoke(entry, oldValue, newValue, gamePath);

    #region Get

    /// <summary>
    /// 获取一个布尔值
    /// </summary>
    /// <param name="entry">要获取的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>获取到的值，如果键不存在则返回条目的默认值</returns>
    public static bool GetBool(SetupEntry entry, string? gamePath = null)
    {
        var rawValue = _GetSourceManager(entry).Get(entry.KeyName, gamePath);
        if (rawValue is null)
            return (bool)entry.DefaultValue;
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretDecrypt(rawValue);
        return bool.Parse(rawValue);
    }

    /// <summary>
    /// 获取一个 32 位整数值
    /// </summary>
    /// <param name="entry">要获取的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>获取到的值，如果键不存在则返回条目的默认值</returns>
    public static int GetInt32(SetupEntry entry, string? gamePath = null)
    {
        var rawValue = _GetSourceManager(entry).Get(entry.KeyName, gamePath);
        if (rawValue is null)
            return (int)entry.DefaultValue;
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretDecrypt(rawValue);
        return int.Parse(rawValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 获取一个字符串值
    /// </summary>
    /// <param name="entry">要获取的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>获取到的值，如果键不存在则返回条目的默认值</returns>
    public static string GetString(SetupEntry entry, string? gamePath = null)
    {
        var rawValue = _GetSourceManager(entry).Get(entry.KeyName, gamePath);
        if (rawValue is null)
            return (string)entry.DefaultValue;
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretDecrypt(rawValue);
        return rawValue;
    }

    #endregion

    #region Set

    /// <summary>
    /// 设置一个布尔值，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要设置的条目</param>
    /// <param name="value">要设置的值</param>
    /// <param name="gamePath">游戏目录路径</param>
    public static void SetBool(SetupEntry entry, bool value, string? gamePath = null)
    {
        var rawValue = value.ToString();
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretEncrypt(rawValue);
        var oldRawValue = _GetSourceManager(entry).Set(entry.KeyName, rawValue, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        bool? oldValue = oldRawValue is null ? null : bool.Parse(oldRawValue);
        SetupChanged?.Invoke(entry, oldValue, value, gamePath);
    }

    /// <summary>
    /// 设置一个 32 位整数值，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要设置的条目</param>
    /// <param name="value">要设置的值</param>
    /// <param name="gamePath">游戏目录路径</param>
    public static void SetInt32(SetupEntry entry, int value, string? gamePath = null)
    {
        var rawValue = value.ToString(CultureInfo.InvariantCulture);
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretEncrypt(rawValue);
        var oldRawValue = _GetSourceManager(entry).Set(entry.KeyName, rawValue, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        int? oldValue = oldRawValue is null ? null : int.Parse(oldRawValue, CultureInfo.InvariantCulture);
        SetupChanged?.Invoke(entry, oldValue, value, gamePath);
    }

    /// <summary>
    /// 设置一个字符串值，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要设置的条目</param>
    /// <param name="value">要设置的值</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为空</exception>
    public static void SetString(SetupEntry entry, string value, string? gamePath = null)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        var rawValue = value;
        if (entry.IsEncrypted)
            rawValue = EncryptHelper.SecretEncrypt(rawValue);
        string? oldRawValue = _GetSourceManager(entry).Set(entry.KeyName, rawValue, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        SetupChanged?.Invoke(entry, oldRawValue, value, gamePath);
    }

    #endregion

    #region Delete

    /// <summary>
    /// 删除一个布尔条目，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要删除的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    public static void DeleteBool(SetupEntry entry, string? gamePath = null)
    {
        var oldRawValue = _GetSourceManager(entry).Remove(entry.KeyName, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        bool? oldValue = oldRawValue is null ? null : bool.Parse(oldRawValue);
        SetupChanged?.Invoke(entry, oldValue, null, gamePath);
    }

    /// <summary>
    /// 删除一个 32 位整数条目，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要删除的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    public static void DeleteInt32(SetupEntry entry, string? gamePath = null)
    {
        var oldRawValue = _GetSourceManager(entry).Remove(entry.KeyName, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        int? oldValue = oldRawValue is null ? null : int.Parse(oldRawValue, CultureInfo.InvariantCulture);
        SetupChanged?.Invoke(entry, oldValue, null, gamePath);
    }

    /// <summary>
    /// 删除一个字符串条目，会触发 <see cref="SetupChanged"/> 事件
    /// </summary>
    /// <param name="entry">要删除的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    public static void DeleteString(SetupEntry entry, string? gamePath = null)
    {
        string? oldRawValue = _GetSourceManager(entry).Remove(entry.KeyName, gamePath);
        if (entry.IsEncrypted && oldRawValue is not null)
            oldRawValue = EncryptHelper.SecretDecrypt(oldRawValue);
        SetupChanged?.Invoke(entry, oldRawValue, null, gamePath);
    }

    #endregion

    /// <summary>
    /// 判断一个条目是否存在
    /// </summary>
    /// <param name="entry">要判断的条目</param>
    /// <param name="gamePath">游戏目录路径</param>
    /// <returns>条目是否存在</returns>
    public static bool IsUnset(SetupEntry entry, string? gamePath = null)
    {
        return _GetSourceManager(entry).Get(entry.KeyName, gamePath) is null;
    }

    #endregion

#if DEBUG
    public const string GlobalSetupFolder = "PCLCEDebug"; // 社区开发版的注册表与社区常规版的注册表隔离，以防数据冲突
#else
    public const string GlobalSetupFolder = "PCLCE"; // PCL 社区版的注册表与 PCL 的注册表隔离，以防数据冲突
#endif
    private static readonly IFileSerializer<ConcurrentDictionary<string, string>> _JsonSerializer = new JsonDictSerializer();
    private static readonly IFileSerializer<ConcurrentDictionary<string, string>> _IniSerializer = new IniDictSerializer();
    public static IFileSerializer<ConcurrentDictionary<string, string>> GlobalFileSerializer => _JsonSerializer;
    public static IFileSerializer<ConcurrentDictionary<string, string>> LocalFileSerializer => _IniSerializer;
    public static IFileSerializer<ConcurrentDictionary<string, string>> InstanceFileSerializer => _IniSerializer;
    private static LifecycleContext _context = null!;
    private static Lazy<FileSetupSourceManager> _lazyGlobalSetupSource = null!;
    private static Lazy<FileSetupSourceManager> _lazyLocalSetupSource = null!;
    private static RegisterSetupSourceManager _globalOldSetupSource = null!;
    private static InstanceSetupSourceManager _instanceSetupSource = null!;
    private static CombinedMigrationSetupSourceManager _migrationSetupSource = null!;
    private static CombinedMigrationSetupSourceManager _migrationSetupSourceEncrypted = null!;

    #region ILifecycleService

    public SetupService() : base("program-setup", "程序配置") { _context = ServiceContext; }

    public override void Start()
    {
        // 全局配置源托管器
        _lazyGlobalSetupSource = new Lazy<FileSetupSourceManager>(_CreateGlobalSetupSource);
        // 局部配置源托管器
        _lazyLocalSetupSource = new Lazy<FileSetupSourceManager>(_CreateLocalSetupSource);
        // 来自注册表的旧全局源托管器、游戏实例源托管器、用来支持配置迁移的托管器
        _globalOldSetupSource = new RegisterSetupSourceManager(@$"Software\{GlobalSetupFolder}");
        _instanceSetupSource = new InstanceSetupSourceManager(InstanceFileSerializer);
        _migrationSetupSource =
            new CombinedMigrationSetupSourceManager(_globalOldSetupSource, _lazyGlobalSetupSource.Value)
                { ProcessValueAsEncrypted = false };
        _migrationSetupSourceEncrypted =
            new CombinedMigrationSetupSourceManager(_globalOldSetupSource, _lazyGlobalSetupSource.Value)
                { ProcessValueAsEncrypted = true };
        SetupListener.Load();
    }

    public override void Stop()
    {
        // 销毁源托管器
        if (_lazyGlobalSetupSource.IsValueCreated)
            _lazyGlobalSetupSource.Value.Dispose();
        if (_lazyLocalSetupSource.IsValueCreated)
            _lazyLocalSetupSource.Value.Dispose();
        _instanceSetupSource.Dispose();
        // 删除 SetupChanged 事件处理器
        SetupListener.Unload();
        SetupChanged = null;
    }

    #endregion

    public static FileTransfer MigrateGlobalSetupFile => (file, resultCallback) =>
    {
        var source = file.Sources!.First();
        file.CreateDirectory();
        if (!File.Exists(source))
        {
            LogWrapper.Info("Setup", "无需处理全局配置文件迁移");
            resultCallback.Invoke(file.TargetPath);
            return;
        }
        try
        {
            if (File.Exists(file.TargetPath))
            {
                LogWrapper.Info("Setup", "全局配置文件在新旧路径均存在，将删除旧路径下的文件");
                try { File.Delete(source); }
                catch (Exception ex) { LogWrapper.Warn(ex, "Setup", "旧全局配置文件删除失败"); }
            }
            else
            {
                LogWrapper.Info("Setup", "将迁移全局配置文件");
                File.Move(source, file.TargetPath);
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Setup", "全局配置文件迁移处理失败");
            resultCallback.Invoke(null);
            return;
        }
        LogWrapper.Info("Setup", "全局配置文件迁移处理完成");
        resultCallback.Invoke(file.TargetPath);
    };

    private static FileSetupSourceManager _CreateGlobalSetupSource()
    {
        try
        {
            return new FileSetupSourceManager(PredefinedFileItems.GlobalSetup, GlobalFileSerializer, true);
        }
        catch (Exception ex)
        {
            LogWrapper.Fatal(ex, "Setup", "全局配置源托管器初始化失败");
            _BackupFile(PredefinedFileItems.GlobalSetup);
            throw new IOException("全局配置源托管器初始化失败");
        }
    }

    private FileSetupSourceManager _CreateLocalSetupSource()
    {
        try
        {
            return new FileSetupSourceManager(PredefinedFileItems.LocalSetup, LocalFileSerializer, true);
        }
        catch (Exception ex)
        {
            LogWrapper.Fatal(ex, "Setup", "局部配置源托管器初始化失败");
            _BackupFile(PredefinedFileItems.LocalSetup);
            throw new IOException("局部配置源托管器初始化失败");
        }
    }

    private static void _BackupFile(FileItem file)
    {
        var filePath = file.TargetPath;
        var bakPath = filePath + ".bak";
        if (File.Exists(bakPath))
            File.Replace(filePath, bakPath, filePath + ".tmp");
        else
            File.Move(filePath, bakPath);
        LogWrapper.Fatal(
            "Setup", 
            $"配置文件无法解析，可能已经损坏！{Environment.NewLine}" +
            $"请删除 {filePath}{Environment.NewLine}" +
            $"并使用备份配置文件 {bakPath}");
    }

    private static ISetupSourceManager _GetSourceManager(SetupEntry entry)
    {
        return entry.SourceType switch
        {
            SetupEntrySource.PathLocal => _lazyLocalSetupSource.Value,
            SetupEntrySource.SystemGlobal => entry.IsEncrypted ? _migrationSetupSourceEncrypted : _migrationSetupSource,
            SetupEntrySource.GameInstance => _instanceSetupSource,
            _ => throw new ArgumentOutOfRangeException($"{nameof(SetupEntry)} 具有不正确的 {nameof(SetupEntry.SourceType)}")
        };
    }
}

file class IniDictSerializer : IFileSerializer<ConcurrentDictionary<string, string>>
{
    public ConcurrentDictionary<string, string> Deserialize(Stream source)
    {
        var result = new ConcurrentDictionary<string, string>();
        using var reader = new StreamReader(source);
        while (reader.ReadLine() is { } line)
        {
            var splitPos = line.IndexOf(':');
            if (splitPos == -1)
                continue;
            var key = line[..splitPos];
            var value = line[(splitPos + 1)..];
            result.TryAdd(key, value);
        }
        return result;
    }

    public void Serialize(ConcurrentDictionary<string, string> input, Stream destination)
    {
        using var writer = new StreamWriter(destination);
        foreach (var pair in input)
            writer.WriteLine("{0}:{1}", pair.Key, pair.Value);
    }
}

file class JsonDictSerializer : IFileSerializer<ConcurrentDictionary<string, string>>
{
    private static readonly JsonSerializerOptions _SerializerOptions = new() { WriteIndented = true };

    public ConcurrentDictionary<string, string> Deserialize(Stream source)
    {
        if (source.Length == 0)
            return new ConcurrentDictionary<string, string>();
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(source) ?? new();
    }

    public void Serialize(ConcurrentDictionary<string, string> input, Stream destination) =>
        JsonSerializer.Serialize(destination, input, _SerializerOptions);
}