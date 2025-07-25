using System;
using System.Collections.Generic;
using System.Text;

namespace PCL.Core.Utils;

using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ArgumentsBuilder
{
    private readonly Dictionary<string, string?> _args = new();

    /// <summary>
    /// 添加键值对参数（自动处理空格转义）
    /// </summary>
    /// <param name="key">参数名（不带前缀）</param>
    /// <param name="value">参数值</param>
    public ArgumentsBuilder Add(string key, string value)
    {
        if (key is null) throw new NullReferenceException(nameof(key));
        if (value is null) throw new NullReferenceException(nameof(value));
        _args[key] = _handleEscapeValue(value);
        return this;
    }

    /// <summary>
    /// 添加标志参数（无值参数）
    /// </summary>
    /// <param name="flag">标志名（不带前缀）</param>
    public ArgumentsBuilder AddFlag(string flag)
    {
        if (flag is null) throw new NullReferenceException(nameof(flag));
        _args[flag] = null;
        return this;
    }

    /// <summary>
    /// 条件添加参数（仅当condition为true时添加）
    /// </summary>
    public ArgumentsBuilder AddIf(bool condition, string key, string value)
    {
        if (condition) Add(key, value);
        return this;
    }

    /// <summary>
    /// 条件添加标志（仅当condition为true时添加）
    /// </summary>
    public ArgumentsBuilder AddFlagIf(bool condition, string flag)
    {
        if (condition) AddFlag(flag);
        return this;
    }

    /// <summary>
    /// 构建参数字符串
    /// </summary>
    /// <param name="prefixStyle">前缀样式：0=自动（单字符用-，多字符用--），1=强制单横线，2=强制双横线</param>
    public string Build(int prefixStyle = 0)
    {
        var sb = new StringBuilder();

        foreach (var arg in _args)
        {
            if (sb.Length > 0) sb.Append(' ');

            // 添加前缀
            switch (prefixStyle)
            {
                case 1: // 强制单横线
                    sb.Append('-').Append(arg.Key);
                    break;
                case 2: // 强制双横线
                    sb.Append("--").Append(arg.Key);
                    break;
                default: // 自动判断
                    sb.Append(arg.Key.Length == 1 ? "-" : "--").Append(arg.Key);
                    break;
            }

            // 添加值（如果有）
            if (arg.Value is not null)
            {
                sb.Append('=').Append(arg.Value);
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return Build();
    }

    /// <summary>
    /// 清空所有参数
    /// </summary>
    public void Clear() => _args.Clear();

    // 转义包含空格的值（用双引号包裹）
    private static string _handleEscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        return value.Contains(' ') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"")}\""  // 处理双引号转义
            : value;
    }
}