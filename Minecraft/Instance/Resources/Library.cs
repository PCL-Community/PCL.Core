using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Instance.Resources;

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

    // 检查是否满足rules条件
    public bool CheckRules() {
        if (Rules == null || Rules.Count == 0)
            return true; // 没有规则，默认允许

        var required = false;

        foreach (var rule in Rules) {
            var ruleMatches = true; // 当前规则是否匹配

            // 检查操作系统条件
            // Using a property pattern
            // 简化后的代码（C# 9/10 语法）
            if (rule is { Os.Name: not null }) {
                var osName = rule.Os.Name.ToLowerInvariant();
                var currentOs = EnvironmentInterop.GetCurrentOsName();

                // 仅当操作系统名称匹配时继续检查
                if (osName == "unknown" || osName != currentOs) {
                    ruleMatches = false;
                } else if (osName == currentOs && rule.Os.Version != null) {
                    // 检查操作系统版本
                    try {
                        var versionPattern = rule.Os.Version;
                        var osVersion = Environment.OSVersion.Version.ToString();
                        ruleMatches = ruleMatches && Regex.IsMatch(osVersion, versionPattern);
                    } catch (RegexParseException) {
                        // 无效的正则表达式，规则不匹配
                        ruleMatches = false;
                    }
                }

                // 检查系统架构（x86 或 x64）
                if (rule.Os.Arch != null) {
                    var is32BitSystem = !Environment.Is64BitOperatingSystem;
                    ruleMatches = ruleMatches && string.Equals(rule.Os.Arch, "x86", StringComparison.OrdinalIgnoreCase) == is32BitSystem;
                }
            }

            // 根据action更新结果
            switch (rule.Action) {
                case "allow":
                    if (ruleMatches) {
                        required = true; // 规则匹配，允许使用
                    }
                    break;
                case "disallow":
                    if (ruleMatches) {
                        required = false; // 规则匹配，禁止使用
                    }
                    break;
            }
        }

        return required;
    }
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

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }
}

// Deserialization method
public static class LibraryDeserializer {
    public static Library? DeserializeLibrary(JsonNode? json) {
        try {
            return json.Deserialize<Library>(Files.PrettierJsonOptions);
        } catch (JsonException ex) {
            LogWrapper.Warn($"依赖库反序列化错误: {ex.Message}");
            return null;
        }
    }

    // 反序列化依赖库列表
    public static List<Library>? DeserializeLibraries(JsonNode? json) {
        try {
            return json.Deserialize<List<Library>>(Files.PrettierJsonOptions);
        } catch (JsonException ex) {
            LogWrapper.Warn($"依赖库列表反序列化错误: {ex.Message}");
            return null;
        }
    }
}
