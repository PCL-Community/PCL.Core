using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public static class MinecraftInstanceManager {
    public const int McInstanceCacheVersion = 30;
    private static McInstance? _mcInstanceCurrent;
    private static object? _mcInstanceLast;

    /// <summary>
    /// 当前的 Minecraft 实例
    /// </summary>
    public static McInstance? McInstanceCurrent
    {
        get => _mcInstanceCurrent;
        set
        {
            if (ReferenceEquals(_mcInstanceLast, value)) return;
            _mcInstanceCurrent = value;
            _mcInstanceLast = value;
            if (value == null) return;
        }
    }
}

