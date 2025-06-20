namespace PCL.Core.Helper.Configure;
public interface IConfigure
{
    /// <summary>
    /// 设置配置项
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public void Set(string key, object value);
    /// <summary>
    /// 获取指定配置项
    /// </summary>
    /// <typeparam name="TValue">配置项的类型</typeparam>
    /// <param name="key">键名</param>
    /// <returns></returns>
    public TValue? Get<TValue>(string key);
    /// <summary>
    /// 是否包含指定配置项
    /// </summary>
    /// <param name="key">键值</param>
    /// <returns></returns>
    public bool Contains(string key);
    /// <summary>
    /// 移除配置项
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key);
    /// <summary>
    /// 清空配置项
    /// </summary>
    public void Clear();
    /// <summary>
    /// 立即向文件中写入配置项
    /// </summary>
    public void Flush();
    /// <summary>
    /// 重新从文件中获取内容
    /// </summary>
    public void Reload();
}
