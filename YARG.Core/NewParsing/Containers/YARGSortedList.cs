using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public abstract class YARGSortedList<TKey, TValue>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        protected const int DEFAULT_CAPACITY = 16;

        protected int _count;
        protected int _version;

        public int Count => _count;

        public abstract int Capacity { get; set; }

        public abstract Span<YARGKeyValuePair<TKey, TValue>> Span { get; }

        public bool IsEmpty()
        {
            return Count == 0;
        }

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

        public bool ContainsKey(TKey key) { return ContainsKey(0, key); }

        public bool ContainsKey(int startIndex, TKey key) { return Find(startIndex, key) >= 0; }

        public abstract void Clear();
        public abstract ref TValue Append(TKey key);
        public abstract ref TValue Append(TKey key, TValue value);
        public abstract ref TValue Append(TKey key, in TValue value);

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public abstract void Insert_Forced(int index, TKey key, in TValue value);
        public abstract void Pop();
        public ref TValue this[TKey key] => ref FindOrEmplaceValue(0, key);
        public abstract int FindOrEmplaceIndex(int startIndex, TKey key);
        public abstract ref TValue FindOrEmplaceValue(int startIndex, TKey key);
        public abstract ref TValue At(TKey key);
        public abstract bool TryGetValue(TKey key, out TValue? value);
        public abstract ref YARGKeyValuePair<TKey, TValue> ElementAtIndex(int index);
        public abstract ref TValue GetLastOrAppend(TKey key);
        public abstract int Find(int startIndex, TKey key);
        public abstract bool ValidateLastKey(TKey key);
        public abstract ref TValue Last();
    }
}
