using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Instance;
using PCL.Core.Minecraft.Launch.State;
using PCL.Core.UI;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.Launch.Services;

/// <summary>
/// 启动前预检查服务
/// </summary>
public static class PreCheckService {
    /// <summary>
    /// 验证启动配置和环境
    /// </summary>
    public static object Validate(CancellationTokenSource source) {
        // 检查路径
        var pathResult = ValidatePaths(source);

        // 检查实例状态
        var instanceResult = ValidateInstance();

        // 检查档案有效性
        var profileResult = ValidateProfile();

        // 检查登录要求
        var loginResult = ValidateLoginRequirements();

        return new VoidResult();
    }

    private static bool ValidatePaths(CancellationTokenSource source) {
        if (McInstanceManager.Current == null) {
            HintWrapper.Show("未选择Minecraft实例", HintTheme.Error);
            return false;
        }

        // 检查路径中的特殊字符
        if (McInstanceManager.Current!.IsolatedPath!.Contains('!') || McInstanceManager.Current.IsolatedPath.Contains(';')) {
            HintWrapper.Show($"游戏路径中不可包含 ! 或 ;（{McInstanceManager.Current.IsolatedPath}）", HintTheme.Error);
            return false;
        }

        // UTF-8代码页下的路径检查
        if (EncodingUtils.IsDefaultEncodingUtf8() && !Config.Hint.NonAsciiGamePath && !McInstanceManager.Current.Path.IsASCII()) {
            var userChoice = MsgBoxWrapper.Show(
                $"欲启动实例 \"{McInstanceManager.Current.Name}\" 的路径中存在可能影响游戏正常运行的字符（非 ASCII 字符），是否仍旧启动游戏？\n\n如果不清楚具体作用，你可以先选择 \"继续\"，发现游戏在启动后很快出现崩溃的情况后再尝试修改游戏路径等操作",
                "游戏路径检查",
                buttons: [
                    "继续",
                    "返回处理",
                    "不再提示"
                ]);
            switch (userChoice) {
                case 1:
                    // 继续
                    break;
                case 2:
                    source.Cancel();
                    break;
                case 3:
                    // 不再提示
                    Config.Hint.NonAsciiGamePath = true;
                    break;
            }
        }

        return true;
    }

    private static bool ValidateInstance(McLaunchOptions options)
    {
        var instance = options.Version ?? McInstanceCurrent;
        if (instance == null)
            return Result.Failed("未选择Minecraft实例");

        try
        {
            instance.Load();
            if (instance.State == McInstanceState.Error)
                return Result.Failed($"Minecraft存在问题：{instance.Info}");
        }
        catch (Exception ex)
        {
            return Result.Failed($"加载实例失败：{ex.Message}", ex);
        }

        return Result.Success();
    }

    private static Result ValidateProfile()
    {
        if (SelectedProfile == null)
            return Result.Failed("请先选择一个档案再启动游戏！");

        // 这里应该调用原来的IsProfileVaild()方法
        // 简化实现
        var checkResult = IsProfileVaild();
        if (!string.IsNullOrEmpty(checkResult))
            return Result.Failed(checkResult);

        return Result.Success();
    }

    private static Result ValidateLoginRequirements(LaunchOptions options)
    {
        var instance = options.Version ?? McInstanceCurrent;
        
        // 检查是否要求正版验证
        if (instance.Version.HasLabyMod || Setup.Get("VersionServerLoginRequire", instance) == 1)
        {
            if (SelectedProfile.Type != McLoginType.Ms)
                return Result.Failed("当前实例要求使用正版验证，请使用正版验证档案启动游戏！");
        }

        // 检查是否要求第三方验证
        if (Setup.Get("VersionServerLoginRequire", instance) == 2)
        {
            if (SelectedProfile.Type != McLoginType.Auth)
                return Result.Failed("当前实例要求使用第三方验证，请使用第三方验证档案启动游戏！");

            var requiredServer = Setup.Get("VersionServerAuthServer", instance);
            if (SelectedProfile.Server.BeforeLast("/authserver") != requiredServer)
                return Result.Failed("当前档案使用的第三方验证服务器与实例要求使用的不一致！");
        }

        // 检查是否要求正版或第三方验证
        if (Setup.Get("VersionServerLoginRequire", instance) == 3)
        {
            if (SelectedProfile.Type == McLoginType.Legacy)
                return Result.Failed("当前实例要求使用正版验证或第三方验证，请使用符合要求的档案启动游戏！");

            if (SelectedProfile.Type == McLoginType.Auth)
            {
                var requiredServer = Setup.Get("VersionServerAuthServer", instance);
                if (SelectedProfile.Server.BeforeLast("/authserver") != requiredServer)
                    return Result.Failed("当前档案使用的第三方验证服务器与实例要求使用的不一致！");
            }
        }

        return Result.Success();
    }
}
