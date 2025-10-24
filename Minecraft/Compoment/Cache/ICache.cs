namespace PCL.Core.Minecraft.Compoment.Cache;

public interface ICache<in TKey, TEntity>
{
    void Add(TEntity entity);
    void AddOrUpdate(TEntity entity);
    void Add(TKey key, TEntity entity);
    void AddOrUpdate(TKey key, TEntity entity);
    TEntity Get(TKey key);
    bool TryGet(TKey s, out TEntity? fileHashCacheEntry);
    void Set(TKey key, TEntity entity);
    void SetOrAdd(TKey key, TEntity entity);
    void Remove(TKey key);
}