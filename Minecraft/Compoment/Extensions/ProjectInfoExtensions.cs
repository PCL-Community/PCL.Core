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

    public static bool IsLike(this ProjectInfo fir, ProjectInfo sec)
    {
        if (fir.Id.Equals(sec.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fir.FromCurseForge == sec.FromCurseForge) return false;

        if (fir.ModLoaders.Count != sec.ModLoaders.Count || fir.ModLoaders.Except(sec.ModLoaders).Any())
        {
            return false;
        }

        if (fir.Type != CompType.Shader &&
            (fir.GameVersions.Count != sec.GameVersions.Count ||
             fir.GameVersions.Except(sec.GameVersions).Any()))
        {
            return false;
        }

        if (fir.LastUpdate.HasValue && sec.LastUpdate.HasValue &&
            Math.Abs((fir.LastUpdate.Value - sec.LastUpdate.Value).TotalDays) > 7)
        {
            return false;
        }

        // NOTE: not have DatabaseEntry, bacause Database can not move to Core
        //string GetRaw(string data) => string.Concat(data.Where(char.IsLetterOrDigit)).ToLower();
        //if (TranslateName == otherProject.TranslateName ||
        //    RawName == otherProject.RawName ||
        //    Description == otherProject.Description ||
        //    GetRaw(Slug) == GetRaw(otherProject.Slug))
        //{
        //    // Log.Information($"[Comp] 将 {RawName} ({Slug}) 与 {otherProject.RawName} ({otherProject.Slug}) 认定为相似工程");

        //    if (DatabaseEntry is null && otherProject.DatabaseEntry is not null)
        //    {
        //        DatabaseEntry = otherProject.DatabaseEntry;
        //    }
        //    if (DatabaseEntry is not null && otherProject.DatabaseEntry is null)
        //    {
        //        otherProject.DatabaseEntry = DatabaseEntry;
        //    }

        //    return true;
        //}

        return false;
    }
}