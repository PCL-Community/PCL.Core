using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 增强版多线程下载器使用示例
/// 展示所有优化功能：性能监控、配置管理、错误处理、断点续传等
/// </summary>
public static class EnhancedDownloadExample
{
    private const string LogModule = "EnhancedDownloadExample";
    
    /// <summary>
    /// 基础增强下载示例
    /// </summary>
    public static async Task BasicEnhancedDownloadExample()
    {
        LogWrapper.Info(LogModule, "=== 基础增强下载示例 ===");
        
        var configManager = new DownloadConfigurationManager();
        var profile = DownloadConfigurationManager.GetPresetProfile("High-Speed");
        var monitor = new DownloadMonitor();
        var errorHandler = new DownloadErrorHandler();
        
        try
        {
            var downloadTask = new EnhancedMultiThreadDownloadTask(
                new Uri("https://releases.ubuntu.com/22.04/ubuntu-22.04.3-desktop-amd64.iso"),
                Path.Combine(Path.GetTempPath(), "ubuntu-enhanced.iso"),
                profile.DownloadConfig
            );
            
            // 注册监控
            monitor.RegisterDownload("ubuntu-download", downloadTask);
            
            // 订阅事件
            downloadTask.ProgressChanged += (sender, old, progress) =>
            {
                var stats = downloadTask.GetDetailedStatus();
                LogWrapper.Info(LogModule, 
                    $"进度: {progress:P2} | " +
                    $"速度: {stats.CurrentSpeed / 1024 / 1024:F2}MB/s | " +
                    $"峰值: {stats.PeakSpeed / 1024 / 1024:F2}MB/s | " +
                    $"线程: {stats.ActiveThreads} | " +
                    $"重试: {stats.RetryCount}");
            };
            
            downloadTask.StateChanged += (sender, oldState, newState) =>
            {
                LogWrapper.Info(LogModule, $"状态变化: {oldState} -> {newState}");
            };
            
            // 错误处理
            errorHandler.ErrorOccurred += error =>
            {
                LogWrapper.Warn(LogModule, $"发生错误: {error}");
                LogWrapper.Info(LogModule, $"建议解决方案: {string.Join(", ", error.SuggestedSolutions)}");
            };
            
            errorHandler.ErrorRecovered += (error, result) =>
            {
                LogWrapper.Info(LogModule, $"错误已恢复: {result.Strategy}, {result.Message}");
            };
            
            // 性能监控
            monitor.PerformanceWarning += diagnosis =>
            {
                LogWrapper.Warn(LogModule, $"性能警告: {diagnosis.HealthDescription}");
                LogWrapper.Info(LogModule, $"建议: {string.Join(", ", diagnosis.Recommendations)}");
            };
            
            // 执行下载
            var result = await downloadTask.RunAsync();
            
            if (result.IsSuccess)
            {
                LogWrapper.Info(LogModule, 
                    $"✓ 下载成功完成！" +
                    $"文件大小: {result.TotalSize / 1024 / 1024:F1}MB | " +
                    $"耗时: {result.Duration.TotalSeconds:F1}秒 | " +
                    $"平均速度: {result.AverageSpeed / 1024 / 1024:F2}MB/s");
                    
                // 生成性能报告
                var report = monitor.GetPerformanceReport();
                LogWrapper.Info(LogModule, $"性能报告:\n{report}");
            }
            else
            {
                LogWrapper.Error(LogModule, $"✗ 下载失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            var error = errorHandler.HandleException(ex, new Dictionary<string, object>
            {
                ["TaskName"] = "ubuntu-download",
                ["URL"] = "https://releases.ubuntu.com/22.04/ubuntu-22.04.3-desktop-amd64.iso"
            });
            
            var recovery = await errorHandler.TryRecoverAsync(error);
            LogWrapper.Info(LogModule, $"错误恢复结果: {recovery.Message}");
        }
        finally
        {
            monitor.Dispose();
        }
    }
    
    /// <summary>
    /// 配置管理示例
    /// </summary>
    public static async Task ConfigurationManagementExample()
    {
        LogWrapper.Info(LogModule, "=== 配置管理示例 ===");
        
        var configManager = new DownloadConfigurationManager();
        
        // 展示预设配置
        LogWrapper.Info(LogModule, "可用的预设配置:");
        var presets = new[] { "High-Speed", "Low-Bandwidth", "Stable", "Mobile", "Server" };
        foreach (var preset in presets)
        {
            var profile = DownloadConfigurationManager.GetPresetProfile(preset);
            LogWrapper.Info(LogModule, $"  {preset}: {profile.Description}");
        }
        
        // 创建自定义配置
        var customProfile = new DownloadConfigurationProfile
        {
            Name = "Custom-Gaming",
            Description = "游戏下载专用配置",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 8,
                ChunkSize = 4 * 1024 * 1024, // 4MB
                BufferSize = 128 * 1024, // 128KB
                MaxRetries = 5,
                EnableResumeSupport = true,
                SpeedLimit = 50 * 1024 * 1024 // 50MB/s限制
            },
            NetworkConfig = new NetworkConfiguration
            {
                UserAgent = "Gaming-Downloader/1.0",
                EnableHttp2 = true,
                CustomHeaders = new Dictionary<string, string>
                {
                    ["X-Download-Priority"] = "High"
                }
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = true,
                MaxThreads = 12,
                RetryStrategy = RetryStrategy.ExponentialBackoff,
                EnablePerformanceMonitoring = true
            }
        };
        
        // 保存自定义配置
        configManager.SaveProfile(customProfile);
        LogWrapper.Info(LogModule, $"已保存自定义配置: {customProfile.Name}");
        
        // 切换配置并测试下载
        configManager.SetActiveProfile("Custom-Gaming");
        
        var testDownloadTask = new EnhancedMultiThreadDownloadTask(
            new Uri("https://www.7-zip.org/a/7z2201-x64.exe"),
            Path.Combine(Path.GetTempPath(), "7zip-test.exe"),
            configManager.ActiveProfile.DownloadConfig
        );
        
        LogWrapper.Info(LogModule, $"使用配置 '{configManager.ActiveProfile.Name}' 进行测试下载");
        
        var testResult = await testDownloadTask.RunAsync();
        if (testResult.IsSuccess)
        {
            LogWrapper.Info(LogModule, $"✓ 测试下载成功: 平均速度 {testResult.AverageSpeed / 1024 / 1024:F2}MB/s");
        }
        
        // 清理测试文件
        if (File.Exists(testResult.FilePath))
        {
            File.Delete(testResult.FilePath);
        }
    }
    
    /// <summary>
    /// 断点续传示例
    /// </summary>
    public static async Task ResumeDownloadExample()
    {
        LogWrapper.Info(LogModule, "=== 断点续传示例 ===");
        
        var config = new DownloadConfiguration
        {
            ThreadCount = 4,
            ChunkSize = 2 * 1024 * 1024,
            EnableResumeSupport = true,
            VerboseLogging = true
        };
        
        var url = new Uri("https://releases.ubuntu.com/20.04/ubuntu-20.04.6-desktop-amd64.iso");
        var filePath = Path.Combine(Path.GetTempPath(), "ubuntu-resume-test.iso");
        
        try
        {
            // 第一次下载（模拟中断）
            LogWrapper.Info(LogModule, "开始第一次下载...");
            
            var firstDownload = new EnhancedMultiThreadDownloadTask(url, filePath, config);
            
            // 让它运行5秒后取消
            var downloadTask = firstDownload.RunAsync();
            await Task.Delay(5000);
            firstDownload.CancelDownload();
            
            try
            {
                await downloadTask;
            }
            catch (OperationCanceledException)
            {
                LogWrapper.Info(LogModule, "第一次下载已取消");
            }
            
            // 检查部分下载的文件
            var tempFile = filePath + ".download";
            if (File.Exists(tempFile))
            {
                var partialSize = new FileInfo(tempFile).Length;
                LogWrapper.Info(LogModule, $"部分下载文件大小: {partialSize / 1024 / 1024:F1}MB");
            }
            
            // 等待一秒后继续下载
            await Task.Delay(1000);
            
            // 第二次下载（断点续传）
            LogWrapper.Info(LogModule, "开始断点续传...");
            
            var resumeDownload = new EnhancedMultiThreadDownloadTask(url, filePath, config);
            
            resumeDownload.ProgressChanged += (sender, old, progress) =>
            {
                var stats = resumeDownload.GetDetailedStatus();
                LogWrapper.Info(LogModule, 
                    $"续传进度: {progress:P2} | " +
                    $"已下载: {stats.DownloadedBytes / 1024 / 1024:F1}MB | " +
                    $"速度: {stats.CurrentSpeed / 1024 / 1024:F2}MB/s");
            };
            
            var resumeResult = await resumeDownload.RunAsync();
            
            if (resumeResult.IsSuccess)
            {
                LogWrapper.Info(LogModule, 
                    $"✓ 断点续传成功！" +
                    $"总大小: {resumeResult.TotalSize / 1024 / 1024:F1}MB | " +
                    $"总耗时: {resumeResult.Duration.TotalSeconds:F1}秒");
            }
            else
            {
                LogWrapper.Error(LogModule, $"✗ 断点续传失败: {resumeResult.ErrorMessage}");
            }
        }
        finally
        {
            // 清理文件
            if (File.Exists(filePath))
                File.Delete(filePath);
            if (File.Exists(filePath + ".download"))
                File.Delete(filePath + ".download");
        }
    }
    
    /// <summary>
    /// 批量下载与监控示例
    /// </summary>
    public static async Task BatchDownloadWithMonitoringExample()
    {
        LogWrapper.Info(LogModule, "=== 批量下载与监控示例 ===");
        
        var monitor = new DownloadMonitor
        {
            EnableDetailedMonitoring = true,
            MonitoringInterval = 500 // 500ms高频监控
        };
        
        var config = DownloadConfigurationManager.GetPresetProfile("Stable").DownloadConfig;
        config.ThreadCount = 6;
        
        var downloads = new[]
        {
            (new Uri("https://www.7-zip.org/a/7z2201-x64.exe"), "7zip.exe"),
            (new Uri("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.5.4/npp.8.5.4.Installer.x64.exe"), "notepadpp.exe"),
            (new Uri("https://download.mozilla.org/?product=firefox-latest&os=win64&lang=en-US"), "firefox.exe")
        };
        
        var tasks = new List<Task>();
        var results = new Dictionary<string, MultiThreadDownloadResult>();
        
        try
        {
            foreach (var (url, filename) in downloads)
            {
                var filePath = Path.Combine(Path.GetTempPath(), filename);
                var downloadTask = new EnhancedMultiThreadDownloadTask(url, filePath, config);
                
                // 注册监控
                monitor.RegisterDownload(filename, downloadTask);
                
                // 启动下载任务
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await downloadTask.RunAsync();
                        lock (results)
                        {
                            results[filename] = result;
                        }
                        LogWrapper.Info(LogModule, $"完成下载: {filename}");
                    }
                    catch (Exception ex)
                    {
                        LogWrapper.Error(ex, LogModule, $"下载失败: {filename}");
                    }
                });
                
                tasks.Add(task);
            }
            
            // 实时监控循环
            var monitoringTask = Task.Run(async () =>
            {
                while (tasks.Any(t => !t.IsCompleted))
                {
                    var (totalBytes, downloaded, avgSpeed, activeTasks) = monitor.GetOverallStatistics();
                    
                    if (totalBytes > 0)
                    {
                        var overallProgress = (double)downloaded / totalBytes * 100;
                        LogWrapper.Info(LogModule, 
                            $"整体进度: {overallProgress:F1}% | " +
                            $"活动任务: {activeTasks} | " +
                            $"总速度: {avgSpeed / 1024 / 1024:F2}MB/s");
                    }
                    
                    await Task.Delay(2000); // 每2秒更新一次
                }
            });
            
            // 等待所有下载完成
            await Task.WhenAll(tasks);
            
            // 生成最终报告
            var finalReport = monitor.GetPerformanceReport();
            LogWrapper.Info(LogModule, $"批量下载完成！\n{finalReport}");
            
            // 显示结果统计
            var successful = results.Values.Count(r => r.IsSuccess);
            var totalSize = results.Values.Where(r => r.IsSuccess).Sum(r => r.TotalSize);
            var avgSpeed = results.Values.Where(r => r.IsSuccess).Average(r => r.AverageSpeed);
            
            LogWrapper.Info(LogModule, 
                $"批量下载统计: {successful}/{results.Count} 成功 | " +
                $"总大小: {totalSize / 1024 / 1024:F1}MB | " +
                $"平均速度: {avgSpeed / 1024 / 1024:F2}MB/s");
        }
        finally
        {
            monitor.Dispose();
            
            // 清理下载文件
            foreach (var (_, filename) in downloads)
            {
                var filePath = Path.Combine(Path.GetTempPath(), filename);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LogWrapper.Debug(LogModule, $"已清理文件: {filename}");
                }
            }
        }
    }
    
    /// <summary>
    /// 错误处理与自动恢复示例
    /// </summary>
    public static async Task ErrorHandlingExample()
    {
        LogWrapper.Info(LogModule, "=== 错误处理与自动恢复示例 ===");
        
        var errorHandler = new DownloadErrorHandler
        {
            EnableAutoRecovery = true
        };
        
        // 测试各种错误情况
        var testCases = new[]
        {
            ("https://httpstat.us/404", "404错误测试"),
            ("https://httpstat.us/500", "服务器错误测试"),
            ("https://httpstat.us/503", "服务不可用测试"),
            ("https://nonexistent-domain-123456.com/file.zip", "DNS解析失败测试")
        };
        
        foreach (var (url, description) in testCases)
        {
            LogWrapper.Info(LogModule, $"\n测试场景: {description}");
            
            try
            {
                var config = new DownloadConfiguration
                {
                    ThreadCount = 2,
                    MaxRetries = 3,
                    TimeoutMs = 10000,
                    VerboseLogging = false
                };
                
                var downloadTask = new EnhancedMultiThreadDownloadTask(
                    new Uri(url),
                    Path.Combine(Path.GetTempPath(), $"error-test-{Guid.NewGuid()}.tmp"),
                    config
                );
                
                await downloadTask.RunAsync();
                LogWrapper.Info(LogModule, "意外成功");
            }
            catch (Exception ex)
            {
                var error = errorHandler.HandleException(ex, new Dictionary<string, object>
                {
                    ["TestCase"] = description,
                    ["URL"] = url
                });
                
                LogWrapper.Info(LogModule, $"捕获错误: {error.Type} - {error.Message}");
                LogWrapper.Info(LogModule, $"错误代码: {error.ErrorCode}");
                LogWrapper.Info(LogModule, $"可重试: {error.IsRetryable}");
                LogWrapper.Info(LogModule, $"建议解决方案: {string.Join(", ", error.SuggestedSolutions)}");
                
                // 尝试自动恢复
                var recovery = await errorHandler.TryRecoverAsync(error);
                LogWrapper.Info(LogModule, $"恢复策略: {recovery.Strategy}");
                LogWrapper.Info(LogModule, $"恢复结果: {(recovery.IsRecovered ? "成功" : "失败")}");
                LogWrapper.Info(LogModule, $"恢复消息: {recovery.Message}");
            }
        }
        
        // 显示错误统计
        var errorStats = errorHandler.GetErrorStatistics();
        LogWrapper.Info(LogModule, "\n错误统计:");
        foreach (var kvp in errorStats.Where(x => x.Value > 0))
        {
            LogWrapper.Info(LogModule, $"  {kvp.Key}: {kvp.Value} 次");
        }
    }
    
    /// <summary>
    /// 性能压力测试示例
    /// </summary>
    public static async Task PerformanceStressTestExample()
    {
        LogWrapper.Info(LogModule, "=== 性能压力测试示例 ===");
        
        var monitor = new DownloadMonitor();
        var config = DownloadConfigurationManager.GetPresetProfile("High-Speed").DownloadConfig;
        
        // 同时启动多个大文件下载
        var testUrls = new[]
        {
            "https://speed.hetzner.de/100MB.bin",
            "https://speed.hetzner.de/1GB.bin",
            "https://releases.ubuntu.com/22.04/ubuntu-22.04.3-desktop-amd64.iso"
        };
        
        var downloadTasks = new List<Task<MultiThreadDownloadResult>>();
        
        try
        {
            foreach (var (url, index) in testUrls.Select((url, i) => (url, i)))
            {
                var filePath = Path.Combine(Path.GetTempPath(), $"stress-test-{index}.tmp");
                var downloadTask = new EnhancedMultiThreadDownloadTask(
                    new Uri(url),
                    filePath,
                    config
                );
                
                monitor.RegisterDownload($"stress-test-{index}", downloadTask);
                downloadTasks.Add(downloadTask.RunAsync());
            }
            
            // 监控系统性能
            var perfMonitorTask = Task.Run(async () =>
            {
                var startTime = DateTime.Now;
                while (downloadTasks.Any(t => !t.IsCompleted) && DateTime.Now - startTime < TimeSpan.FromMinutes(5))
                {
                    var metrics = monitor.GetCurrentMetrics();
                    var (_, downloaded, speed, active) = monitor.GetOverallStatistics();
                    
                    LogWrapper.Info(LogModule, 
                        $"性能监控 | " +
                        $"活动任务: {active} | " +
                        $"总速度: {speed / 1024 / 1024:F2}MB/s | " +
                        $"已下载: {downloaded / 1024 / 1024:F1}MB | " +
                        $"内存: {metrics.MemoryUsageBytes / 1024 / 1024:F1}MB");
                    
                    await Task.Delay(3000); // 每3秒监控一次
                }
            });
            
            // 等待所有下载完成（最多5分钟）
            var completedTasks = await Task.WhenAll(downloadTasks.Select(async task =>
            {
                try
                {
                    return await task.WaitAsync(TimeSpan.FromMinutes(5));
                }
                catch (TimeoutException)
                {
                    return new MultiThreadDownloadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "下载超时"
                    };
                }
            }));
            
            // 分析结果
            var successful = completedTasks.Count(r => r.IsSuccess);
            var totalTransferred = completedTasks.Where(r => r.IsSuccess).Sum(r => r.TotalSize);
            var avgSpeed = completedTasks.Where(r => r.IsSuccess && r.AverageSpeed > 0)
                .DefaultIfEmpty().Average(r => r?.AverageSpeed ?? 0);
            
            LogWrapper.Info(LogModule, 
                $"压力测试完成！" +
                $"成功: {successful}/{completedTasks.Length} | " +
                $"总传输: {totalTransferred / 1024 / 1024:F1}MB | " +
                $"平均速度: {avgSpeed / 1024 / 1024:F2}MB/s");
            
            // 生成详细性能报告
            var report = monitor.GetPerformanceReport();
            LogWrapper.Info(LogModule, $"详细性能报告:\n{report}");
        }
        finally
        {
            monitor.Dispose();
            
            // 清理测试文件
            for (int i = 0; i < testUrls.Length; i++)
            {
                var filePath = Path.Combine(Path.GetTempPath(), $"stress-test-{i}.tmp");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
    
    /// <summary>
    /// 运行所有增强示例
    /// </summary>
    public static async Task RunAllEnhancedExamples()
    {
        LogWrapper.Info(LogModule, "🚀 === PCL.Core 增强多线程下载器全功能演示 === 🚀\n");
        
        try
        {
            LogWrapper.Info(LogModule, "1️⃣ 基础增强下载示例");
            await BasicEnhancedDownloadExample();
            await Task.Delay(2000);
            
            LogWrapper.Info(LogModule, "\n2️⃣ 配置管理示例");
            await ConfigurationManagementExample();
            await Task.Delay(2000);
            
            LogWrapper.Info(LogModule, "\n3️⃣ 断点续传示例");
            await ResumeDownloadExample();
            await Task.Delay(2000);
            
            LogWrapper.Info(LogModule, "\n4️⃣ 批量下载与监控示例");
            await BatchDownloadWithMonitoringExample();
            await Task.Delay(2000);
            
            LogWrapper.Info(LogModule, "\n5️⃣ 错误处理与自动恢复示例");
            await ErrorHandlingExample();
            await Task.Delay(2000);
            
            LogWrapper.Info(LogModule, "\n6️⃣ 性能压力测试示例");
            await PerformanceStressTestExample();
            
            LogWrapper.Info(LogModule, "\n🎉 === 所有增强功能演示完成！=== 🎉");
            LogWrapper.Info(LogModule, 
                "✅ 性能优化：连接池复用、内存缓冲区优化、异步IO\n" +
                "✅ 断点续传：真正的分块续传支持\n" +
                "✅ 速度限制：灵活的带宽控制\n" +
                "✅ 智能监控：实时性能分析和诊断\n" +
                "✅ 配置管理：多种预设和自定义配置\n" +
                "✅ 错误处理：智能错误分析和自动恢复\n" +
                "✅ 完美集成：PCL.Core架构完全兼容");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, "运行增强示例时发生异常");
        }
    }
}
