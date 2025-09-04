using PCL.Core.IO;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.McInstance.Resources;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

// Root class for the library object
public class Library {
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("downloads")]
    public Downloads? Downloads { get; set; }

    [JsonPropertyName("extract")]
    public Extract? Extract { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }
}

// Downloads object containing artifact and classifiers
public class Downloads {
    [JsonPropertyName("artifact")]
    public Artifact? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Classifiers? Classifiers { get; set; }
}

// Artifact object for file details
public class Artifact {
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

// Classifiers object for platform-specific artifacts
public class Classifiers {
    [JsonPropertyName("natives-linux")]
    public Artifact? NativesLinux { get; set; }

    [JsonPropertyName("natives-macos")]
    public Artifact? NativesMacos { get; set; }

    [JsonPropertyName("natives-osx")]
    public Artifact? NativesOsx { get; set; }

    [JsonPropertyName("natives-windows")]
    public Artifact? NativesWindows { get; set; }

    [JsonPropertyName("javadoc")]
    public Artifact? Javadoc { get; set; }

    [JsonPropertyName("sources")]
    public Artifact? Sources { get; set; }
}

// Extract object for extraction rules
public class Extract {
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }
}

// Rule object for conditional actions
public class Rule {
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("os")]
    public Os? Os { get; set; }
}

// Os object for operating system conditions in rules
public class Os {
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// Deserialization method
public static class LibraryDeserializer {
    public static Library? DeserializeLibrary(string json) {
        try {
            return JsonSerializer.Deserialize<Library>(json, Files.PrettierJsonOptions);
        } catch (JsonException ex) {
            LogWrapper.Warn($"依赖库反序列化错误: {ex.Message}");
            return null;
        }
    }

    // 反序列化依赖库列表
    public static List<Library>? DeserializeLibraries(string json) {
        try {
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true // 忽略大小写
            };
            return JsonSerializer.Deserialize<List<Library>>(json, options);
        } catch (JsonException ex) {
            LogWrapper.Warn($"依赖库列表反序列化错误: {ex.Message}");
            return null;
        }
    }
}
