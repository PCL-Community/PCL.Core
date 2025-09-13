using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 基础下载配置
    /// </summary>
    public DownloadConfiguration DownloadConfig { get; set; } = new();
    
    /// <summary>
    /// 网络配置
    /// </summary>
    public NetworkConfiguration NetworkConfig { get; set; } = new();
    
    /// <summary>
    /// 性能配置
    /// </summary>
    public PerformanceConfiguration PerformanceConfig { get; set; } = new();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// 网络配置
/// </summary>
public class NetworkConfiguration
{
    /// <summary>
    /// 连接超时时间(毫秒)
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;
    
    /// <summary>
    /// 读取超时时间(毫秒)
    /// </summary>
    public int ReadTimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 最大重定向次数
    /// </summary>
    public int MaxRedirects { get; set; } = 5;
    
    /// <summary>
    /// User-Agent
    /// </summary>
    public string UserAgent { get; set; } = "PCL.Core-MultiThreadDownloader/1.0";
    
    /// <summary>
    /// 自定义请求头
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    
    /// <summary>
    /// 代理配置
    /// </summary>
    public ProxyConfiguration? Proxy { get; set; }
    
    /// <summary>
    /// 启用HTTP/2
    /// </summary>
    public bool EnableHttp2 { get; set; } = true;
    
    /// <summary>
    /// 启用压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;
    
    /// <summary>
    /// 启用Keep-Alive
    /// </summary>
    public bool EnableKeepAlive { get; set; } = true;
}

/// <summary>
/// 代理配置
/// </summary>
public class ProxyConfiguration
{
    /// <summary>
    /// 代理类型
    /// </summary>
    public ProxyType Type { get; set; } = ProxyType.Http;
    
    /// <summary>
    /// 代理服务器地址
    /// </summary>
    public string Host { get; set; } = "";
    
    /// <summary>
    /// 代理端口
    /// </summary>
    public int Port { get; set; } = 0;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// 绕过本地地址
    /// </summary>
    public bool BypassLocal { get; set; } = true;
}

/// <summary>
/// 代理类型
/// </summary>
public enum ProxyType
{
    Http,
    Socks5,
    Socks4
}

/// <summary>
/// 性能配置
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// 自动调整线程数
    /// </summary>
    public bool AutoAdjustThreads { get; set; } = true;
    
    /// <summary>
    /// 最小线程数
    /// </summary>
    public int MinThreads { get; set; } = 1;
    
    /// <summary>
    /// 最大线程数
    /// </summary>
    public int MaxThreads { get; set; } = 16;
    
    /// <summary>
    /// 自动调整分块大小
    /// </summary>
    public bool AutoAdjustChunkSize { get; set; } = true;
    
    /// <summary>
    /// 最小分块大小
    /// </summary>
    public int MinChunkSize { get; set; } = 64 * 1024; // 64KB
    
    /// <summary>
    /// 最大分块大小
    /// </summary>
    public int MaxChunkSize { get; set; } = 16 * 1024 * 1024; // 16MB
    
    /// <summary>
    /// 自动重试策略
    /// </summary>
    public RetryStrategy RetryStrategy { get; set; } = RetryStrategy.ExponentialBackoff;
    
    /// <summary>
    /// 基础重试延迟(毫秒)
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// 最大重试延迟(毫秒)
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 30000;
    
    /// <summary>
    /// 启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
    
    /// <summary>
    /// 监控间隔(毫秒)
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = 1000;
}

/// <summary>
/// 重试策略
/// </summary>
public enum RetryStrategy
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
/// 下载配置管理器
/// 提供配置文件的加载、保存、管理功能
/// </summary>
public class DownloadConfigurationManager
{
    private const string LogModule = "DownloadConfigManager";
    private readonly string _configDirectory;
    private readonly Dictionary<string, DownloadConfigurationProfile> _profiles = new();
    private DownloadConfigurationProfile? _activeProfile;
    
    /// <summary>
    /// 配置文件扩展名
    /// </summary>
    public const string ConfigFileExtension = ".dlconfig";
    
    /// <summary>
    /// 默认配置文件名
    /// </summary>
    public const string DefaultConfigFileName = "default" + ConfigFileExtension;
    
    /// <summary>
    /// 当前活动配置
    /// </summary>
    public DownloadConfigurationProfile ActiveProfile => _activeProfile ?? GetDefaultProfile();
    
    /// <summary>
    /// 所有可用配置
    /// </summary>
    public IReadOnlyCollection<DownloadConfigurationProfile> AvailableProfiles => _profiles.Values.ToArray();
    
    /// <summary>
    /// 配置变更事件
    /// </summary>
    public event Action<DownloadConfigurationProfile>? ConfigurationChanged;
    
    public DownloadConfigurationManager(string? configDirectory = null)
    {
        _configDirectory = configDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "PCL.Core", "DownloadConfigs");
            
        EnsureConfigDirectory();
        LoadAllProfiles();
        
        LogWrapper.Info(LogModule, $"配置管理器初始化完成，配置目录: {_configDirectory}");
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
    /// 创建高速下载配置
    /// </summary>
    private static DownloadConfigurationProfile CreateHighSpeedProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "High-Speed",
            Description = "高速下载配置，适用于高速网络环境",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 16,
                ChunkSize = 8 * 1024 * 1024, // 8MB
                BufferSize = 128 * 1024, // 128KB
                MaxRetries = 5,
                TimeoutMs = 60000,
                EnableConnectionPooling = true,
                PreAllocateFile = true
            },
            NetworkConfig = new NetworkConfiguration
            {
                ConnectionTimeoutMs = 15000,
                ReadTimeoutMs = 60000,
                EnableHttp2 = true,
                EnableCompression = true,
                EnableKeepAlive = true
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = true,
                MaxThreads = 32,
                AutoAdjustChunkSize = true,
                MaxChunkSize = 32 * 1024 * 1024, // 32MB
                RetryStrategy = RetryStrategy.ExponentialBackoff,
                EnablePerformanceMonitoring = true
            }
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
            Description = "低带宽下载配置，适用于网络较慢的环境",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 2,
                ChunkSize = 256 * 1024, // 256KB
                BufferSize = 16 * 1024, // 16KB
                MaxRetries = 8,
                TimeoutMs = 120000, // 2分钟
                EnableConnectionPooling = true,
                SpeedLimit = 100 * 1024 // 100KB/s限制
            },
            NetworkConfig = new NetworkConfiguration
            {
                ConnectionTimeoutMs = 30000,
                ReadTimeoutMs = 120000,
                EnableCompression = true,
                EnableKeepAlive = false // 避免占用连接
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = false, // 固定线程数
                MaxThreads = 4,
                AutoAdjustChunkSize = false,
                RetryStrategy = RetryStrategy.Fixed,
                BaseRetryDelayMs = 3000,
                EnablePerformanceMonitoring = true,
                MonitoringIntervalMs = 5000 // 5秒监控
            }
        };
    }
    
    /// <summary>
    /// 创建稳定下载配置
    /// </summary>
    private static DownloadConfigurationProfile CreateStableProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Stable",
            Description = "稳定下载配置，平衡速度和稳定性",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 4,
                ChunkSize = 2 * 1024 * 1024, // 2MB
                BufferSize = 64 * 1024, // 64KB
                MaxRetries = 5,
                TimeoutMs = 45000,
                EnableConnectionPooling = true,
                EnableResumeSupport = true
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = true,
                MaxThreads = 8,
                AutoAdjustChunkSize = true,
                RetryStrategy = RetryStrategy.ExponentialBackoff,
                EnablePerformanceMonitoring = true
            }
        };
    }
    
    /// <summary>
    /// 创建移动设备配置
    /// </summary>
    private static DownloadConfigurationProfile CreateMobileProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Mobile",
            Description = "移动设备配置，节省电量和流量",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 2,
                ChunkSize = 512 * 1024, // 512KB
                BufferSize = 32 * 1024, // 32KB
                MaxRetries = 3,
                TimeoutMs = 90000,
                EnableConnectionPooling = false, // 减少内存占用
                SpeedLimit = 500 * 1024 // 500KB/s限制
            },
            NetworkConfig = new NetworkConfiguration
            {
                EnableCompression = true,
                EnableKeepAlive = false
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = false,
                MaxThreads = 3,
                EnablePerformanceMonitoring = false, // 节省资源
                MonitoringIntervalMs = 10000 // 10秒监控
            }
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
            Description = "服务器环境配置，最大化利用资源",
            DownloadConfig = new DownloadConfiguration
            {
                ThreadCount = 32,
                ChunkSize = 16 * 1024 * 1024, // 16MB
                BufferSize = 256 * 1024, // 256KB
                MaxRetries = 10,
                TimeoutMs = 180000, // 3分钟
                EnableConnectionPooling = true,
                PreAllocateFile = true
            },
            NetworkConfig = new NetworkConfiguration
            {
                ConnectionTimeoutMs = 30000,
                ReadTimeoutMs = 180000,
                EnableHttp2 = true,
                EnableCompression = true,
                EnableKeepAlive = true
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                AutoAdjustThreads = true,
                MaxThreads = 64,
                AutoAdjustChunkSize = true,
                MaxChunkSize = 64 * 1024 * 1024, // 64MB
                RetryStrategy = RetryStrategy.ExponentialBackoff,
                EnablePerformanceMonitoring = true,
                MonitoringIntervalMs = 500 // 500ms高频监控
            }
        };
    }
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    private static DownloadConfigurationProfile CreateDefaultProfile()
    {
        return new DownloadConfigurationProfile
        {
            Name = "Default",
            Description = "默认下载配置",
            IsDefault = true
        };
    }
    
    /// <summary>
    /// 加载所有配置文件
    /// </summary>
    private void LoadAllProfiles()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
                return;
                
            var configFiles = Directory.GetFiles(_configDirectory, "*" + ConfigFileExtension);
            
            foreach (var file in configFiles)
            {
                try
                {
                    var profile = LoadProfileFromFile(file);
                    _profiles[profile.Name] = profile;
                    
                    if (profile.IsDefault)
                        _activeProfile = profile;
                }
                catch (Exception ex)
                {
                    LogWrapper.Warn(LogModule, $"加载配置文件失败: {file}, 错误: {ex.Message}");
                }
            }
            
            // 如果没有找到默认配置，创建一个
            if (_activeProfile == null)
            {
                _activeProfile = CreateDefaultProfile();
                SaveProfile(_activeProfile);
            }
            
            LogWrapper.Info(LogModule, $"已加载 {_profiles.Count} 个配置文件");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, "加载配置文件时发生错误");
        }
    }
    
    /// <summary>
    /// 从文件加载配置
    /// </summary>
    private DownloadConfigurationProfile LoadProfileFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        return JsonSerializer.Deserialize<DownloadConfigurationProfile>(json, options) 
               ?? throw new InvalidOperationException($"无法解析配置文件: {filePath}");
    }
    
    /// <summary>
    /// 保存配置到文件
    /// </summary>
    /// <param name="profile">配置实例</param>
    public void SaveProfile(DownloadConfigurationProfile profile)
    {
        try
        {
            EnsureConfigDirectory();
            
            profile.LastModified = DateTime.Now;
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            
            var json = JsonSerializer.Serialize(profile, options);
            var fileName = profile.Name.Replace(" ", "-").ToLower() + ConfigFileExtension;
            var filePath = Path.Combine(_configDirectory, fileName);
            
            File.WriteAllText(filePath, json);
            
            _profiles[profile.Name] = profile;
            
            LogWrapper.Info(LogModule, $"配置已保存: {profile.Name}");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, $"保存配置失败: {profile.Name}");
            throw;
        }
    }
    
    /// <summary>
    /// 删除配置
    /// </summary>
    /// <param name="profileName">配置名称</param>
    public bool DeleteProfile(string profileName)
    {
        try
        {
            if (!_profiles.TryGetValue(profileName, out var profile))
                return false;
                
            if (profile.IsDefault)
                throw new InvalidOperationException("不能删除默认配置");
                
            var fileName = profileName.Replace(" ", "-").ToLower() + ConfigFileExtension;
            var filePath = Path.Combine(_configDirectory, fileName);
            
            if (File.Exists(filePath))
                File.Delete(filePath);
                
            _profiles.Remove(profileName);
            
            // 如果删除的是活动配置，切换到默认配置
            if (_activeProfile?.Name == profileName)
                _activeProfile = GetDefaultProfile();
                
            LogWrapper.Info(LogModule, $"配置已删除: {profileName}");
            return true;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, $"删除配置失败: {profileName}");
            return false;
        }
    }
    
    /// <summary>
    /// 切换活动配置
    /// </summary>
    /// <param name="profileName">配置名称</param>
    public bool SetActiveProfile(string profileName)
    {
        if (_profiles.TryGetValue(profileName, out var profile))
        {
            _activeProfile = profile;
            ConfigurationChanged?.Invoke(profile);
            
            LogWrapper.Info(LogModule, $"已切换到配置: {profileName}");
            return true;
        }
        
        LogWrapper.Warn(LogModule, $"配置不存在: {profileName}");
        return false;
    }
    
    /// <summary>
    /// 获取默认配置
    /// </summary>
    private DownloadConfigurationProfile GetDefaultProfile()
    {
        return _profiles.Values.FirstOrDefault(p => p.IsDefault) ?? CreateDefaultProfile();
    }
    
    /// <summary>
    /// 确保配置目录存在
    /// </summary>
    private void EnsureConfigDirectory()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
    }
    
    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public void ResetToDefaults()
    {
        var defaultProfile = CreateDefaultProfile();
        SaveProfile(defaultProfile);
        SetActiveProfile(defaultProfile.Name);
        
        LogWrapper.Info(LogModule, "已重置为默认配置");
    }
    
    /// <summary>
    /// 导出配置
    /// </summary>
    /// <param name="profileName">配置名称</param>
    /// <param name="exportPath">导出路径</param>
    public void ExportProfile(string profileName, string exportPath)
    {
        if (!_profiles.TryGetValue(profileName, out var profile))
            throw new ArgumentException($"配置不存在: {profileName}");
            
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var json = JsonSerializer.Serialize(profile, options);
        File.WriteAllText(exportPath, json);
        
        LogWrapper.Info(LogModule, $"配置已导出: {profileName} -> {exportPath}");
    }
    
    /// <summary>
    /// 导入配置
    /// </summary>
    /// <param name="importPath">导入路径</param>
    public DownloadConfigurationProfile ImportProfile(string importPath)
    {
        if (!File.Exists(importPath))
            throw new FileNotFoundException($"配置文件不存在: {importPath}");
            
        var profile = LoadProfileFromFile(importPath);
        
        // 确保名称唯一
        var originalName = profile.Name;
        var counter = 1;
        while (_profiles.ContainsKey(profile.Name))
        {
            profile.Name = $"{originalName}_{counter++}";
        }
        
        profile.CreatedAt = DateTime.Now;
        profile.LastModified = DateTime.Now;
        profile.IsDefault = false;
        
        SaveProfile(profile);
        
        LogWrapper.Info(LogModule, $"配置已导入: {profile.Name} <- {importPath}");
        return profile;
    }
}
