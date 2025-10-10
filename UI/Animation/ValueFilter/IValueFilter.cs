namespace PCL.Core.UI.Animation.ValueFilter;

/// <summary>
/// 数值过滤器。
/// </summary>
public interface IValueFilter<T>
{
    /// <summary>
    /// 过滤值。
    /// </summary>
    /// <param name="value">值。</param>
    /// <returns>返回过滤后的值。</returns>
    T Filter(T value);
}