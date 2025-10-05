using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record ProjectFileInfo
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public required string ProjectId { get; init; } // NOTE: old ver ProjectFileInfo not have this property

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]

    public DateTime? ReleaseDate { get; set; } // NOTE: old ver ProjectFileInfo not have this

    [JsonPropertyName("fromCurseForge")] public bool FromCurseForge { get; init; }
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("displayName")] public required string DisplayName { get; init; }
    [JsonPropertyName("downloadCount")] public required int DownloadCount { get; init; }
    [JsonPropertyName("modLoaders")] public required IReadOnlyList<LoaderType> ModLoaders { get; init; }
    [JsonPropertyName("gameVersions")] public required IReadOnlyList<string> GameVersions { get; init; }
    [JsonPropertyName("status")] public required ProjectFileStatus Status { get; init; }

    [JsonPropertyName("fileName")] public string? FileName { get; init; }
    [JsonPropertyName("downloadUrls")] public List<string>? DownloadUrls { get; init; }
    [JsonPropertyName("hash")] public string? Hash { get; init; }
    [JsonPropertyName("rawDependencies")] public required IReadOnlyList<string> RawDependencies { get; init; }

    [JsonPropertyName("rawOptionalDependencies")]
    public required IReadOnlyList<string> RawOptionalDependencies { get; init; }

    [JsonPropertyName("denpendencies")] public required List<string> Dependencies { get; init; }

    [JsonPropertyName("optionalDependencies")]
    public required List<string> OptionalDependencies { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public required CompType Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string StatusDescription
    {
        get
        {
            return Status switch
            {
                ProjectFileStatus.Release => "正式版",
                ProjectFileStatus.Beta => "测试版",
                ProjectFileStatus.Alpha => "早期测试版",
                _ => string.Empty
            };
        }
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Available => !(string.IsNullOrEmpty(FileName) || DownloadUrls?.Count != 0);

    /// <inheritdoc />
    public override string ToString() => $"{Id}: {FileName}";
}