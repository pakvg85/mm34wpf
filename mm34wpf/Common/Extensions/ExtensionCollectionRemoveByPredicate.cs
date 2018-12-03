using System;
using System.Collections.Generic;
using System.Linq;

namespace mm34wpf
{
    public static class ExtensionCollectionRemoveByPredicate
    {
        public static void RemoveByPredicate<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            T element;

            for (int i = 0; i < collection.Count; i++)
            {
                element = collection.ElementAt(i);
                if (predicate(element))
                {
                    collection.Remove(element);
                    i--;
                }
            }
        }
    }
}