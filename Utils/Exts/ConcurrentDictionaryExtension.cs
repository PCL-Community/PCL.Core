using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PCL.Core.Utils.Exts;

public static class ConcurrentDictionaryExtension
{
    public static bool CompareAndRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key,
        TValue comparison) where TKey: notnull
        => ((ICollection<KeyValuePair<TKey, TValue>>)dict).Remove(new KeyValuePair<TKey, TValue>(key, comparison));

    public static TValue? UpdateAndGetPrevious<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dict,
        TKey key,
        TValue value) where TKey: notnull
    {
        TValue? prevValue = default;
        dict.AddOrUpdate(key, _ =>
        {
            prevValue = default;
            return value;
        }, (_, existingValue) =>
        {
            prevValue = existingValue;
            return value;
        });
        return prevValue;
    }
}