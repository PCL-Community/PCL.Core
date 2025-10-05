using PCL.Core.Minecraft.Compoment.Cache;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Net;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Compoment.Projects;

public static class ProjectGetter
{
    public static async Task<List<ProjectFileInfo>> GetByIdAsync(string projectId, bool fromCurseForge)
    {
        var (projectInfo, fileInfos) = await _GetProjectInfosAsync(projectId, fromCurseForge).ConfigureAwait(false);
        var deps = fileInfos.SelectMany(fi => fi.RawDependencies).Distinct().ToImmutableArray();
        var undonDeps = deps.Where(dep => !ProjectCache.Instance.TryGetCachedProject(dep, out _)).ToImmutableArray();
        var optionalDeps = fileInfos.SelectMany(fi => fi.RawOptionalDependencies).Distinct().ToImmutableArray();
        var undonOptDeps = optionalDeps.Where(dep => !ProjectCache.Instance.TryGetCachedProject(dep, out _))
            .ToImmutableArray();

        List<ProjectInfo> projects =
        [
            ..await _GetAllProjectsAsync(undonDeps, fromCurseForge).ConfigureAwait(false),
            ..await _GetAllProjectsAsync(undonOptDeps, fromCurseForge).ConfigureAwait(false)
        ];

        foreach (var proj in projects)
        {
            ProjectCache.Instance.AddOrUpdateCachedProject(proj.Id, projectInfo);
        }

        // 处理普通依赖项
        if (deps.Length != 0)
        {
            var depProjects = deps
                .Select(id =>
                    ProjectCache.Instance.TryGetCachedProject(id, out var cachedProject) ? cachedProject : null);

            foreach (var depProject in depProjects)
            {
                if (depProject is null)
                {
                    continue;
                }

                foreach (var file in fileInfos
                             .Where(file => file.RawDependencies.Contains(depProject.Id) && depProject.Id != projectId)
                             .Where(file => !file.Dependencies.Contains(depProject.Id)))
                {
                    file.Dependencies.Add(depProject.Id);
                }
            }
        }

        // 处理可选依赖项
        if (optionalDeps.Length != 0)
        {
            var optionalDepProjects = optionalDeps
                .Select(id =>
                    ProjectCache.Instance.TryGetCachedProject(id, out var cachedProject) ? cachedProject : null);

            foreach (var depProject in optionalDepProjects)
            {
                if (depProject is null)
                {
                    continue;
                }

                foreach (var file in fileInfos
                             .Where(file =>
                                 file.RawOptionalDependencies.Contains(depProject.Id) && depProject.Id != projectId)
                             .Where(file => !file.OptionalDependencies.Contains(depProject.Id)))
                {
                    file.OptionalDependencies.Add(depProject.Id);
                }
            }
        }

        return ProjectCache.Instance.GetCachedFile(projectId);
    }

    private static async Task<(ProjectInfo, List<ProjectFileInfo>)> _GetProjectInfosAsync
        (string projectId, bool fromCurseForge)
    {
        ProjectInfo targetProject;
        if (ProjectCache.Instance.TryGetCachedProject(projectId, out var info))
        {
            targetProject = info;
        }
        else
        {
            if (fromCurseForge)
            {
                var content = await ModApiMirrorSourceReq
                    .RequestAsync($"https://api.curseforge.com/v1/mods/{projectId}").ConfigureAwait(false);
                targetProject = ProjectFactory.Create(content);
            }
            else
            {
                var cotent = await ModApiMirrorSourceReq
                    .RequestAsync($"https://api.modrinth.com/v2/project/{projectId}").ConfigureAwait(false);
                targetProject = ProjectFactory.Create(cotent);
            }
        }

        List<ProjectFileInfo> files;
        if (ProjectCache.Instance.TryGetCachedFile(projectId, out var fileInfo))
        {
            files = fileInfo;
        }
        else
        {
            files = [];

            if (fromCurseForge)
            {
                var res = await ModApiMirrorSourceReq
                    .RequestAsync($"https://api.curseforge.com/v1/mods/{projectId}/files?pageSize=10000")
                    .ConfigureAwait(false);
                using var data = JsonDocument.Parse(res);
                var dataSeg = data.RootElement.GetProperty("data").EnumerateArray();
                files.AddRange(dataSeg.Select(seg => seg.GetRawText())
                    .Select(rawText => ProjectFileFactory.Create(rawText, targetProject.Type)));
            }
            else
            {
                var res = await ModApiMirrorSourceReq
                    .RequestAsync($"https://api.modrinth.com/v2/project/{projectId}/version")
                    .ConfigureAwait(false);


                using var data = JsonDocument.Parse(res);
                var dataSeg = data.RootElement.EnumerateArray();
                files.AddRange(dataSeg.Select(seg => seg.GetRawText())
                    .Select(rawText => ProjectFileFactory.Create(rawText, targetProject.Type)));
            }
        }

        var waitForCache = files.Where(fl => fl.Available).Distinct();
        ProjectCache.Instance.AddOrUpdateCachedFile(projectId, files);

        return (targetProject, files);
    }

    private static async Task<List<ProjectInfo>> _GetAllProjectsAsync(IReadOnlyList<string> deps, bool fromCurseForge)
    {
        if (deps.Count == 0)
        {
            return [];
        }

        List<ProjectInfo> projects = [];

        if (fromCurseForge)
        {
            projects.AddRange(await CompRequest.GetProjFromCurseForgeAsync(deps).ConfigureAwait(false));
        }
        else
        {
            projects.AddRange(await CompRequest.GetProjFromModrinthAsync(deps).ConfigureAwait(false));
        }


        return projects;
    }
}