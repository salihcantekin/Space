using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Space.Abstraction.Extensions;

public static class DictionaryExtensions
{
    public static void AddOrUpdateList<TKey, TValue>(this ConcurrentDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
    {
        dict.AddOrUpdate(key,
            _ => [value],
            (_, list) =>
            {
                list.Add(value);
                return list;
            });
    }
}
