using System;
using System.Diagnostics.CodeAnalysis;
using LiteDB;
using PCL.Core.App.Database;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Compoment.Exceptions;
using PCL.Core.Minecraft.Compoment.LocalComp;

namespace PCL.Core.Minecraft.Compoment.Cache;

public class LocalSchematicCache : DatabaseEntry, ICache<int, ModCacheEntry>
{
    /// <inheritdoc />
    public LocalSchematicCache() : base($"{FileCacheService.CachePath}\\LocalResourceCache.db")
    {
        _SchematicCache.EnsureIndex("hashKeyIndex", cache => cache.HashKey);
        LogWrapper.Info("[LocalModCache] Finished loading.");
    }

    private ILiteCollection<ModCacheEntry>? _schematicCache;

    private ILiteCollection<ModCacheEntry> _SchematicCache =>
        _schematicCache ??= Db.GetCollection<ModCacheEntry>("schematicCache");

    /// <inheritdoc />
    public void Add(ModCacheEntry entity)
    {
        if (_SchematicCache.Exists(Query.EQ("HashKey", entity.HashKey)))
        {
            return;
        }

        _SchematicCache.Insert(entity);
    }

    /// <inheritdoc />
    public void AddOrUpdate(ModCacheEntry entity)
    {
        if (_SchematicCache.Exists(Query.EQ("HashKey", entity.HashKey)))
        {
            _SchematicCache.Update(entity);
        }

        _SchematicCache.Insert(entity);
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
        var val = _SchematicCache.FindOne(Query.EQ("HashKey", hashKey));
        return val;
    }

    /// <inheritdoc />
    public void Set(int key, ModCacheEntry entity)
    {
        _SchematicCache.Update(entity);
    }

    /// <inheritdoc />
    public void SetOrAdd(int key, ModCacheEntry entity)
    {
        AddOrUpdate(entity);
    }

    /// <inheritdoc />
    public void Remove(int key)
    {
        _SchematicCache.Delete(new BsonValue(Query.EQ("HashKey", key)));
    }
}

public record SchematicCacheEntry(int HashKey, LocalSchematicFile File, DateTime InsertTime);