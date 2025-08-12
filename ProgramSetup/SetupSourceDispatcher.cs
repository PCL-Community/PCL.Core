using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup.SourceManage;

namespace PCL.Core.ProgramSetup;

public static class SetupSourceDispatcher
{
    private static volatile Lazy<FileSetupSourceManager>? _lazyGlobalSourceManager = null;
    private static volatile Lazy<FileSetupSourceManager>? _lazyLocalSourceManager = null;
    private static volatile InstanceSetupSourceManager? _instanceSourceManager = null;

    public static ISetupSourceManager GlobalSourceManager =>
        _lazyGlobalSourceManager?.Value ?? throw new InvalidOperationException("全局配置源托管器尚未加载或已被销毁");

    public static ISetupSourceManager LocalSourceManager =>
        _lazyLocalSourceManager?.Value ?? throw new InvalidOperationException("局部配置源托管器尚未加载或已被销毁");

    public static ISetupSourceManager InstanceSourceManager =>
        _instanceSourceManager ?? throw new InvalidOperationException("实例配置源托管器尚未加载或已被销毁");

    public static void Load()
    {
        _lazyGlobalSourceManager = new Lazy<FileSetupSourceManager>(() =>
        {
            try
            {
                var result = new FileSetupSourceManager(PredefinedFileItems.GlobalSetup, SetupService.GlobalFileSerializer, true);
                SetupService.MigrateGlobalSetupRegister(result);
                return result;
            }
            catch (Exception ex)
            {
                LogWrapper.Fatal(ex, "Setup", "全局配置源托管器初始化失败");
                _BackupFile(PredefinedFileItems.GlobalSetup);
                throw new IOException("全局配置源托管器初始化失败", ex);
            }
        });
        _lazyLocalSourceManager = new Lazy<FileSetupSourceManager>(() =>
        {
            try
            {
                return new FileSetupSourceManager(PredefinedFileItems.LocalSetup, SetupService.LocalFileSerializer, true);
            }
            catch (Exception ex)
            {
                LogWrapper.Fatal(ex, "Setup", "局部配置源托管器初始化失败");
                _BackupFile(PredefinedFileItems.LocalSetup);
                throw new IOException("局部配置源托管器初始化失败");
            }
        });
        _instanceSourceManager = new InstanceSetupSourceManager(SetupService.InstanceFileSerializer);
    }

    public static void Unload()
    {
        Task[] tasks =
        [
            Task.Run(() =>
            {
                if (Interlocked.Exchange(ref _lazyGlobalSourceManager, null) is
                    { IsValueCreated: true } lazyGlobalSourceManager)
                    lazyGlobalSourceManager.Value.Dispose();
            }),
            Task.Run(() =>
            {
                if (Interlocked.Exchange(ref _lazyLocalSourceManager, null) is
                    { IsValueCreated: true } lazyLocalSourceManager)
                    lazyLocalSourceManager.Value.Dispose();
            }),
            Task.Run(() =>
            {
                if (Interlocked.Exchange(ref _instanceSourceManager, null) is { } instanceSourceManager)
                    instanceSourceManager.Dispose();
            })
        ];
        try
        {
            Task.WhenAll(tasks).Wait();
        }
        catch (AggregateException ex)
        {
            LogWrapper.Error(ex, "Setup", "销毁配置源托管器时发生异常");
        }
    }

    private static void _BackupFile(FileItem file)
    {
        try
        {
            var filePath = file.TargetPath;
            var bakPath = filePath + ".bak";
            if (File.Exists(bakPath))
                File.Replace(filePath, bakPath, filePath + ".tmp");
            else
                File.Move(filePath, bakPath);
            LogWrapper.Fatal("Setup",
                $"配置文件无法解析，可能已经损坏！{Environment.NewLine}" +
                $"文件已删除并备份至 {bakPath}{Environment.NewLine}" +
                $"可以修正其内容并重命名为 {filePath}");
        }
        catch (Exception ex)
        {
            LogWrapper.Fatal(ex, "Setup", "配置文件无法解析，且移动文件失败！" + file.TargetDirectory);
        }
    }
}