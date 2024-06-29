using System;
using System.Collections.Generic;

namespace HeroesProfile.Uploader.Extensions;

public static class EnumerableExtensions
{
    public static void Do<T>(this IEnumerable<T> items, Action<T> action)
    {
        foreach(var item in items)
        {
            action(item);
        }
    }
}
