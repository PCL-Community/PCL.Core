using System;
using System.Collections.Generic;
using PCL.Core.Minecraft.Compoment.Projects.Entities;

namespace PCL.Core.Minecraft.Compoment;

public static class CompRequest
{
    public static bool IsFromCurseForge(string id)
        => int.TryParse(id, out _);

    public static List<ProjectInfo> GetProjectsById()
    {
        throw new NotImplementedException();
    }
}