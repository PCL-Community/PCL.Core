using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteDB;
using PCL.Core.App.Database;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Compoment.Exceptions;
using PCL.Core.Minecraft.Compoment.Projects.Entities;

namespace PCL.Core.Minecraft.Compoment.Cache;

public class ProjectCache : DatabaseEntry
{
    public static readonly ProjectCache Instance = new();

    /// <inheritdoc />
    public ProjectCache() : base($"{FileCacheService.CachePath}\\ProjectCache.db")
    {
        _CachedProjects.EnsureIndex("projectIdIndex", fl => fl.ProjectId);
        _CachedFiles.EnsureIndex("projectIdIndex", fl => fl.PojectId);

        LogWrapper.Info("[ProjectCache] Finished loading.");
    }


    #region CachedFiles

    private ILiteCollection<CacheFileInfo>? _cachedFile;

    private ILiteCollection<CacheFileInfo> _CachedFiles =>
        _cachedFile ??= Db.GetCollection<CacheFileInfo>("projectFile");


    public void AddOrUpdateCachedFile(ProjectFileInfo info)
    {
        var cache = new CacheFileInfo(info.ProjectId, [info], DateTime.Now);
        if (_CachedFiles.Exists(Query.EQ("ProjectId", info.ProjectId)))
        {
            _CachedFiles.Update(cache);
            return;
        }

        _CachedFiles.Insert(cache);
    }

    public void AddOrUpdateCachedFile(string projectId, List<ProjectFileInfo>? info)
    {
        var cache = new CacheFileInfo(projectId, info, DateTime.Now);
        if (_CachedFiles.Exists(Query.EQ("ProjectId", projectId)))
        {
            _CachedFiles.Update(cache);
            return;
        }

        _CachedFiles.Insert(cache);
    }

    /// <exception cref="CacheResultNotFoundException">Throw if target value not found.</exception>
    public List<ProjectFileInfo> GetCachedFile(string projectId)
    {
        var val = _GetCachedFile(projectId);

        if (val is null)
        {
            throw new CacheResultNotFoundException("projectFile", projectId);
        }

        return val;
    }

    public bool TryGetCachedFile(string projectId, [NotNullWhen(true)] out List<ProjectFileInfo>? value)
    {
        var val = _GetCachedFile(projectId);


        if (val is null)
        {
            value = null;
            return false;
        }

        value = val;
        return true;
    }

    private List<ProjectFileInfo>? _GetCachedFile(string projectId)
    {
        var val = _CachedFiles.FindOne(Query.EQ("ProjectId", projectId));
        if (val is null)
        {
            return null;
        }

        var timeOut = val.InsertTime + TimeSpan.FromHours(7);
        if (timeOut < DateTime.Now)
        {
            return null;
        }

        return val.FileInfo;
    }

    public void RemoveCachedFile(string projectId)
    {
        _CachedFiles.Delete(new BsonValue(Query.EQ("ProjectId", projectId)));
    }

    #endregion

    #region ProjectCache

    public void AddOrUpdateCachedProject(string projectId, ProjectInfo info)
    {
        var cache = new CacheProjectInfo(projectId, info, DateTime.Now);
        if (_CachedProjects.Exists(Query.EQ("ProjectId", projectId)))
        {
            _CachedProjects.Update(cache);
            return;
        }

        _CachedProjects.Insert(cache);
    }

    private ILiteCollection<CacheProjectInfo>? _cachedProject;

    private ILiteCollection<CacheProjectInfo> _CachedProjects =>
        _cachedProject ??= Db.GetCollection<CacheProjectInfo>("projectFile");

    public bool TryGetCachedProject(string projectId, [NotNullWhen(true)] out ProjectInfo? value)
    {
        var val = _GetCachedProject(projectId);


        if (val is null)
        {
            value = null;
            return false;
        }

        value = val;
        return true;
    }

    /// <exception cref="CacheResultNotFoundException">Throw if target value not found.</exception>
    public List<ProjectFileInfo> GetCachedProject(string projectId)
    {
        var val = _GetCachedFile(projectId);

        if (val is null)
        {
            throw new CacheResultNotFoundException("project", projectId);
        }

        return val;
    }

    private ProjectInfo? _GetCachedProject(string projectId)
    {
        var val = _CachedProjects.FindOne(Query.EQ("ProjectId", projectId));
        if (val is null)
        {
            return null;
        }

        var timeOut = val.InsertTime + TimeSpan.FromHours(7);
        if (timeOut < DateTime.Now)
        {
            return null;
        }

        return val.ProjectInfo;
    }

    public void RemoveCachedProject(string projectId)
    {
        _CachedProjects.Delete(new BsonValue(Query.EQ("ProjectId", projectId)));
    }

    #endregion
}

internal record CacheProjectInfo(string ProjectId, ProjectInfo ProjectInfo, DateTime InsertTime);

internal record CacheFileInfo(string PojectId, List<ProjectFileInfo>? FileInfo, DateTime InsertTime);