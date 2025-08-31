using System.Collections.Generic;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 配置作用域。
/// </summary>
public interface IConfigScope
{
    /// <summary>
    /// 检查指定的多个配置项是否在该作用域中。
    /// </summary>
    /// <param name="keys">配置键</param>
    /// <returns>所有存在于该作用域中的键的集合</returns>
    public IEnumerable<string> CheckScope(IReadOnlySet<string> keys);
}
