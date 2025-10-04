using System;
using System.Diagnostics.CodeAnalysis;
using LiteDB;
using PCL.Core.App.Database;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Compoment.Exceptions;
using PCL.Core.Minecraft.Compoment.LocalComp;

namespace PCL.Core.Minecraft.Compoment.Cache;

public class LocalModCache : DatabaseEntry, ICache<int, ModCacheEntry>
{
    /// <inheritdoc />
    public LocalModCache() : base($"{FileCacheService.CachePath}\\LocalResourceCache.db")
    {
        _ModCache.EnsureIndex("hashKeyIndex", cache => cache.HashKey);
        LogWrapper.Info("[LocalModCache] Finished loading.");
    }

    private ILiteCollection<ModCacheEntry>? _modCache;

    private ILiteCollection<ModCacheEntry> _ModCache =>
        _modCache ??= Db.GetCollection<ModCacheEntry>("modCache");

    /// <inheritdoc />
    public void Add(ModCacheEntry entity)
    {
        if (_ModCache.Exists(Query.EQ("HashKey", entity.HashKey)))
        {
            return;
        }

        _ModCache.Insert(entity);
    }

    /// <inheritdoc />
    public void AddOrUpdate(ModCacheEntry entity)
    {
        if (_ModCache.Exists(Query.EQ("HashKey", entity.HashKey)))
        {
            _ModCache.Update(entity);
        }

        _ModCache.Insert(entity);
    }

    /// <inheritdoc />
    public void Add(int key, ModCacheEntry entity)
    {
        Add(entity);
    }

    /// <inheritdoc />
    public void AddOrUpdate(int key, ModCacheEntry entity)
    {
        AddOrUpdate(entity);
    }

    /// <inheritdoc />
    /// <exception cref="CacheResultNotFoundException">Throw if not found.</exception>
    public ModCacheEntry Get(int hashKey)
    {
        var val = _Get(hashKey);
        if (val is null)
        {
            throw new CacheResultNotFoundException("HashKey", hashKey.ToString());
        }

        return val;
    }

    /// <inheritdoc />
    public bool TryGet(int hashKey, [NotNullWhen(true)] out ModCacheEntry? entity)
    {
        var val = _Get(hashKey);

        if (val is null)
        {
            entity = null;
            return false;
        }

        entity = val;
        return true;
    }

    private ModCacheEntry? _Get(int hashKey)
    {
        var val = _ModCache.FindOne(Query.EQ("HashKey", hashKey));
        return val;
    }

    /// <inheritdoc />
    public void Set(int key, ModCacheEntry entity)
    {
        _ModCache.Update(entity);
    }

    /// <inheritdoc />
    public void SetOrAdd(int key, ModCacheEntry entity)
    {
        AddOrUpdate(entity);
    }

    /// <inheritdoc />
    public void Remove(int key)
    {
        _ModCache.Delete(new BsonValue(Query.EQ("HashKey", key)));
    }
}

public record ModCacheEntry(int HashKey, LocalModFile File, DateTime InsertTime);