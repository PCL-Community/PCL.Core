namespace PCL.Core.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 提供集合排序的实用方法。
/// </summary>
public static class SortUtils {
    /// <summary>
    /// 对列表进行稳定排序，返回新列表。
    /// </summary>
    /// <typeparam name="T">列表元素类型。</typeparam>
    /// <param name="list">要排序的列表。</param>
    /// <param name="comparison">比较器，接收两个对象，若第一个对象应排在前面，则返回 true。</param>
    /// <returns>排序后的新列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="list"/> 或 <paramref name="comparison"/> 为 null 时抛出。</exception>
    public static List<T> Sort<T>(this IList<T> list, Func<T, T, bool> comparison) {
        // 使用 LINQ OrderBy 实现稳定排序
        var result = list
            .Select((item, index) => new { Item = item, Index = index }) // 使用匿名类型保留索引
            .OrderBy(x => x.Item, new StableComparer<T>(comparison)) // 显式指定比较器
            .Select(x => x.Item)
            .ToList();
        
        return result;
    }

    // 实现稳定比较器
    private class StableComparer<T>(Func<T, T, bool> comparison) : IComparer<T> {
        private readonly Func<T, T, bool> _comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        
        public int Compare(T? x, T? y) {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var xComesFirst = _comparison(x, y);
            var yComesFirst = _comparison(y, x);

            if (!xComesFirst && !yComesFirst) // 相等时返回 0，保持稳定
                return 0;
            return xComesFirst ? -1 : 1; // x 在前返回 -1，否则返回 1
        }
    }
}
