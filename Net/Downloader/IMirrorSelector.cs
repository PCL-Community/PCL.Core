using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net.Downloader;

public interface IMirrorSelector
{
    Task<Uri> GetBestMirrorAsync(IEnumerable<Uri> uris, CancellationToken token);
}