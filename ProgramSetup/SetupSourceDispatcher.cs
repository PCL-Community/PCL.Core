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
    private static volatile FileSetupSourceManager? _globalSourceManager = null;
    private static volatile FileSetupSourceManager? _localSourceManager = null;
    private static volatile InstanceSetupSourceManager? _instanceSourceManager = null;

    public static ISetupSourceManager GlobalSourceManager =>
        _globalSourceManager ?? throw new InvalidOperationException("全局配置源托管器尚未加载或已被销毁");

    public static ISetupSourceManager LocalSourceManager =>
        _localSourceManager ?? throw new InvalidOperationException("局部配置源托管器尚未加载或已被销毁");

    public static ISetupSourceManager InstanceSourceManager =>
        _instanceSourceManager ?? throw new InvalidOperationException("实例配置源托管器尚未加载或已被销毁");

    public static void Load()
    {
        try
        {
            var result = new FileSetupSourceManager(PredefinedFileItems.GlobalSetup, SetupService.GlobalFileSerializer, true);
            SetupService.MigrateGlobalSetupRegister(result, true);
            _globalSourceManager = result;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Setup", "全局配置源托管器初始化失败");
            _FatalBackup(PredefinedFileItems.GlobalSetup);
        }
        try
        {
            var result = new FileSetupSourceManager(PredefinedFileItems.LocalSetup, SetupService.LocalFileSerializer, true);
            _localSourceManager = result;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Setup", "局部配置源托管器初始化失败");
            _FatalBackup(PredefinedFileItems.LocalSetup);
        }
        _instanceSourceManager = new InstanceSetupSourceManager(SetupService.InstanceFileSerializer);
    }

    public static void Unload()
    {
        Task[] tasks =
        [
            Task.Run(() => Interlocked.Exchange(ref _globalSourceManager, null)?.Dispose()),
            Task.Run(() => Interlocked.Exchange(ref _localSourceManager, null)?.Dispose()),
            Task.Run(() => Interlocked.Exchange(ref _instanceSourceManager, null)?.Dispose())
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

    private static void _FatalBackup(FileItem file)
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
                $"发生了什么：配置文件 {filePath} 解析失败，这通常是人为修改文件内容或系统环境/硬盘故障导致的。\n" +
                $"应该如何做：文件已备份至 {bakPath}，如果你曾经修改过这个文件，请修正其内容并改回原文件名；" +
                $"如果你不知道如何修正，或是根本不知道发生了什么，无视这个提示即可，相关配置会自动重置到默认值。");
        }
        catch (Exception ex)
        {
            LogWrapper.Fatal(ex, "Setup", "配置文件无法解析，且备份文件失败！" + file.TargetDirectory);
        }
    }
}