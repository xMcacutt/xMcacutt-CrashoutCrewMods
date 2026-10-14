using System;
using System.Collections.Generic;
using System.Linq;

namespace CrashoutCrew_APClient;

public static class Utility
{
    public static T? Random<T>(this IEnumerable<T> collection, int rangeStart = 0, int rangeEnd = -1, Random? random = null)
    {
        var enumerable = collection as T?[] ?? collection.ToArray();
        return enumerable.Length == 0 ? default : enumerable[random?.Next(0, enumerable.Length - 1) ?? UnityEngine.Random.Range(rangeStart, rangeEnd == -1 ? enumerable.Length : rangeEnd)];
    }
    
    public static T? Random<T>(this Array collection, int rangeStart = 0, int rangeEnd = -1, Random? random = null)
    {
        return collection.Length == 0 ? default : (T)collection.GetValue(random?.Next(0, collection.Length - 1) ?? UnityEngine.Random.Range(rangeStart, rangeEnd == -1 ? collection.Length : rangeEnd));
    }
}