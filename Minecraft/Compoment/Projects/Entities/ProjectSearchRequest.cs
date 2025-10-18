using System;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record ProjectSearchRequest(ProjectStorage Storage, int ResultCount, CompType Type)
{
    public CompType Type { get; init; }
    public string Tag { get; init; } = string.Empty;
    public LoaderType ModLoader { get; init; } = LoaderType.Any;
    public string GameVersion { get; init; } = string.Empty;
    public string SearchText { get; init; } = string.Empty;
    public SourceType Source { get; init; } = SourceType.Any;
    public SortType Sort { get; init; } = SortType.Default;

    public bool IsCanContinue
    {
        get
        {
            if (Tag.StartsWith('/') || !Source.HasFlag(SourceType.CurseForge))
            {
                Storage.CurseForgeTotal = 0;
            }

            if (Tag.EndsWith('/') || !Source.HasFlag(SourceType.Modrinth))
            {
                Storage.ModrinthTotal = 0;
            }

            if (Storage.CurseForgeTotal == -1 || Storage.ModrinthTotal == -1)
            {
                return true;
            }

            return Storage.CurseForgeOffset < Storage.CurseForgeTotal ||
                   Storage.ModrinthOffset < Storage.ModrinthTotal;
        }
    }

    /// <inheritdoc />
    public virtual bool Equals(ProjectSearchRequest? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && string.Equals(Tag, other.Tag, StringComparison.CurrentCultureIgnoreCase) &&
               ModLoader == other.ModLoader &&
               string.Equals(GameVersion, other.GameVersion, StringComparison.CurrentCultureIgnoreCase) &&
               string.Equals(SearchText, other.SearchText, StringComparison.CurrentCultureIgnoreCase) &&
               Source == other.Source &&
               Sort == other.Sort &&
               ResultCount == other.ResultCount;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Type;
            hashCode = (hashCode * 397) ^ StringComparer.CurrentCultureIgnoreCase.GetHashCode(Tag);
            hashCode = (hashCode * 397) ^ (int)ModLoader;
            hashCode = (hashCode * 397) ^ StringComparer.CurrentCultureIgnoreCase.GetHashCode(GameVersion);
            hashCode = (hashCode * 397) ^ StringComparer.CurrentCultureIgnoreCase.GetHashCode(SearchText);
            hashCode = (hashCode * 397) ^ (int)Source;
            hashCode = (hashCode * 397) ^ (int)Sort;
            hashCode = (hashCode * 397) ^ ResultCount;
            return hashCode;
        }
    }
}