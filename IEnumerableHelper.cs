using System;
using System.Collections;
using System.Collections.Generic;

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
}