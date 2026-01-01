using System;

namespace PCL.Core.Net.Downloader;

public class MirrorFailedException(Uri uri, Exception inner)
    : Exception($"Mirror {uri} failed.", inner);