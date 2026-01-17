using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Minecraft;

/// <summary>
/// 版本区间（左闭右开区间）
/// </summary>
public class McVersionRange(McVersion start, McVersion end)
{
    public McVersion Start { get; } = start;
    public McVersion End { get; } = end;

    public virtual bool Validate(McVersion version)
        => version.ReleaseTime >= Start.ReleaseTime && version.ReleaseTime < End.ReleaseTime;

    public override string ToString()
    {
        return $"{Start.Id}..{End.Id}";
    }
}

/// <summary>
/// 版本区间（闭区间）
/// </summary>
public class McVersionRangeClosed(McVersion start, McVersion end) : McVersionRange(start, end)
{
    public override bool Validate(McVersion version)
        => version.ReleaseTime >= Start.ReleaseTime && version.ReleaseTime <= End.ReleaseTime;

    public override string ToString()
    {
        if (Start.Id == End.Id)
            return $"{Start.Id}";
        return $"{Start.Id}..={End.Id}";
    }
}


public class McVersionRanges(IList<McVersionRange> ranges)
{
    public List<McVersionRange> RangeList { get; set; } = [..ranges];

    public bool Validate(McVersion version)
        => RangeList.Any(range => range.Validate(version));

    /// <summary>
    /// 解析文本形式的版本区间。
    /// </summary>
    /// <param name="versionRange">
    /// 代表版本范围的字符串。<br/>
    /// 使用规范（多个范围以半角逗号分隔）：<br/>
    /// "A..B" => [A, B) <br/>
    /// "A..=B" => [A, B] <br/>
    /// "A" => {A} <br/>
    /// </param>
    /// <returns></returns>
    public static McVersionRanges Parse(string versionRange)
    {
        IList<McVersionRange> ranges = [];
        versionRange = versionRange.Replace(" ", "");
        foreach (var str in versionRange.Split(","))
        {
            if (str.Contains("..="))
                ranges.Add(new McVersionRangeClosed(
                    new McVersion(str.Split("..=")[0]),
                    new McVersion(str.Split("..=")[1])
                ));
            else if (str.Contains(".."))
                ranges.Add(new McVersionRange(
                    new McVersion(str.Split("..=")[0]),
                    new McVersion(str.Split("..=")[1])
                ));
            else
                ranges.Add(new McVersionRangeClosed(new McVersion(str), new McVersion(str)));
        }
        return new McVersionRanges(ranges);
    }

    public override string ToString()
    {
        var str = RangeList.Aggregate("", (current, range) => current + $", {range}");
        return str[2..];
    }
}