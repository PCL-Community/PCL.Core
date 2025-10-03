using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft;

[LifecycleService(LifecycleState.Loaded)]
public sealed class JavaService : GeneralService
{
    private static LifecycleContext? _context;
    public static LifecycleContext Context => _context!;

    /// <inheritdoc />
    public JavaService() : base("java", "Java管理")
    {
        _context = Lifecycle.GetContext(this);
    }

    private static JavaManager? _javaManager;
    public static JavaManager JavaManager => _javaManager!;

    /// <inheritdoc />
    public override void Start()
    {
        if (_javaManager != null)
        {
            return;
        }

        Context.Info("Start to initialize java manager.");

        _javaManager = new JavaManager();
        LoadFromConfig();
        _javaManager.ScanJavaAsync().ContinueWith(_ =>
        {
            SaveToConfig();

            var logInfo = string.Join("\n\t", _javaManager.JavaList);
            Context.Info($"Finished to scan java: {logInfo}");
        }, TaskScheduler.Default);
    }

    public static void LoadFromConfig()
    {
        if (_javaManager is null) return;

        var raw = Config.Launch.Javas;
        if (raw.IsNullOrWhiteSpace()) return;

        var caches = JsonSerializer.Deserialize<List<JavaLocalCache>>(raw);
        if (caches is null)
        {
            Context.Warn("序列化 Java 配置信息失败");
            return;
        }

        foreach (var cache in caches)
        {
            try
            {
                var targetInRecord = _javaManager.InternalJavas.FirstOrDefault(x => x.JavaExePath == cache.Path);
                if (targetInRecord is not null)
                    targetInRecord.IsEnabled = cache.IsEnable;
            }
            catch(Exception e)
            {
                Context.Error("应用配置项信息失败", e);
                var temp = JavaInfo.Parse(cache.Path);
                if (temp == null)
                    continue;
                temp.IsEnabled = cache.IsEnable;
                _javaManager.InternalJavas.Add(temp);
            }
        }
    }

    public static void SaveToConfig()
    {
        var caches = _javaManager?.InternalJavas.Select(x => new JavaLocalCache
        {
            IsEnable = x.IsEnabled,
            Path = x.JavaExePath
        }).ToList();
        if (caches is null) return;
        var jsonContent = JsonSerializer.Serialize(caches);
        Config.Launch.Javas = jsonContent;
    }

    private class JavaLocalCache
    {
        public required string Path { get; init; }
        public bool IsEnable { get; init; }
    }

}