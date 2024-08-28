using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace YARG.Core.Containers
{
    [DebuggerDisplay("{Key} = {Value}")]
    public struct YARGKeyValuePair<TKey, TValue>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        public TKey Key;
        public TValue Value;

        public static bool operator <(YARGKeyValuePair<TKey, TValue> node, TKey key) { return node.Key.CompareTo(key) < 0; }
        public static bool operator >(YARGKeyValuePair<TKey, TValue> node, TKey key) { return node.Key.CompareTo(key) > 0; }
    }
}
