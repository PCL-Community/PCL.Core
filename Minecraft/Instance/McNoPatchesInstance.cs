using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Folder;
using PCL.Core.Minecraft.Instance.Handler;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Resources;
using PCL.Core.Minecraft.Launch;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.Instance;

/// <summary>
/// 管理实例基础信息
/// </summary>
public class McNoPatchesInstance : IMcInstance {
    // 使用缓存以避免复杂属性的重复计算
    private JsonObject? _versionJson;
    private JsonObject? _versionJsonInJar;
    private McInstanceInfo? _instanceInfo;
    private McInstanceCardType _cachedCardType;

    private List<Library>? _libraries; // 依赖库列表
    private HashSet<string>? _libraryNameHashCache; // 依赖库哈希缓存
    private AssetIndex? _assetIndex;

    /// <summary>
    /// 初始化 Minecraft 实例
    /// 初始化后请一定要先运行 <c>CheckAsync()</c> 方法
    /// 在你调用其他方法时，我们默认你已经调用了 <c>CheckAsync()</c> 并且通过了检查
    /// </summary>
    /// <param name="path"></param>
    public McNoPatchesInstance(string path) {
        // 定义基础路径
        var basePath = System.IO.Path.Combine(McFolderManager.PathMcFolder, "versions");

        // 判断是否为绝对路径，并拼接正确的路径
        Path = path.Contains(':') ? path : System.IO.Path.Combine(basePath, path);
    }
    
    public string Path { get; }
    
    public string Name => InstanceBasicHandler.GetName(Path);
    
    public string IsolatedPath {
        get {
            if (_instanceInfo == null) {
                GetInstanceInfo();
            }
            return InstanceIsolationHandler.GetIsolatedPath(this);
        }
    }
    
    public string Desc { get; set; } = "该实例未被加载，请向作者反馈此问题";

    public string Logo { get; set; } = Basics.GetAppImagePath("Blocks/RedstoneBlock.png");
    
    public bool IsStarred => InstanceBasicHandler.GetIsStarred(Path);
    
    public McInstanceCardType CardType {
        get {
            if (!InstanceBasicHandler.HasCorrectCardType(_cachedCardType)) {
                _cachedCardType = InstanceBasicHandler.RefreshInstanceCardType(this);
            }
            return _cachedCardType;
        }
        set {
            if (InstanceBasicHandler.HasCorrectCardType(_cachedCardType)) {
                return;
            }
            _cachedCardType = value;
            Config.Instance.CardType[Path] = (int)value;
        }
    }
    
    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    private async Task GetVersionJsonAsync() {
        _versionJson ??= await InstanceJsonHandler.RefreshVersionJsonAsync(this);
    }

    private async Task RefreshVersionJsonAsync() {
        _versionJson = await InstanceJsonHandler.RefreshVersionJsonAsync(this);
    }

    /// <summary>
    /// 异步获取 Jar 中的 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 Jar 中的 JSON 对象。</returns>
    private async Task GetVersionJsonInJarAsync() {
        _versionJsonInJar ??= await InstanceJsonHandler.RefreshVersionJsonInJarAsync(this);
    }

    private async Task RefreshVersionJsonInJarAsync() {
        _versionJsonInJar = await InstanceJsonHandler.RefreshVersionJsonInJarAsync(this);
    }
    
    public McInstanceInfo? InstanceInfo {
        get {
            return _instanceInfo ??= RefreshInstanceInfo();
        }
        set {
            _instanceInfo = value;
        }
    }

    /// <summary>
    /// 从 HashSet 中查找以指定前缀开头的 name 并提取版本号
    /// </summary>
    /// <param name="prefix">目标前缀</param>
    /// <returns>匹配的版本号或如果未找到</returns>
    private string? FindPatcherVersionsInHashSet(string prefix) {
        return _libraryNameHashCache!.Where(name => name.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
            .Select(name => name[(prefix.Length + 1)..])
            .FirstOrDefault();
    }

    public async Task<(Version MinVer, Version MaxVer)> GetCompatibleJavaVersionRangeAsync() {
        await GetVersionJsonInJarAsync();
        return _instanceJavaHandler.GetCompatibleJavaVersionRange();
    }
    
    /// <summary>
    /// 实例信息
    /// </summary>
    public McInstanceInfo? GetInstanceInfo() {
        return _instanceInfo ??= RefreshInstanceInfo();
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public bool IsOldJson => _versionJson!.ContainsKey("minecraftArguments");

    public async Task CheckAsync() {
        if (!await CheckPermissionAsync() || !await CheckJsonAsync()) {
            DisplayType = McInstanceCardType.Error;
            Logo = System.IO.Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
        } else {
            // 确定实例图标
            Logo = _instanceUiHandler.GetLogo();
        }
    }

    private async Task<bool> CheckPermissionAsync() {
        if (!Directory.Exists(Path)) {
            Desc = $"未找到实例 {this.Name}";
            return false;
        }

        try {
            Directory.CreateDirectory(Path + "PCL\\");
            await Directories.CheckPermissionWithExceptionAsync(Path + "PCL\\");
        } catch (Exception ex) {
            Desc = "PCL 没有对该文件夹的访问权限，请以管理员身份运行";
            LogWrapper.Warn(ex, "没有访问实例文件夹的权限");
            return false;
        }
        return true;
    }

    private async Task<bool> CheckJsonAsync() {
        await GetVersionJsonAsync();
        if (_versionJson == null) {
            LogWrapper.Warn($"实例 JSON 可用性检查失败（{Path}）");
            Desc = "实例 JSON 不存在或无法解析";
            return false;
        }
        
        GetInstanceInfo();
        if (_instanceInfo == null) {
            LogWrapper.Warn($"实例信息检查失败（{Path}）");
            Desc = "无法识别实例信息";
            return false;
        }

        return true;
    }

    public void Load() {
        GetInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    public async Task RefreshAsync() {
        await RefreshVersionJsonAsync();

        RefreshInstanceInfo();
        RefreshInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    private void SetDescriptiveInfo() {
        // 确定实例描述和状态
        Desc = _instanceUiHandler.GetDescription();

        // 写入缓存
        Config.Instance.Info[Path] = Desc;
        Config.Instance.LogoPath[Path] = Logo!;
    }

    #region Libraries

    public List<Library>? Libraries => _libraries ?? ParseLibrariesFromJson();

    // 从完整JSON中提取并反序列化libraries字段
    private List<Library>? ParseLibrariesFromJson() {
        try {
            // 获取libraries字段
            var librariesNode = _versionJson!["libraries"];
            if (librariesNode == null) {
                Console.WriteLine("JSON中未找到libraries字段");
                _libraries = null;
                return null;
            }

            // 反序列化libraries字段
            _libraries = LibraryDeserializer.DeserializeLibraries(librariesNode);
            return _libraries;
        } catch (JsonException ex) {
            Console.WriteLine($"JSON解析或反序列化错误: {ex.Message}");
            _libraries = null;
            return null;
        }
    }

    #endregion

    #region Asset Index

    /// <summary>
    /// 从完整JSON中提取并反序列化assetIndex字段
    /// </summary>
    /// <returns>反序列化后的AssetIndex对象，如果不存在或失败则返回null</returns>
    public AssetIndex? ParseAssetIndexFromJson() {
        try {
            var assetIndexNode = _versionJson!["assetIndex"];
            if (assetIndexNode == null) {
                LogWrapper.Warn("JSON中未找到assetIndex字段");
                _assetIndex = null;
                return null;
            }
            
            _assetIndex = AssetIndexDeserializer.DeserializeAssetIndex(assetIndexNode);
            return _assetIndex;
        } catch (JsonException ex) {
            LogWrapper.Warn(ex, "JSON解析或反序列化错误");
            _assetIndex = null;
            return null;
        }
    }

    #endregion
}
