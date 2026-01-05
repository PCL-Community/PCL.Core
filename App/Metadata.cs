using System.Text.Json.Serialization;

namespace PCL.Core.App;

public sealed record LauncherVersionModel(
    [property: JsonPropertyName("base")] string BaseName,
    [property: JsonPropertyName("suffix")] string Suffix,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("upstream")] string UpstreamVersion
) {
}

public sealed record MetadataModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] LauncherVersionModel Version
);
