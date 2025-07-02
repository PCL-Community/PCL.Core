using System;
using System.IO;
using PCL.Core.LifecycleManagement;
using PCL.Core.ProgramSetup;
using PCL.Core.ProgramSetup.FileManager;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading, Priority = -10000)]
public sealed class SetupService : ILifecycleService
{
#if DEBUG // 常量迁移自 ModSecret
    public const string GlobalSetupFolder = "PCLCEDebug"; // 社区开发版的注册表与社区常规版的注册表隔离，以防数据冲突
#else
    public const string GlobalSetupFolder = "PCLCE"; // PCL 社区版的注册表与 PCL 的注册表隔离，以防数据冲突
#endif

    private static LifecycleContext _Context = null!;

    private SetupService()
    {
        _Context = Lifecycle.GetContext(this);
    }

    public string Identifier => "setup";
    public string Name => "程序配置";
    public bool SupportAsyncStart => true;

    public static SetupModel Setup { get; private set; } = null!;
    public static CommonSetupFileManager GlobalSetupFile { get; private set; } = null!;
    public static CommonSetupFileManager LocalSetupFile { get; private set; } = null!;
    public static InstanceSetupFileManager InstanceSetupFile { get; private set; } = null!;

    public void Start()
    {
        // 初始化配置文件托管器
        _Context.Trace("初始化配置文件托管器");
        // 全局配置文件托管器
        var globalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            GlobalSetupFolder,
            "Config.json");
        try
        {
            GlobalSetupFile = new CommonSetupFileManager(globalPath, SetupJsonSerializer.Instance);
        }
        catch (Exception ex)
        {
            _Context.Fatal("全局配置文件托管器初始化失败", ex);
            File.Move(globalPath, globalPath + ".bak");
            _Context.Info($"配置文件无法解析，可能已经损坏！{Environment.NewLine}" +
                          $"请删除 {globalPath}{Environment.NewLine}" +
                          $"并使用备份配置文件 {globalPath}.bak", actionLevel: LifecycleActionLevel.MsgBoxExit);
            Lifecycle.ForceShutdown(1);
        }
        // 局部配置文件托管器
        var localPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "PCL", "Setup.ini");
        try
        {
            LocalSetupFile = new CommonSetupFileManager(localPath, SetupIniSerializer.Instance);
        }
        catch (Exception ex)
        {
            _Context.Fatal("局部配置文件托管器初始化失败", ex);
            File.Move(localPath, localPath + ".bak");
            _Context.Info($"配置文件无法解析，可能已经损坏！{Environment.NewLine}" +
                          $"请删除 {localPath}{Environment.NewLine}" +
                          $"并使用备份配置文件 {localPath}.bak", actionLevel: LifecycleActionLevel.MsgBoxExit);
            Lifecycle.ForceShutdown(1);
        }
        // 实例配置文件托管器
        InstanceSetupFile = new InstanceSetupFileManager();
        // 初始化配置模型
        _Context.Trace("初始化配置模型");
        Setup = new SetupModel();
    }

    public void Stop()
    {
        Setup = null!;
        GlobalSetupFile.Dispose();
        LocalSetupFile.Dispose();
        InstanceSetupFile.Dispose();
    }
}