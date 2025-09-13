using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Launch.Utils;

public static class McLaunchUtils {
    public static void Log(string msg) {
        // TODO: UI Log
        LogWrapper.Info("McLaunch", msg);
    }
    
    // 检查是否满足rules条件
    public static bool CheckRules(JsonObject? rulesObj) {
        if (rulesObj == null) return false;
        
        var rules = rulesObj.Deserialize<List<Rule>>();
        if (rules == null || rules.Count == 0)
            return true; // 没有规则，默认允许

        var required = false;

        foreach (var rule in rules) {
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
