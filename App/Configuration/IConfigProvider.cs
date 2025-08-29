namespace PCL.Core.App.Configuration;

public interface IConfigProvider
{
    public T GetValue<T>(string key, object? argument = null);
    public void SetValue<T>(string key, T? value, object? argument = null);
}
