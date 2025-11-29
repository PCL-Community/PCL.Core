using System;
using System.IO
using PCL.Core.App;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Accounts;



[LifecycleService(LifecycleState.Loaded)]
public sealed class AccountsService : GeneralService {
    
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    public new const bool SupportAsyncStart = false;

    private static readonly string _ProfilePath = Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "","profiles.v2.json");
    
    private AccountsService() : base("account", "账号管理")
    {
        _context = ServiceContext;
        
    }

    public override void Start() {
        Context.Info("开始加载档案信息");
        var profileV1 = _ProfilePath.Replace(".v2", "");
        if (File.Exists(profileV1))
        {
            Context.Info("检测到旧版本档案文件，开始进行迁移");
        }
    }
    
    

    public override void Stop() {
        Context.Info("正在保存档案信息");
    }

}