using System;
using System.Threading.Tasks;
using PCL.Core.App.Configuration.NTraffic;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.Utils.Threading;

namespace PCL.Core.App.Configuration.Impl;

public sealed class FileTrafficCenter(IKeyValueFileProvider provider) : AsyncTrafficCenter(1)
{
    public IKeyValueFileProvider Provider { get; } = provider;

    private readonly AsyncDebounce _saveDebounce = new()
    {
        Delay = TimeSpan.FromSeconds(10),
        ScheduledTask = () =>
        {
            try { provider.Sync(); }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Config", "配置文件保存失败");
                const string hint = "保存配置文件时出现问题，请尽快重启启动器，" +
                    "否则可能出现状态不一致的情况，严重时会导致启动器崩溃。";
                MsgBoxWrapper.Show(hint, "配置文件保存失败", MsgBoxTheme.Error);
            }
            return Task.CompletedTask;
        }
    };

    protected override bool CanAsync<TInput, TOutput>(TrafficEventArgs<TInput, TOutput> e)
        => e.Access == TrafficAccess.Write;

    protected override void OnTrafficSync<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e)
    {
        if (e.Access != TrafficAccess.Read) return;
        if (!e.HasInput || e.Input is not string input) return;
        // 获取值 / 检查存在性
        if (e.HasOutput) e.SetOutput(Provider.Exists(input));
        else e.SetOutput(Provider.Get<TOutput>(input));
    }

    protected override async Task OnTrafficAsync<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e)
    {
        if (e.Access != TrafficAccess.Write) return;
        if (!e.HasInput || e.Input is not string input) return;
        // 设置值 / 删除值
        if (e.HasOutput) Provider.Set(input, e.Output);
        else Provider.Remove(input);
        // 延迟保存
        await _saveDebounce.Reset();
    }
}
