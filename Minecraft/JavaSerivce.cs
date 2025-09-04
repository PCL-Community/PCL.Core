using System.Collections.Generic;
using System.Text.Json;
using PCL.Core.App;

namespace PCL.Core.Minecraft;

[LifecycleService(LifecycleState.Loaded)]
public sealed class JavaSerivce : GeneralService
{
    private static LifecycleContext? _context;
    public static LifecycleContext Context => _context!;

    /// <inheritdoc />
    public JavaSerivce() : base("java", "Java服务")
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
        var cache = _GetCaches();
        if (cache.Count != 0)
        {
            _javaManager.SetCache(cache);
            return;
        }

        _javaManager.ScanJava().ContinueWith((_) =>
        {
            _SetCache(_javaManager.GetCache());

            var logInfo = string.Join("\n\t", _javaManager.JavaList);
            Context.Info($"Finished to scan java: {logInfo}");
        });
    }

    private static List<JavaLocalCache> _GetCaches()
    {
        var raw = Config.Launch.Javas;
        if (string.IsNullOrEmpty(raw))
        {
            return [];
        }

        var cache = JsonSerializer.Deserialize<List<JavaLocalCache>>(raw);
        return cache ?? [];
    }

    private static void _SetCache(List<JavaLocalCache> caches)
    {
        var jsonContent = JsonSerializer.Serialize(caches);
        Config.Launch.Javas = jsonContent;
    }
}