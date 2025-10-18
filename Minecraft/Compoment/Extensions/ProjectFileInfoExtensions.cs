using System;
using System.Collections.Generic;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Compoment.Extensions;

public static class ProjectFileInfoExtensions
{
    public static string ToJsonSerialized(this ProjectFileInfo info)
    {
        var content = JsonSerializer.Serialize(info);
        return content;
    }

    public static List<DownloadItem> ToDownloadItem(this ProjectFileInfo info, string localPath)
    {
        List<DownloadItem> items = [];
        string savePath;
        if (localPath.EndsWith('\\'))
        {
            savePath = info.FileName ??
                       throw new ArgumentNullException(nameof(info.FileName), "Save path can not be null.");
        }
        else
        {
            savePath = string.Empty;
        }

        if (info.DownloadUrls is null)
        {
            throw new ArgumentNullException(nameof(info.DownloadUrls), "Download URLs cannot be null.");
        }

        foreach (var url in info.DownloadUrls)
        {
            var uri = new Uri(url);
            var item = new DownloadItem(uri, savePath);

            items.Add(item);
        }


        return items;
    }
}