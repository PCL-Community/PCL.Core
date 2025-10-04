using System.Collections.Generic;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record ProjectStorage
{
    public int CurseForgeOffset { get; set; } = 0;
    public int CurseForgeTotal { get; set; } = -1;

    public int ModrinthOffset { get; set; } = 0;
    public int ModrinthTotal { get; set; } = -1;

    public List<ProjectInfo> Results { get; set; } = [];

    public string ErrorMessage { get; set; } = string.Empty;
}