using System;
using System.Linq;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Extensions;

public static class ProjectInfoExtensions
{
    public static bool IsSameProject(this ProjectInfo fir, ProjectInfo sec)
    {
        if (fir.Equals(sec))
        {
            return true;
        }

        if (fir.FromCurseForge && sec.FromCurseForge)
        {
            return false;
        }

        if (fir.ModLoaders.Count != sec.ModLoaders.Count)
        {
            return false;
        }
        else
        {
            var res = fir.ModLoaders.Except(sec.ModLoaders);
            if (res.Any())
            {
                return false;
            }
        }

        if (fir.Type is not CompType.Shader && sec.Type is not CompType.Shader)
        {
            if (fir.GameVersions.Count != sec.GameVersions.Count)
            {
                return false;
            }
            else
            {
                var res = fir.GameVersions.Except(sec.GameVersions);
                if (res.Any())
                {
                    return false;
                }
            }
        }

        var subTime = (fir.LastUpdate - sec.LastUpdate) ?? TimeSpan.Zero;
        if (Math.Abs(subTime.Days) > 7)
        {
            return false;
        }

        // TODO: impl: translateName RawName Description Slug similarity check

        return true;
    }
}