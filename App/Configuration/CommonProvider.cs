using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration;

public abstract class CommonProvider : IConfigProvider
{
    protected abstract bool GetValue<T>(string key, out T? value);
    public bool GetValue<T>(string key, [NotNullWhen(true)] out T? value, object? argument) => GetValue(key, out value);
    
    protected abstract void SetValue<T>(string key, T? value);
    public void SetValue<T>(string key, T? value, object? argument) => SetValue(key, value);
    
    protected abstract void Reset(string key);
    public void Delete(string key, object? argument) => Reset(key);
    
    protected abstract bool Exists(string key);
    public bool Exists(string key, object? argument) => Exists(key);
}
