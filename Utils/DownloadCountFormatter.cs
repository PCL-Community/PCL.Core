namespace PCL.Core.Utils;

public static class DownloadCountFormatter
{
    public static string FormatDownloadCount(int count)
    {
        return count switch
        {
            >= 1_000_000_000 => $"{count / 1_000_000_000.0:F1} 亿",
            >= 10_000 => $"{count / 10_000.0:F1} 万",
            >= 1_000 => $"{count / 1_000.0:F1} 千",
            _ => $"{count}",
        };
    }
}