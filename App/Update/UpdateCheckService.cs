using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.ProgramSetup;

namespace PCL.Core.App.Update;

[LifecycleService(LifecycleState.Running)]
public class UpdateCheckService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    private UpdateCheckService() : base("update-check", "更新检查", true) { _context = ServiceContext; }

    public override void Start()
    {
        try
        {
            switch (Setup.System.UpdateSolution)
            {
                case 0: // 直接更新
                    break;
                case 1: // 提示更新
                    break;
                case 2:
                case 3: // 不想更新
                    break;
                default:
                    Setup.System.UpdateSolution = 0; // 乱改配置，改回去 😡
                    break;
            }
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "Update", "An unexpected error occured when checking for updates");
        }
    }
}