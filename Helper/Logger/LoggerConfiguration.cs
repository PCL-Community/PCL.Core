namespace PCL.Core.Helper.Logger;

public record LoggerConfiguration(
    string StoreFolder,
    LoggerSegmentMode SegmentMode,
    long MaxFileSize,
    string? FileNameFormat,
    bool AutoDeleteOldFile,
    int MaxKeepOldFile);