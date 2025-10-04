using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record ProjectSearchRequest(ProjectStorage Storage, int ResultCount, CompType Type)
{
    public CompType Type { get; set; }
    public string Tag { get; set; } = string.Empty;
    public LoaderType ModLoader { get; set; } = LoaderType.Any;
    public string GameVersion { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public SourceType Source { get; set; } = SourceType.Any;
    public SortType Sort { get; set; } = SortType.Default;

    public bool Cancontinue
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
}