using System;
using System.Collections.Generic;

namespace PCL.Core.Utils;

/// <summary>
/// 提供获取实现指定接口的类型的相关辅助方法
/// </summary>
public static class ImplementedUtils
{
    /// <summary>
    /// 获取程序集中实现指定接口的所有类型
    /// </summary>
    /// <typeparam name="InterfaceType">要查找其实现的接口类型</typeparam>
    /// <returns>返回实现指定接口的类型列表</returns>
    public static List<Type> GetImplementTypes<InterfaceType>()
    {
        var assembly = typeof(InterfaceType).Assembly;
        var types = assembly.GetTypes();
        List<Type> result = [];
        
        // 遍历程序集中的所有类型，筛选出实现指定接口的类
        foreach (var type in types)
        {
            if (type.IsInterface)
            {
                continue;
            }

            if (type.GetInterface(typeof(InterfaceType).Name) != null)
            {
                result.Add(type);
            }
        }
        return result;
    }
}