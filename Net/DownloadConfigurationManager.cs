using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PCL.Core.IO;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 下载器配置文件结构
/// </summary>
public class DownloadConfigurationProfile
{
    /// <summary>
    /// 配置文件名称
    /// </summary>
    public string Name { get; set; } = "Default";
    
    /// <summary>
    /// 配置描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// 线程数
    /// </summary>
    public int ThreadCount { get; set; } = 4;
    
    /// <summary>
    /// 分块大小 (字节)
    /// </summary>
    public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 超时时间 (毫秒)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 缓冲区大小 (字节)
    /// </summary>
    public int BufferSize { get; set; } = 8192;
    
    /// <summary>
    /// 速度限制 (字节/秒，0表示无限制)
    /// </summary>
    public long SpeedLimit { get; set; } = 0;
    
    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
    
    /// <summary>
    /// 连接池大小
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 8;
    
    /// <summary>
    /// 连接保持时间 (秒)
    /// </summary>
    public int KeepAliveTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;
    
    /// <summary>
    /// 用户代理
    /// </summary>
    public string UserAgent { get; set; } = "PCL.Core/1.0 (Enhanced Multi-Thread Downloader)";
    
    /// <summary>
    /// 自定义请求头
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    
    /// <summary>
    /// 是否验证SSL证书
    /// </summary>
    public bool ValidateSSLCertificate { get; set; } = true;
    
    /// <summary>
    /// 重试延迟策略
    /// </summary>
    public RetryDelayStrategy RetryDelayStrategy { get; set; } = RetryDelayStrategy.ExponentialBackoff;
    
    /// <summary>
    /// 基础重试延迟 (毫秒)
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// 最大重试延迟 (毫秒)
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 30000;
    
    /// <summary>
    /// 适用场景标签
    /// </summary>
    public List<string> ScenarioTags { get; set; } = new();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 创建配置的副本
    /// </summary>
    public DownloadConfigurationProfile Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<DownloadConfigurationProfile>(json)!;
    }
    
    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (ThreadCount <= 0 || ThreadCount > 64)
            throw new ArgumentOutOfRangeException(nameof(ThreadCount), "线程数必须在1-64之间");
            
        if (ChunkSize < 1024 || ChunkSize > 100 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(ChunkSize), "分块大小必须在1KB-100MB之间");
            
        if (TimeoutMs < 1000 || TimeoutMs > 300000)
            throw new ArgumentOutOfRangeException(nameof(TimeoutMs), "超时时间必须在1-300秒之间");
            
        if (BufferSize < 1024 || BufferSize > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(BufferSize), "缓冲区大小必须在1KB-1MB之间");
            
        if (SpeedLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(SpeedLimit), "速度限制不能为负数");
            
        if (MaxRetries < 0 || MaxRetries > 10)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), "重试次数必须在0-10之间");
    }
}

/// <summary>
/// 下载配置
/// </summary>
public class DownloadConfiguration
{
    /// <summary>
    /// 线程数
    /// </summary>
    public int ThreadCount { get; set; } = 4;
    
    /// <summary>
    /// 分块大小 (字节)
    /// </summary>
    public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 超时时间 (毫秒)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 缓冲区大小 (字节)
    /// </summary>
    public int BufferSize { get; set; } = 8192;
    
    /// <summary>
    /// 速度限制 (字节/秒，0表示无限制)
    /// </summary>
    public long SpeedLimit { get; set; } = 0;
    
    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
    
    /// <summary>
    /// 启用断点续传
    /// </summary>
    public bool EnableResumeSupport { get; set; } = true;
    
    /// <summary>
    /// 启用连接池复用
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;
    
    /// <summary>
    /// 文件预分配
    /// </summary>
    public bool PreAllocateFile { get; set; } = true;
    
    /// <summary>
    /// 从配置文件创建下载配置
    /// </summary>
    public static DownloadConfiguration FromProfile(DownloadConfigurationProfile profile)
    {
        return new DownloadConfiguration
        {
            ThreadCount = profile.ThreadCount,
            ChunkSize = profile.ChunkSize,
            MaxRetries = profile.MaxRetries,
            TimeoutMs = profile.TimeoutMs,
            BufferSize = profile.BufferSize,
            SpeedLimit = profile.SpeedLimit,
            VerboseLogging = profile.VerboseLogging,
            EnableResumeSupport = true, // 默认启用断点续传
            EnableConnectionPooling = true, // 默认启用连接池
            PreAllocateFile = true // 默认启用文件预分配
        };
    }
}

/// <summary>
/// 重试延迟策略
/// </summary>
public enum RetryDelayStrategy
{
    /// <summary>
    /// 固定延迟
    /// </summary>
    Fixed,
    
    /// <summary>
    /// 指数退避
    /// </summary>
    ExponentialBackoff,
    
    /// <summary>
    /// 线性增长
    /// </summary>
    Linear,
    
    /// <summary>
    /// 随机延迟
    /// </summary>
    Random
}

/// <summary>
/// 下载配置提供器
/// 提供预定义的下载配置，遵循底层服务原则，不维护配置文件
/// </summary>
public static class DownloadConfigurationProvider
{
    private const string LogModule = "DownloadConfigProvider";
    
    /// <summary>
    /// 当前活动配置
    /// </summary>
    public static DownloadConfigurationProfile ActiveProfile { get; private set; } = GetPresetProfile("Default");
    
    /// <summary>
    /// 所有可用的预设配置
    /// </summary>
    public static IReadOnlyCollection<DownloadConfigurationProfile> AvailableProfiles => GetAllPresetProfiles();
    
    /// <summary>
    /// 配置变更事件
    /// </summary>
    public static event Action<DownloadConfigurationProfile>? ConfigurationChanged;
    
    /// <summary>
    /// 切换活动配置
    /// </summary>
    /// <param name="profileName">配置名称</param>
    public static bool SetActiveProfile(string profileName)
    {
        try
        {
            var profile = GetPresetProfile(profileName);
            ActiveProfile = profile;
            ConfigurationChanged?.Invoke(profile);
            LogWrapper.Info(LogModule, $"已切换到配置: {profileName}");
            return true;
        }
        catch
        {
            LogWrapper.Warn(LogModule, $"配置不存在: {profileName}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取预定义配置
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <returns>配置实例</returns>
    public static DownloadConfigurationProfile GetPresetProfile(string name)
    {
        return name.ToLower() switch
        {
            "high-speed" => CreateHighSpeedProfile(),
            "low-bandwidth" => CreateLowBandwidthProfile(),
            "stable" => CreateStableProfile(),
            "mobile" => CreateMobileProfile(),
            "server" => CreateServerProfile(),
            _ => CreateDefaultProfile()
        };
    }
    
    /// <summary>
    /// 获取所有预设配置
    /// </summary>
    public static List<DownloadConfigurationProfile> GetAllPresetProfiles()
    {
        return new List<DownloadConfigurationProfile>
        {
            GetPresetProfile("Default"),
            GetPresetProfile("High-Speed"),
            GetPresetProfile("Low-Bandwidth"),
            GetPresetProfile("Stable"),
            GetPresetProfile("Mobile"),
            GetPresetProfile("Server")
        };
    }
    
    /// <summary>
    /// 根据网络条件自动选择最佳配置
    /// </summary>
    /// <param name="networkSpeed">网络速度 (字节/秒)</param>
    /// <param name="isMetered">是否为计量网络</param>
    /// <returns>推荐的配置</returns>
    public static DownloadConfigurationProfile GetRecommendedProfile(long networkSpeed = 0, bool isMetered = false)
    {
        try
        {
            if (isMetered)
            {
                LogWrapper.Info(LogModule, "检测到计量网络，使用低带宽配置");
                return GetPresetProfile("Low-Bandwidth");
            }
            
            if (networkSpeed > 0)
            {
                // 根据网速推荐配置
                if (networkSpeed > 50 * 1024 * 1024) // 50MB/s
                {
                    LogWrapper.Info(LogModule, "高速网络，使用高速配置");
                    return GetPresetProfile("High-Speed");
                }
                else if (networkSpeed < 1 * 1024 * 1024) // 1MB/s
                {
                    LogWrapper.Info(LogModule, "低速网络，使用移动网络配置");
                    return GetPresetProfile("Mobile");
                }
            }
            
            LogWrapper.Info(LogModule, "使用稳定配置");
            return GetPresetProfile("Stable");
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(LogModule, $"获取推荐配置失败: {ex.Message}，使用默认配置");
            return GetPresetProfile("Default");
        }
    }
    
    /// <summary>
    /// 根据文件大小推荐配置
    /// </summary>
    /// <param name="fileSize">文件大小</param>
    /// <returns>推荐的配置</returns>
    public static DownloadConfigurationProfile GetProfileForFileSize(long fileSize)
    {
        try
        {
            if (fileSize < 10 * 1024 * 1024) // 小于10MB
            {
                LogWrapper.Info(LogModule, "小文件，使用移动网络配置");
                return GetPresetProfile("Mobile");
            }
            else if (fileSize > 1024 * 1024 * 1024) // 大于1GB
            {
                LogWrapper.Info(LogModule, "大文件，使用高速配置");
                return GetPresetProfile("High-Speed");
            }
            else
            {
                LogWrapper.Info(LogModule, "中等文件，使用稳定配置");
                return GetPresetProfile("Stable");
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(LogModule, $"获取文件大小配置失败: {ex.Message}，使用默认配置");
            return GetPresetProfile("Default");
        }
    }
    
    /// <summary>
    /// 创建自定义配置（基于预设配置）
    /// </summary>
    /// <param name="baseName">基础配置名称</param>
    /// <param name="customName">自定义配置名称</param>
    /// <param name="customizations">自定义参数</param>
    public static DownloadConfigurationProfile CreateCustomProfile(string baseName, string customName, Action<DownloadConfigurationProfile> customizations)
    {
        try
        {
            var baseProfile = GetPresetProfile(baseName);
            var customProfile = baseProfile.Clone();
            
            // 应用自定义设置
            customProfile.Name = customName;
            customProfile.IsDefault = false;
            customProfile.LastModified = DateTime.Now;
            customizations(customProfile);
            
            // 验证配置
            customProfile.Validate();
            
            LogWrapper.Info(LogModule, $"创建自定义配置: {customName} (基于 {baseName})");
            return customProfile;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, $"创建自定义配置失败: {customName}");
            throw;
        }
    }
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    private static DownloadConfigurationProfile CreateDefaultProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Default",
            Description = "默认下载配置，适用于大多数场景",
            IsDefault = true,
            ThreadCount = 4,
            ChunkSize = 1024 * 1024, // 1MB
            MaxRetries = 3,
            TimeoutMs = 30000,
            BufferSize = 8192,
            SpeedLimit = 0,
            VerboseLogging = false,
            ConnectionPoolSize = 8,
            KeepAliveTimeoutSeconds = 30,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.ExponentialBackoff,
            BaseRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            ScenarioTags = new List<string> { "general", "default" }
        };
    }
    
    /// <summary>
    /// 创建高速下载配置
    /// </summary>
    private static DownloadConfigurationProfile CreateHighSpeedProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "High-Speed",
            Description = "高速下载配置，适用于高带宽网络环境",
            ThreadCount = 8,
            ChunkSize = 4 * 1024 * 1024, // 4MB
            MaxRetries = 5,
            TimeoutMs = 60000,
            BufferSize = 32768,
            SpeedLimit = 0,
            VerboseLogging = false,
            ConnectionPoolSize = 16,
            KeepAliveTimeoutSeconds = 60,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.Fixed,
            BaseRetryDelayMs = 500,
            MaxRetryDelayMs = 5000,
            ScenarioTags = new List<string> { "high-speed", "high-bandwidth", "server" }
        };
    }
    
    /// <summary>
    /// 创建低带宽配置
    /// </summary>
    private static DownloadConfigurationProfile CreateLowBandwidthProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Low-Bandwidth",
            Description = "低带宽配置，适用于网络条件较差的环境",
            ThreadCount = 2,
            ChunkSize = 256 * 1024, // 256KB
            MaxRetries = 5,
            TimeoutMs = 120000,
            BufferSize = 4096,
            SpeedLimit = 512 * 1024, // 512KB/s
            VerboseLogging = true,
            ConnectionPoolSize = 4,
            KeepAliveTimeoutSeconds = 15,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.ExponentialBackoff,
            BaseRetryDelayMs = 2000,
            MaxRetryDelayMs = 60000,
            ScenarioTags = new List<string> { "low-bandwidth", "slow-network", "conservative" }
        };
    }
    
    /// <summary>
    /// 创建稳定配置
    /// </summary>
    private static DownloadConfigurationProfile CreateStableProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Stable",
            Description = "稳定配置，平衡速度和稳定性",
            ThreadCount = 6,
            ChunkSize = 2 * 1024 * 1024, // 2MB
            MaxRetries = 4,
            TimeoutMs = 45000,
            BufferSize = 16384,
            SpeedLimit = 0,
            VerboseLogging = false,
            ConnectionPoolSize = 12,
            KeepAliveTimeoutSeconds = 45,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.ExponentialBackoff,
            BaseRetryDelayMs = 1500,
            MaxRetryDelayMs = 20000,
            ScenarioTags = new List<string> { "stable", "balanced", "reliable" }
        };
    }
    
    /// <summary>
    /// 创建移动网络配置
    /// </summary>
    private static DownloadConfigurationProfile CreateMobileProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Mobile",
            Description = "移动网络配置，适用于移动设备和不稳定网络",
            ThreadCount = 2,
            ChunkSize = 512 * 1024, // 512KB
            MaxRetries = 6,
            TimeoutMs = 90000,
            BufferSize = 4096,
            SpeedLimit = 1024 * 1024, // 1MB/s
            VerboseLogging = true,
            ConnectionPoolSize = 4,
            KeepAliveTimeoutSeconds = 20,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.ExponentialBackoff,
            BaseRetryDelayMs = 3000,
            MaxRetryDelayMs = 60000,
            ScenarioTags = new List<string> { "mobile", "unstable-network", "metered" }
        };
    }
    
    /// <summary>
    /// 创建服务器配置
    /// </summary>
    private static DownloadConfigurationProfile CreateServerProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Server",
            Description = "服务器配置，适用于服务器环境的批量下载",
            ThreadCount = 16,
            ChunkSize = 8 * 1024 * 1024, // 8MB
            MaxRetries = 3,
            TimeoutMs = 120000,
            BufferSize = 65536,
            SpeedLimit = 0,
            VerboseLogging = false,
            ConnectionPoolSize = 32,
            KeepAliveTimeoutSeconds = 300,
            EnableCompression = true,
            ValidateSSLCertificate = true,
            RetryDelayStrategy = RetryDelayStrategy.Fixed,
            BaseRetryDelayMs = 1000,
            MaxRetryDelayMs = 10000,
            ScenarioTags = new List<string> { "server", "batch", "high-performance" }
        };
    }
    
    /// <summary>
    /// 获取配置的性能评估
    /// </summary>
    /// <param name="profile">配置文件</param>
    /// <returns>性能评估信息</returns>
    public static string GetPerformanceAssessment(DownloadConfigurationProfile profile)
    {
        var assessment = new List<string>();
        
        // 评估并发能力
        if (profile.ThreadCount >= 8)
            assessment.Add("高并发");
        else if (profile.ThreadCount >= 4)
            assessment.Add("中等并发");
        else
            assessment.Add("低并发");
            
        // 评估内存使用
        var memoryUsage = profile.ThreadCount * profile.ChunkSize / 1024 / 1024;
        if (memoryUsage > 50)
            assessment.Add("高内存使用");
        else if (memoryUsage > 20)
            assessment.Add("中等内存使用");
        else
            assessment.Add("低内存使用");
            
        // 评估网络友好度
        if (profile.SpeedLimit > 0)
            assessment.Add("带宽限制");
        if (profile.MaxRetries >= 5)
            assessment.Add("高容错");
            
        return string.Join(", ", assessment);
    }
    
    /// <summary>
    /// 获取适用场景建议
    /// </summary>
    /// <param name="profile">配置文件</param>
    /// <returns>场景建议</returns>
    public static List<string> GetScenarioRecommendations(DownloadConfigurationProfile profile)
    {
        var recommendations = new List<string>();
        
        if (profile.ThreadCount >= 8 && profile.ChunkSize >= 4 * 1024 * 1024)
            recommendations.Add("大文件下载");
            
        if (profile.SpeedLimit > 0)
            recommendations.Add("带宽受限环境");
            
        if (profile.MaxRetries >= 5)
            recommendations.Add("不稳定网络");
            
        if (profile.ConnectionPoolSize >= 16)
            recommendations.Add("服务器批量下载");
            
        if (profile.ThreadCount <= 2 && profile.ChunkSize <= 512 * 1024)
            recommendations.Add("移动设备");
            
        return recommendations;
    }
}
