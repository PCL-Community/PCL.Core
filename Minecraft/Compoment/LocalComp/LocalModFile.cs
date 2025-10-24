using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.App;
using PCL.Core.Minecraft.Compoment.Cache;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Utils.Hash;

namespace PCL.Core.Minecraft.Compoment.LocalComp;

/// <inheritdoc />
public class LocalModFile(string path) : LocalResource(path)
{
    private static readonly CompFileHashCache _FileHashCache = new();

    public ModMetadata? Metadata { get; set; }

    public Dictionary<string, string?> Dependencies { get; } = new();

    public ProjectInfo Comp { get; set; } // TODO: imlp init

    public ProjectFileInfo CompFile { get; set; } // TODO: impl init

    public ProjectFileInfo UpdateFile { get; set; } // TODO: impl init

    public List<string> ChangelogUrls { get; set; } // TODO: impl init

    public bool CompLoaded { get; set; } = false; // TODO: impl init

    public bool CanUpdate => !Config.UI.Hide.FunctionModUpdate && ChangelogUrls.Count != 0;

    private uint? _curseForgeHash;

    public uint CurseForgeHash
    {
        get
        {
            if (_curseForgeHash is not null)
            {
                return _curseForgeHash.Value;
            }


            var cacheKey = $"{Path.GetHashCode()}-CurseForge";
            if (_FileHashCache.TryGet(cacheKey, out var entry))
            {
                var timeOut = entry.InsertTime + TimeSpan.FromHours(7);
                if (timeOut > DateTime.Now)
                {
                    var hash = uint.Parse(entry.Hash);
                    _curseForgeHash = hash;

                    return hash;
                }
            }

            if (!File.Exists(Path))
            {
                _curseForgeHash = 0;
                return 0;
            }

            var murmur2 = HashComputer.ComputeMurmur2(Path);
            _curseForgeHash = murmur2;
            _FileHashCache.AddOrUpdate(cacheKey, new FileHashCacheEntry(cacheKey, murmur2.ToString(), DateTime.Now));

            return murmur2;
        }
    }

    private string? _modrinthHash;

    public string ModrinthHash
    {
        get
        {
            if (!string.IsNullOrEmpty(_modrinthHash))
            {
                return _modrinthHash;
            }

            var cacheKey = $"{Path.GetHashCode()}-Modrinth";
            if (_FileHashCache.TryGet(cacheKey, out var entry))
            {
                var timeOut = entry.InsertTime + TimeSpan.FromHours(7);
                if (timeOut < DateTime.Now)
                {
                    _modrinthHash = entry.Hash;
                    return entry.Hash;
                }
            }

            if (!File.Exists(Path))
            {
                _modrinthHash = string.Empty;
                return string.Empty;
            }

            var sha1 = HashComputer.ComputeSha1(Path);
            _modrinthHash = sha1;
            _FileHashCache.AddOrUpdate(cacheKey, new FileHashCacheEntry(cacheKey, sha1, DateTime.Now));

            return sha1;
        }
    }

    private static readonly List<IModMetadataParser> _Parsers =
    [
        new LegacyForgeModParser(),
        new ForgeModParser(),
        new FabricModJsonParser(),
        new QuitModParser(),
        new PackPngParser()
    ];

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