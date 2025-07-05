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

    private static LifecycleContext _context = null!;
    private static CommonSetupFileManager? _globalSetupFile;
    private static CommonSetupFileManager? _localSetupFile;
    private static InstanceSetupFileManager? _instanceSetupFile;
    private static SetupRegManager? _globalSetupReg;

    private SetupService()
    {
        _context = Lifecycle.GetContext(this);
    }

    public string Identifier => "setup";
    public string Name => "程序配置";
    public bool SupportAsyncStart => true;

    public static SetupModel Setup { get; private set; } = null!;

    public static CommonSetupFileManager GlobalSetupFile =>
        _globalSetupFile ?? throw new InvalidOperationException("服务未开始");

    public static CommonSetupFileManager LocalSetupFile =>
        _localSetupFile ?? throw new InvalidOperationException("服务未开始");

    public static InstanceSetupFileManager InstanceSetupFile =>
        _instanceSetupFile ?? throw new InvalidOperationException("服务未开始");

    public static SetupRegManager GlobalSetupReg =>
        _globalSetupReg ?? throw new InvalidOperationException("服务未开始");

    public void Start()
    {
        // 初始化配置文件托管器
        _context.Trace("初始化配置文件托管器");
        // 全局配置文件托管器
        var globalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            GlobalSetupFolder,
            "Config.json");
        try
        {
            _globalSetupFile = new CommonSetupFileManager(globalPath, SetupJsonSerializer.Instance);
        }
        catch (Exception ex)
        {
            _context.Fatal("全局配置文件托管器初始化失败", ex);
            BackupFileAndShutdown(globalPath);
        }
        // 局部配置文件托管器
        var localPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "PCL", "Setup.ini");
        try
        {
            _localSetupFile = new CommonSetupFileManager(localPath, SetupIniSerializer.Instance);
        }
        catch (Exception ex)
        {
            _context.Fatal("局部配置文件托管器初始化失败", ex);
            BackupFileAndShutdown(localPath);
        }
        // 实例配置文件托管器
        _instanceSetupFile = new InstanceSetupFileManager();
        // 配置注册表托管器
        _globalSetupReg = new SetupRegManager(@"Software\" + GlobalSetupFolder);
        // 初始化配置模型
        _context.Trace("初始化配置模型");
        Setup = new SetupModel();
    }

    public void Stop()
    {
        Setup = null!;
        GlobalSetupFile.Dispose();
        LocalSetupFile.Dispose();
        InstanceSetupFile.Dispose();
        GlobalSetupReg.Dispose();
    }

    private static void BackupFileAndShutdown(string filePath)
    {
        var bakPath = filePath + ".bak";
        if (File.Exists(bakPath))
            File.Replace(filePath, bakPath, filePath + ".tmp");
        else
            File.Move(filePath, bakPath);
        _context.Info($"配置文件无法解析，可能已经损坏！{Environment.NewLine}" +
                      $"请删除 {filePath}{Environment.NewLine}" +
                      $"并使用备份配置文件 {bakPath}", actionLevel: LifecycleActionLevel.MsgBoxExit);
        Lifecycle.ForceShutdown(1);
    }
}