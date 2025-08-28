namespace PCL.Core.Utils;

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 封装异常信息的数据结构。
/// </summary>
public record ExceptionData(
    string CommonReason,
    List<string> DescriptionList,
    List<string> StackTraceList);

public static class ExceptionFormatter {
    /// <summary>
    /// 提取 Exception 的详细描述与堆栈信息。
    /// </summary>
    /// <param name="ex">要处理的异常对象。</param>
    /// <param name="showAllStacks">是否必须显示所有堆栈。通常用于判定堆栈信息。</param>
    /// <returns>包含详细描述与堆栈的格式化字符串。</returns>
    public static string GetExceptionDetail(Exception? ex, bool showAllStacks = false) {
        if (ex is null) {
            return "无可用错误信息！";
        }

        var data = GetExceptionInfo(ex, showAllStacks);
        var sb = new StringBuilder();

        if (data.CommonReason is not null) {
            sb.AppendLine(data.CommonReason)
                .AppendLine()
                .AppendLine("————————————")
                .AppendLine("详细错误信息：");
        }

        sb.AppendLine(string.Join(Environment.NewLine, data.DescriptionList));

        return sb.ToString();
    }

    /// <summary>
    /// 提取 Exception 的描述，汇总到一行。
    /// </summary>
    /// <param name="ex">要处理的异常对象。</param>
    /// <returns>包含汇总描述的格式化字符串。</returns>
    public static string GetExceptionSummary(Exception? ex) {
        if (ex is null) {
            return "无可用错误信息！";
        }

        var data = GetExceptionInfo(ex, false);

        if (data.CommonReason is not null) {
            return $"{data.CommonReason}详细错误：{data.DescriptionList.First()}";
        } else {
            var uniqueDescriptions = data.DescriptionList.Distinct().ToList();
            uniqueDescriptions.Reverse();
            return string.Join(" ← ", uniqueDescriptions);
        }
    }

    /// <summary>
    /// 核心方法：处理异常并提取所有必要信息。
    /// </summary>
    private static ExceptionData GetExceptionInfo(Exception ex, bool showAllStacks) {
        // 步骤1：获取最底层的异常
        var innerEx = GetInnermostException(ex);

        // 步骤2：获取各级错误的描述与堆栈信息
        var descriptions = new List<string>();
        var stacks = new List<string>();
        var currentEx = ex;

        while (currentEx is not null) {
            descriptions.Add(currentEx.Message.ReplaceLineEndings(" "));

            if (currentEx.StackTrace is not null) {
                foreach (var stack in currentEx.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (showAllStacks || stack.Contains("pcl", StringComparison.OrdinalIgnoreCase)) {
                        stacks.Add(stack.Trim());
                    }
                }
            }
            if (currentEx.GetType().FullName != "System.Exception") {
                descriptions.Add($"   错误类型：{currentEx.GetType().FullName}");
            }

            currentEx = currentEx.InnerException;
        }

        // 步骤3：判断常见错误原因
        var commonReason = GetCommonReason(innerEx, descriptions);

        return new ExceptionData(commonReason!, descriptions, stacks);
    }

    /// <summary>
    /// 核心方法：获取最内层的异常。
    /// </summary>
    private static Exception GetInnermostException(Exception ex) {
        var innerEx = ex;
        while (innerEx.InnerException is not null) {
            innerEx = innerEx.InnerException;
        }
        return innerEx;
    }

    /// <summary>
    /// 核心方法：根据异常类型或描述获取常见错误原因。
    /// </summary>
    private static string? GetCommonReason(Exception innerEx, List<string> descriptions) {
        // 使用 switch 表达式匹配常见类型
        var commonReason = innerEx switch {
            TypeLoadException or BadImageFormatException or MissingMethodException or NotImplementedException or TypeInitializationException =>
                "PCL 的运行环境存在问题。请尝试重新安装 .NET Framework 4.8.1 然后再试。若无法安装，请先卸载较新版本的 .NET Framework，然后再尝试安装。",
            UnauthorizedAccessException =>
                "PCL 的权限不足。请尝试右键 PCL，选择以管理员身份运行。",
            OutOfMemoryException =>
                "你的电脑运行内存不足，导致 PCL 无法继续运行。请在关闭一部分不需要的程序后再试。",
            System.Runtime.InteropServices.COMException =>
                "由于操作系统或显卡存在问题，导致出现错误。请尝试重启 PCL。",
            PlatformNotSupportedException =>
                "你当前的 Windows 版本过低，无法运行当前版本的 PCL。请升级到 Windows 10 或更高版本后再试。",
            _ => null
        };

        // 如果类型匹配失败，则通过描述进行匹配
        if (commonReason is null) {
            var networkErrorMessages = new HashSet<string> {
                "远程主机强迫关闭了", "远程方已关闭传输流", "未能解析此远程名称", "由于目标计算机积极拒绝",
                "操作已超时", "操作超时", "服务器超时", "连接超时"
            };
            var fullDescription = string.Join(" ", descriptions);
            if (networkErrorMessages.Any(s => fullDescription.Contains(s))) {
                commonReason = "你的网络环境不佳，导致难以连接到服务器。请稍后重试，或使用 VPN 以改善网络环境。";
            }
        }
        return commonReason;
    }
}
