using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record class CurseforgeProjectResponse(CurseforgeProject Data);

[Serializable]
public record class CurseforgeProjectsResponse(List<CurseforgeProject> Data);
