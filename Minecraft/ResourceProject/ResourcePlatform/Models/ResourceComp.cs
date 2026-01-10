using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject.ResourcePlatform.Models;

public class ResourceComp
{
    private string? _mcModUrl;
    /// <summary>
    /// 资源名称
    /// </summary>
    public required string Title;
    /// <summary>
    /// 资源简介
    /// </summary>
    public required string Description;
    /// <summary>
    /// 资源图标的 Url
    /// </summary>
    public string? IconUrl;
    /// <summary>
    /// 资源类型
    /// </summary>
    public ResourceType Type;
    public ProjectSource Source;
    public required List<string> Authors;
    public string? I18NName;

    public string? McmodUrl
    {
        get => _mcModUrl ?? "https://www.mcmod.cn";
        set => _mcModUrl = value;
    }

    public required List<string> SupportMcVersions;

    public int DownloadCount { get; set; }
    
}