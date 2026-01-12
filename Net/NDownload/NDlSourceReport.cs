namespace PCL.Core.Net.NDownload;

public record NDlSourceReport(
    bool IsSupportSegment = true,
    int RetryCount = 0,
    long AverageSpeed = -1
);
