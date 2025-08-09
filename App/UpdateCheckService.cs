namespace PCL.Core.App;

[LifecycleService(LifecycleState.Running)]
public class UpdateCheckService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    private UpdateCheckService() : base("update-check", "更新检查", true) { _context = ServiceContext; }

    public override void Start()
    {

    }
}