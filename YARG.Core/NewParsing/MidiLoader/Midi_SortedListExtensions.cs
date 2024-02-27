using System;

namespace YARG.Core.NewParsing.Midi
{
    internal static class Midi_SortedListExtensions
    {
        internal static ref TValue TraverseBackwardsUntil<TKey, TValue>(this YARGManagedSortedList<TKey, TValue> list, TKey key)
            where TKey : IEquatable<TKey>, IComparable<TKey>
            where TValue : new()
        {
            int index = list.Count - 1;
            while (index > 0 && key.CompareTo(list.ElementAtIndex(index).Key) < 0)
            {
                --index;
            }
            return ref list.ElementAtIndex(index).Value;
        }

        internal static ref TValue Traverse_Backwards_Until<TKey, TValue>(this YARGNativeSortedList<TKey, TValue> list, TKey key)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            unsafe
            {
                var current = list.End - 1;
                while (current > list.Data && key.CompareTo(current->Key) < 0)
                    --current;
                return ref current->Value;
            }
        }
    }
}
