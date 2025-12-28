using PCL.Core.App.Updates.Models;
using PCL.Core.Logging;
using PCL.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Updates.Sources;

/// <summary>
/// 管理多个更新源，尝试找到可用源并缓存用于后续调用。
/// </summary>
public sealed class SourceController
{
    private readonly IReadOnlyList<IUpdateSource> _availableSources;
    private IUpdateSource? _currentSource;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// 初始化并过滤出可用的更新源。
    /// </summary>
    public SourceController(IEnumerable<IUpdateSource> sources)
    {
        _availableSources = sources
            .Where(s => s.IsAvailable)
            .ToList();
    }

    /// <summary>
    /// 尝试使用当前源处理操作，若失败则遍历其他可用源直至成功或无可用源。
    /// </summary>
    /// <param name="action">指定操作</param>
    /// <typeparam name="T">返回类型</typeparam>
    /// <returns>操作返回值</returns>
    /// <exception cref="InvalidOperationException">所有更新源均不可用时抛出</exception>
    private async Task<T> _TryFindSourceAsync<T>(Func<IUpdateSource, Task<T>> action)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_currentSource != null)
            {
                try
                {
                    var res = await action(_currentSource).ConfigureAwait(false);
                    _LogInfo("使用默认源处理成功");
                    return res;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }
                    _LogWarning($"默认源失效，遍历其他更新源。异常: {ex}");
                }
            }
            else
            {
                _LogInfo("无默认源，遍历其他更新源");
            }

            foreach (var source in _availableSources)
            {
                try
                {
                    var res = await action(source).ConfigureAwait(false);
                    _currentSource = source;
                    _LogInfo($"使用 {source.SourceName} 处理成功");
                    return res;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }
                    _LogWarning($"{source.SourceName}失效，使用下一个更新源。异常: {ex}");
                }
            }

            throw new InvalidOperationException("警告，所有更新源均无法使用！");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 尝试使用当前源处理操作，若失败则遍历其他可用源直至成功或无可用源。
    /// </summary>
    /// <param name="action">指定操作</param>
    /// <exception cref="InvalidOperationException">所有更新源均不可用时抛出</exception>
    private Task<object?> _TryFindSourceAsync(Func<IUpdateSource, Task> action) =>
        _TryFindSourceAsync<object?>(async s =>
        {
            await action(s).ConfigureAwait(false);
            return null;
        });

    /// <summary>
    /// 检查是否有新版本并返回结果。
    /// </summary>
    public async Task<CheckResult> CheckUpdateAsync()
    {
        var data = await _TryFindSourceAsync(s => s.CheckUpdateAsync()).ConfigureAwait(false);
        var isAvailable = data.VersionCode > Basics.VersionNumber
                          || SemVer.Parse(data.VersionName) > SemVer.Parse(Basics.VersionName);
        return isAvailable
            ? new CheckResult(CheckResultType.Available, data)
            : new CheckResult(CheckResultType.Latest);
    }

    /// <summary>
    /// 获取公告列表。
    /// </summary>
    public Task<VersionAnnouncementDataModel> GetAnnouncementListAsync() =>
        _TryFindSourceAsync(s => s.GetAnnouncementAsync());

    /// <summary>
    /// 使用可用源下载到指定路径。
    /// </summary>
    public Task DownloadAsync(string outputPath) =>
        _TryFindSourceAsync(s => s.DownloadAsync(outputPath));

    #region Logger Wrapper

    private void _LogInfo(string msg)
    {
        LogWrapper.Info("Update", msg);
    }

    private void _LogWarning(string msg, Exception? ex = null)
    {
        LogWrapper.Warn(ex, "Update", msg);
    }

    private void _LogError(string msg, Exception? ex = null)
    {
        LogWrapper.Error(ex, "Update", msg);
    }

    private void _LogTrace(string msg)
    {
        LogWrapper.Trace("Update", msg);
    }

    #endregion
}
