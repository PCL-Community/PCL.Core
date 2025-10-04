using System;
using LiteDB;
using PCL.Core.App.Database;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Compoment.Exceptions;
using PCL.Core.Minecraft.Compoment.Projects.Entities;

namespace PCL.Core.Minecraft.Compoment.Cache;

public class ProjectCache : DatabaseEntry
{
    /// <inheritdoc />
    public ProjectCache() : base($"{FileCacheService.CachePath}\\ProjectCache.db")
    {
        _CachedProjects.EnsureIndex("projectIdIndex", fl => fl.ProjectInfo.Id);
        _CachedFiles.EnsureIndex("fileIdIndex", fl => fl.FileInfo.ProjectId);

        LogWrapper.Info("[ProjectCache] Finished loading.");
    }


    #region CachedFiles

    private ILiteCollection<CacheFileInfo>? _cachedFile;

    private ILiteCollection<CacheFileInfo> _CachedFiles =>
        _cachedFile ??= Db.GetCollection<CacheFileInfo>("projectFile");


    public void AddOrUpdateCachedFile(ProjectFileInfo info)
    {
        var cache = new CacheFileInfo(info.ProjectId, info, DateTime.Now);
        if (_CachedFiles.Exists(Query.EQ("FileId", info.ProjectId)))
        {
            _CachedFiles.Update(cache);
            return;
        }

        _CachedFiles.Insert(cache);
    }

    /// <exception cref="CacheResultNotFoundException">Throw if target value not found.</exception>
    public ProjectFileInfo GetCachedFile(string projectId)
    {
        var val = _GetCachedFile(projectId);

        if (val is null)
        {
            throw new CacheResultNotFoundException("projectFile", projectId);
        }

        return val;
    }

    public bool TryGetCachedFile(string projectId, out ProjectFileInfo? value)
    {
        var val = _GetCachedFile(projectId);

        value = val;

        if (val is null)
        {
            return false;
        }

        return true;
    }

    private ProjectFileInfo? _GetCachedFile(string projectId)
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

    public void AddOrUpdateCachedProject(ProjectFileInfo info)
    {
        var cache = new CacheFileInfo(info.ProjectId, info, DateTime.Now);
        if (_CachedFiles.Exists(Query.EQ("ProjectId", info.ProjectId)))
        {
            _CachedFiles.Update(cache);
            return;
        }

        _CachedFiles.Insert(cache);
    }

    private ILiteCollection<CacheProjectInfo>? _cachedProject;

    private ILiteCollection<CacheProjectInfo> _CachedProjects =>
        _cachedProject ??= Db.GetCollection<CacheProjectInfo>("projectFile");

    public bool TryGetCachedProject(string projectId, out ProjectInfo? value)
    {
        var val = _GetCachedProject(projectId);

        value = val;

        if (val is null)
        {
            return false;
        }

        return true;
    }

    /// <exception cref="CacheResultNotFoundException">Throw if target value not found.</exception>
    public ProjectFileInfo GetCachedProject(string projectId)
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

internal record CacheFileInfo(string PojectId, ProjectFileInfo FileInfo, DateTime InsertTime);