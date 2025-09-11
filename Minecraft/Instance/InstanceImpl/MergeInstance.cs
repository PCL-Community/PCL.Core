using System;
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
using PCL.Core.Minecraft.Instance.Handler.InstanceInfo;
using PCL.Core.Minecraft.Instance.InstanceImpl.JsonBased.Patch;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Resources;

namespace PCL.Core.Minecraft.Instance.InstanceImpl;

/// <summary>
/// 管理实例基础信息
/// </summary>
public class MergeInstance : IMcInstance{
    // 使用缓存以避免复杂属性的重复计算
    private JsonObject? _versionJson;
    private JsonObject? _versionJsonInJar;
    private PatchInstanceInfo? _instanceInfo;
    private McInstanceCardType _cachedCardType;

    private List<Library>? _libraries; // 依赖库列表
    private AssetIndex? _assetIndex;

    /// <summary>
    /// 初始化 Merge JSON 类型的 Minecraft 实例
    /// </summary>
    public MergeInstance(string path, string? logo) {
        // 定义基础路径
        var basePath = System.IO.Path.Combine(McFolderManager.PathMcFolder, "versions");

        // 判断是否为绝对路径，并拼接正确的路径
        Path = path.Contains(':') ? path : System.IO.Path.Combine(basePath, path);
        
        Logo = logo ?? Basics.GetAppImagePath("Blocks/RedstoneBlock.png");
    }
    
    public string Path { get; }
    
    public string Name => InstanceBasicHandler.GetName(Path);
    
    public string IsolatedPath {
        get {
            return InstanceIsolationHandler.GetIsolatedPath(this);
        }
    }
    
    public string Desc { get; set; } = "该实例未被加载，请向作者反馈此问题";

    public string Logo { get; set; }
    
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
    
    public PatchInstanceInfo InstanceInfo {
        get {
            if (_instanceInfo == null) {
                McInstanceFactory.UpdateFromClonedInstance(this, InstanceMergeHandler.RefreshMergeInstanceInfo(this, _versionJson!, Libraries!));
            }
            return _instanceInfo!;
        }
        set => _instanceInfo = value;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public bool IsOldJson => _versionJson!.ContainsKey("minecraftArguments");
    

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
