using System;
using System.Collections.Generic;

namespace PCL.Core.Utils.Exts;

/// <summary>
/// 提供类型相关的扩展方法
/// </summary>
public static class TypeExtension
{
    /// <summary>
    /// 获取指定接口在当前程序集中的所有实现类型
    /// </summary>
    /// <param name="type">接口类型</param>
    /// <returns>实现该接口的所有类型列表</returns>
    /// <exception cref="ArgumentNullException">当type参数为null时抛出</exception>
    /// <exception cref="ArgumentException">当type参数不是接口类型时抛出</exception>
    public static List<Type> GetImplements(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsInterface)
        {
            throw new ArgumentException("类型必须是接口类型", nameof(type));
        }
        var types = type.Assembly.GetTypes();
        List<Type> result = [];
        
        // 遍历程序集中的所有类型，筛选出实现指定接口的类
        foreach (var item in types)
        {
            if (item.IsInterface)
            {
                continue;
            }

            if (item.GetInterface(type.Name) != null)
            {
                result.Add(item);
            }
        }
        return result;    
    }
}