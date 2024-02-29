using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace YARG.Core.NewParsing
{
    [DebuggerDisplay("{Key} | {Value.ToString()}")]
    public struct YARGKeyValuePair<TKey, TValue> : IComparable<TKey>, IEquatable<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        public TKey Key;
        public TValue Value;

        public int CompareTo(TKey key)
        {
            return Key.CompareTo(key);
        }

        public bool Equals(TKey key)
        {
            return Key.Equals(key);
        }

        public static bool operator <(YARGKeyValuePair<TKey, TValue> node, TKey key) { return node.Key.CompareTo(key) < 0; }
        public static bool operator >(YARGKeyValuePair<TKey, TValue> node, TKey key) { return node.Key.CompareTo(key) > 0; }
    }
}
