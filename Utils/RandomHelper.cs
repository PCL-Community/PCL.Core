using System;
using System.Collections.Generic;

namespace PCL.Core.Utils;

public static class RandomHelper
{
    private static readonly System.Random _Random = new();

    /// <summary>
    /// 随机选择其一。
    /// </summary>
    public static T RandomOne<T>(IList<T> objects)
        => objects[RandomInt(0, objects.Count - 1)];

    /// <summary>
    /// 取随机整数。
    /// </summary>
    public static int RandomInt(int min, int max) 
        => (int)Math.Floor((max - min + 1) * _Random.NextDouble());

    /// <summary>
    /// 将数组随机打乱。
    /// </summary>
    public static IList<T> Shuffle<T>(IList<T> array)
    {
        T[] result = [..array];
        _Random.Shuffle(result);
        return [..result];
    }
}

