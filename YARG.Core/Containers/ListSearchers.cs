using System;

namespace YARG.Core.Containers
{
    public static class ListSearchers
    {
        public static int Find<T, U>(this YargManagedList<T> list, U value, int lo = 0, int hi = int.MaxValue)
            where T :  IComparable<U>, new()
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > list.Count)
            {
                hi = list.Count;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = list[curr].CompareTo(value);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return ~lo;
        }

        public static int Find<T, U>(this YargNativeList<T> list, U value, int lo = 0, int hi = int.MaxValue)
            where T :  unmanaged, IComparable<U>
            where U :  unmanaged
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > list.Count)
            {
                hi = list.Count;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = list[curr].CompareTo(value);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return ~lo;
        }

        public static int Find<TKey, TValue, U>(this YargManagedSortedList<TKey, TValue> list, U value, int lo = 0, int hi = int.MaxValue)
            where TKey : IEquatable<TKey>, IComparable<TKey>, IComparable<U>, new()
            where TValue : new()
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > list.Count)
            {
                hi = list.Count;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = list[curr].Key.CompareTo(value);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return ~lo;
        }

        public static int Find<TKey, TValue, U>(this YargNativeSortedList<TKey, TValue> list, U value, int lo = 0, int hi = int.MaxValue)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>, IComparable<U>
            where TValue : unmanaged
            where U :  unmanaged
        {
            if (lo < 0)
            {
                lo = 0;
            }

            if (hi > list.Count)
            {
                hi = list.Count;
            }
            hi--;

            while (lo <= hi)
            {
                int curr = (hi + lo) >> 1;
                int order = list[curr].Key.CompareTo(value);
                if (order == 0)
                {
                    return curr;
                }

                if (order < 0)
                {
                    lo = curr + 1;
                }
                else
                {
                    hi = curr - 1;
                }
            }
            return ~lo;
        }
    }
}