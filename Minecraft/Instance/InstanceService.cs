using PCL.Core.App;

namespace PCL.Core.Minecraft.Instance;

[LifecycleService(LifecycleState.Loaded)]
public sealed class InstanceService : GeneralService {
    private static LifecycleContext? _context;
    public static LifecycleContext Context => _context!;

    /// <inheritdoc />
    public InstanceService() : base("instance", "实例管理服务") {
        _context = Lifecycle.GetContext(this);
    }

    private static InstanceManager? _instanceManager;
    public static InstanceManager InstanceManager => _instanceManager!;

    /// <inheritdoc />
    public override void Start() {
        if (_instanceManager != null) {
            return;
        }

        Context.Info("Start to initialize java manager.");

        _instanceManager = new InstanceManager();

        _instanceManager.McInstanceListLoadAsync() ;

        _instanceManager.ScanJavaAsync().ContinueWith((_) => {
            _SetCache(_instanceManager.GetCache());

            var logInfo = string.Join("\n\t", _instanceManager.JavaList);
            Context.Info($"Finished to scan java: {logInfo}");
        });
    }
}
