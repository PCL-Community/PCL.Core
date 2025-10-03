using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

namespace PCL.Core.Minecraft.Compoment.LocalComp;

public class LocalModFile : LocalResource
{
    public ModMetadata? Metadata { get; set; }

    public Dictionary<string, string?> Dependencies { get; } = new();

    private static readonly List<IModMetadataParser> _Parsers =
    [
        new LegacyForgeModParser(),
        new ForgeModParser(),
        new FabricModJsonParser(),
        new QuitModParser(),
        new PackPngParser()
    ];

    /// <inheritdoc />
    public LocalModFile(string path) : base(path)
    {
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Throw if Metadata is null after parsing</exception>
    public override BaseResourceData? Load(bool lazy = false)
    {
        if (File.Exists(Path))
        {
            FileUnavailableReason = new FileNotFoundException("Resource file not found.", Path);
            State = FileStatus.Unavailable;
            return null;
        }

        if (Path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) ||
            Path.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
        {
            State = FileStatus.Disabled;
        }

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(Path);
            foreach (var parser in _Parsers)
            {
                parser.TryParse(archive, this);
            }
        }
        catch (Exception ex)
        {
            FileUnavailableReason = ex;
            State = FileStatus.Unavailable;
        }

        ArgumentNullException.ThrowIfNull(Metadata);

        if (string.IsNullOrEmpty(Metadata.Name))
        {
            Metadata = Metadata with { Name = System.IO.Path.GetFileNameWithoutExtension(RawFileName) };
        }

        return null;
    }

    public void AddDependency(string modId, string? versionReq = null)
    {
        if (string.IsNullOrEmpty(modId) && modId.Length < 2)
        {
            return;
        }

        if (modId.Equals("name", StringComparison.OrdinalIgnoreCase) ||
            int.TryParse(modId, out _))
        {
            return;
        }

        if (versionReq is null)
        {
            versionReq = string.Empty;
        }
        else if (!versionReq.Contains('.') && !versionReq.Contains('-'))
        {
            versionReq = string.Empty;
        }
        else if (versionReq.Contains('$'))
        {
            versionReq = string.Empty;
        }
        else
        {
            var preFix = versionReq[0];
            var subFix = versionReq[^1];

#pragma warning disable CS8794 // 输入始终与提供的模式匹配。
            if (preFix is not ('[' and '(') && subFix is not (']' and ')'))
            {
                versionReq = $"[{versionReq},)";
            }
#pragma warning restore CS8794 // 输入始终与提供的模式匹配。
        }

        if (Dependencies.TryGetValue(modId, out var val))
        {
            if (string.IsNullOrEmpty(val))
            {
                val = versionReq;
            }
        }
        else
        {
            Dependencies.Add(modId, versionReq);
        }
    }
}