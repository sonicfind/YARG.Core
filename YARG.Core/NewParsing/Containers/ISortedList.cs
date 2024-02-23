using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public interface ISortedList<TKey, TValue> : IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        protected const int DEFAULT_CAPACITY = 16;

        public int Count { get; }
        public int Capacity { get; set; }

        public Span<YARGKeyValuePair<TKey, TValue>> Span { get; }

        public void Clear();

        public ref TValue Append(TKey key);

        public ref TValue Append(TKey key, TValue value);

        public ref TValue Append(TKey key, in TValue value);

        public ref TValue this[TKey key] => ref FindOrEmplaceValue(0, key);

        public int FindOrEmplaceIndex(int startIndex, TKey key);

        public ref TValue FindOrEmplaceValue(int startIndex, TKey key);

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, TKey key, in TValue value);

        public void Pop();

        public ref TValue At(TKey key);

        public ref YARGKeyValuePair<TKey, TValue> ElementAtIndex(int index);

        public ref TValue GetLastOrAppend(TKey key);

        public int Find(int startIndex, TKey key);

        public bool ValidateLastKey(TKey key);

        public ref TValue Last();

        public void Add(TKey key, TValue value)
        {
            if (!Try_Add(key, in value))
            {
                throw new ArgumentException($"A value of key of value {key} already exists");
            }
        }

        public void Add(TKey key, in TValue value)
        {
            if (!Try_Add(key, in value))
            {
                throw new ArgumentException($"A value of key of value {key} already exists");
            }
        }

        public bool Try_Add(TKey key, TValue value)
        {
            return Try_Add(key, in value);
        }

        public bool Try_Add(TKey key, in TValue value)
        {
            int index = Find(0, key);
            if (index >= 0)
            {
                return false;
            }

            index = ~index;
            Insert_Forced(index, key, in value);
            return true;
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, TKey key, TValue value)
        {
            Insert_Forced(index + 1, key, in value);
        }

        public bool TryGetValue(TKey key, out TValue? value);

        public bool ContainsKey(TKey key) { return ContainsKey(0, key); }

        public bool ContainsKey(int startIndex, TKey key) { return Find(startIndex, key) >= 0; }
    }
}
