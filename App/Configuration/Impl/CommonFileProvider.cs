namespace PCL.Core.App.Configuration.Impl;

public abstract class CommonFileProvider(string path) : IKeyValueFileProvider
{
    public string FilePath { get; set; } = path;

    public abstract T Get<T>(string key);
    public abstract void Set<T>(string key, T value);
    public abstract bool Exists(string key);
    public abstract void Remove(string key);
    public abstract void Sync();
}
