using System;

namespace PCL.Core.Net.Downloader;

public class DownloadRangeNotSupportedException(Uri uri)
    : Exception($"Mirror {uri} does not support range requests.");