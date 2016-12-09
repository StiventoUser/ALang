using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class IEnumerableHelper
{
    public static bool CompareTo<T, T2>(this IList<T> collection1, IList<T2> collection2, Func<T, T2, bool> predicate)
    {
        if(collection1.Count != collection2.Count)
            return false;   

        for(int i = 0; i < collection1.Count; ++i)
        {
            if(!predicate(collection1[i], collection2[i]))
            {
                return false;
            }
        }
        return true;
    }
    public static void IndexForeach<T>(this IEnumerable<T> collection, Action<T, int> func)
    {
        int i = 0;
        foreach(var elem in collection)
        {
            func(elem, i);
            ++i;
        }
    }
    public static IEnumerable<T2> IndexSelect<T, T2>(this IEnumerable<T> collection, Func<T, int, T2> func)
    {
        List<T2> list = new List<T2>();
        int i = 0;
        foreach(var elem in collection)
        {
            list.Add(func(elem, i));
            ++i;
        }

        return list;
    }
    public static string MergeInString(this IList<string> collection, string delimiter = ", ")
    {
        if(collection.Count == 0)
            return "";

        StringBuilder builder = new StringBuilder();

        for(int i = 0, end = collection.Count - 1; i < end; ++i)
        {
            builder.Append(collection[i] ?? "[auto]").Append(delimiter);
        }
        builder.Append(collection[collection.Count-1]);

        return builder.ToString();
    }
    public static string MergeInString<T>(this IList<T> collection, Func<T, string> func, string delimiter = ", ")
    {
        if(collection.Count == 0)
            return "";

        StringBuilder builder = new StringBuilder();

        for(int i = 0, end = collection.Count - 1; i < end; ++i)
        {
            builder.Append(func(collection[i])).Append(delimiter);
        }
        builder.Append(collection[collection.Count-1]);

        return builder.ToString();
    }
}