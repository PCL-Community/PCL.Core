using System;
using System.Diagnostics.CodeAnalysis;
using LiteDB;
using PCL.Core.App.Database;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Compoment.Exceptions;

namespace PCL.Core.Minecraft.Compoment.Cache;

public class CompFileHashCache : DatabaseEntry, ICache<string, FileHashCacheEntry>
{
    /// <inheritdoc />
    public CompFileHashCache() : base($"{FileCacheService.CachePath}\\CompFileHashCache.db")
    {
        _CachedHashs.EnsureIndex("keyIndex", entry => entry.Key);
        LogWrapper.Info("[CompFileHashCache] Finished loading.");
    }


    private ILiteCollection<FileHashCacheEntry>? _cachedHashs;

    private ILiteCollection<FileHashCacheEntry> _CachedHashs =>
        _cachedHashs ??= Db.GetCollection<FileHashCacheEntry>("hashs");

    /// <inheritdoc />
    public void Add(FileHashCacheEntry entity)
    {
        if (_CachedHashs.Exists(Query.EQ("Key", entity.Key)))
        {
            return;
        }

        _CachedHashs.Insert(entity);
    }

    /// <inheritdoc />
    public void AddOrUpdate(FileHashCacheEntry entity)
    {
        if (_CachedHashs.Exists(Query.EQ("Key", entity.Key)))
        {
            _CachedHashs.Update(entity);
        }

        _CachedHashs.Insert(entity);
    }

    /// <inheritdoc />
    public void Add(string key, FileHashCacheEntry entity)
    {
        Add(entity);
    }

    /// <inheritdoc />
    public void AddOrUpdate(string key, FileHashCacheEntry entity)
    {
        AddOrUpdate(entity);
    }

    /// <inheritdoc />
    /// <exception cref="CacheResultNotFoundException">Throw if not found.</exception>
    public FileHashCacheEntry Get(string key)
    {
        var val = _Get(key);
        if (val is null)
        {
            throw new CacheResultNotFoundException("hashs", key);
        }

        return val;
    }

    /// <inheritdoc />
    public bool TryGet(string key, [NotNullWhen(true)] out FileHashCacheEntry? entity)
    {
        var val = _Get(key);

        if (val is null)
        {
            entity = null;
            return false;
        }

        entity = val;
        return true;
    }

    private FileHashCacheEntry? _Get(string key)
    {
        var val = _CachedHashs.FindOne(Query.EQ("Key", key));
        return val;
    }

    /// <inheritdoc />
    public void Set(string key, FileHashCacheEntry entity)
    {
        _CachedHashs.Update(entity);
    }

    /// <inheritdoc />
    public void SetOrAdd(string key, FileHashCacheEntry entity)
    {
        AddOrUpdate(entity);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _CachedHashs.Delete(new BsonValue(Query.EQ("Key", key)));
    }
}

public record FileHashCacheEntry(string Key, string Hash, DateTime InsertTime);