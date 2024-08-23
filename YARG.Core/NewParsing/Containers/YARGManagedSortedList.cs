using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Represents a sorted collection of keys and values.
    /// </summary>
    /// <remarks>
    /// This container differs from the built-in Dictionary container in two important ways:<br></br>
    /// 1. The keys and values are stored local to eachother in a single array of data. While it loses key-to-key locality, key-to-value locality improves. <br></br>
    /// 2. Functions that provide access to elements within the container do so by reference instead of by value.
    /// This allows in-place modification of mapped values (<see langword="struct"/> types) through single access calls instead of get + set.
    /// </remarks>
    /// <typeparam name="TKey">The type to use for determining sorting order</typeparam>
    /// <typeparam name="TValue">The value that gets mapped to keys</typeparam>
    [DebuggerDisplay("Count: {_count}")]
    public sealed class YARGManagedSortedList<TKey, TValue> : IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : new()
    {
        private YARGKeyValuePair<TKey, TValue>[] _buffer = Array.Empty<YARGKeyValuePair<TKey, TValue>>();
        private int _count;
        private int _version;

        /// <summary>
        /// The number of elements within the list
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// The capacity of the list where elements will reside
        /// </summary>
        public int Capacity
        {
            get => _buffer.Length;
            set
            {
                if (_count <= value && value != _buffer.Length)
                {
                    Array.Resize(ref _buffer, value);
                    if (value > 0)
                    {
                        ++_version;
                    }
                    else
                    {
                        _version = 0;
                    }
                }
            }
        }

        /// <summary>
        /// The span view of the data up to <see cref="Count"/>
        /// </summary>
        public Span<YARGKeyValuePair<TKey, TValue>> Span => new(_buffer, 0, _count);

        /// <summary>
        /// The direct arrau for the underlying data. Use carefully.
        /// </summary>
        public YARGKeyValuePair<TKey, TValue>[] Data => _buffer;

        public YARGManagedSortedList() { }

        /// <summary>
        /// Transfers all the data to a new instance of the list, leaving the current one in its default state.
        /// </summary>
        /// <remarks>This is only to be used to dodge double-frees from any sort of conversions with readonly instances</remarks>
        public YARGManagedSortedList(YARGManagedSortedList<TKey, TValue> original)
        {
            _buffer = original._buffer;
            _count = original._count;
            _version = original._version;
            original._buffer = Array.Empty<YARGKeyValuePair<TKey, TValue>>();
            original._count = 0;
            original._version = 0;
        }

        /// <summary>
        /// Returns whether count is zero
        /// </summary>
        public bool IsEmpty()
        {
            return _count == 0;
        }

        /// <summary>
        /// Clears every node present in the list and sets count to zero
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; ++i)
            {
                _buffer[i] = default;
            }

            if (_count > 0)
            {
                _version++;
            }
            _count = 0;
        }

        /// <summary>
        /// Appends a new node with the given key - the value of the node being defaulted.
        /// </summary>
        /// <remarks>This does not do any checks in regards to ordering.</remarks>
        /// <param name="key">The key to assign to the new node</param>
        /// <returns>A reference to the value from the newly created node</returns>
        public ref TValue Append(TKey key)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = new TValue();
            return ref node.Value;
        }

        /// <summary>
        /// Appends a new node with the given key and value to the end of the list
        /// </summary>
        /// <remarks>This does not do any checks in regards to ordering.</remarks>
        /// <param name="key">The key to assign to the new node</param>
        /// <param name="value">The value to assign to the new node</param>
        public void Append(in TKey key, in TValue value)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = value;
        }

        /// <summary>
        /// Returns the value of the last node in the list.
        /// If the last node's key does not match, a new one is appended to the list with a defaulted value variable.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>A reference to the last value in the list</returns>
        public ref TValue GetLastOrAppend(in TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return ref Append(key);
            }
            return ref _buffer[_count - 1].Value;
        }

        /// <summary>
        /// If the last node of the list matches the key provided, that node's value will get
        /// replace by the value provided to the method.
        /// Otherwise, a node is appended to the list with said value.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <param name="value">The value to use as the last value of the list</param>
        public void AppendOrUpdate(in TKey key, in TValue value)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                CheckAndGrow();
                ++_count;
            }
            ref var node = ref _buffer [_count - 1];
            node.Key = key;
            node.Value = value;
        }

        /// <summary>
        /// Attempts to add a new node to the end of the list with the provided key.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>Whether a new node was created</returns>
        public bool TryAppend(in TKey key)
        {
            bool append = _count == 0 || !_buffer[_count - 1].Key.Equals(key);
            if (append)
            {
                Append(key);
            }
            return append;
        }

        /// <summary>
        /// Forcibly inserts a node with the provided key and value at the positional index.
        /// </summary>
        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        /// <param name="index">The position to place the node - an array offset.</param>
        /// <param name="key">The key to use for the node</param>
        /// <param name="value">The value to assign to the node</param>
        public void Insert_Forced(int index, in TKey key, in TValue value)
        {
            CheckAndGrow();
            if (index < _count)
            {
                Array.Copy(_buffer, index, _buffer, index + 1, _count - index);
            }
            ++_count;

            ref var node = ref _buffer[index];
            node.Key = key;
            node.Value = value;
        }

        /// <summary>
        /// Removes the node with the given key if one exists
        /// </summary>
        /// <param name="key">The key to query for</param>
        /// <returns>Whether a node was found and removed</returns>
        public bool Remove(in TKey key)
        {
            int index = Find(key);
            return RemoveAtIndex(index);
        }

        /// <summary>
        /// Removes the node present at the provided array offset index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        /// <returns>Whether the index was valid</returns>
        public bool RemoveAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                return false;
            }

            --_count;
            Array.Copy(_buffer, index + 1, _buffer, index, _count - index);
            return true;
        }

        /// <summary>
        /// Removes the last node in the list
        /// </summary>
        /// <exception cref="InvalidOperationException">The list has no elements to remove</exception>
        public void Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            --_count;
            ++_version;
            _buffer[_count] = default;
        }

        /// <summary>
        /// Returns the value from the node with a mathcing key.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The reference to the node with the matching key</returns>
        public ref TValue this[in TKey key]
        {
            get
            {
                int index = FindOrEmplaceIndex(in key);
                return ref _buffer[index].Value;
            }
        }

        /// <summary>
        /// Returns the index of the node from the list that contains a key that matches the one provided, starting from the provided index.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <remarks>Undefined behavior will occur if the key and index are out of sync</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The index of the node with the matching key</returns>
        public int FindOrEmplaceIndex(in TKey key)
        {
            int index = Find(key);
            if (index < 0)
            {
                index = ~index;
                Insert_Forced(index, key, new());
            }
            return index;
        }

        /// <summary>
        /// Returns the index of the node that contains a key that matches the one provided key, starting at the provided index.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <param name="startIndex">The starting index bound for the binary search that is performed</param>
        /// <returns>The index of the node with the matching key. If one was not found, it returns the index where it would go, but bit-flipped.</returns>
        public int Find(in TKey key)
        {
            int lo = 0;
            int hi = _count - 1;
            while (lo <= hi)
            {
                int curr = lo + ((hi - lo) >> 1);
                int order = _buffer[curr].Key.CompareTo(key);
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

        /// <summary>
        /// Returns a reference to the value from the node with a matching key.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>A reference of the node with the matching key.</returns>
        /// <exception cref="KeyNotFoundException">A node with the provided key does not exist</exception>
        public ref TValue At(in TKey key)
        {
            int index = Find(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            return ref _buffer[index].Value;
        }

        /// <summary>
        /// Returns a reference to value from the node with the key that matches the one provided.
        /// </summary>
        /// <remarks>
        /// This function linearly searches from the end of the list backwards.
        /// Therefore, it should only be used when that behavior results in better performance over a binary search.<br></br>
        /// For OUR case, this is mainly restricted to midi files.<br></br><br></br>
        /// Note: this should only be used used if the key exists, but it will stop for anything equal to OR less than the key.
        /// </remarks>
        /// <param name="key">The key to linearly search for</param>
        /// <returns>The reference to the node with the matching key</returns>
        public ref TValue TraverseBackwardsUntil(in TKey key)
        {
            int index = _count - 1;
            while (index > 0 && key.CompareTo(_buffer[index].Key) < 0)
            {
                --index;
            }
            return ref _buffer[index].Value;
        }

        /// <summary>
        /// Returns whether the last node in the list contains a matching key.
        /// If true, a reference to the value in the node will be provided.
        /// </summary>
        /// <param name="key">The key to linearly search for</param>
        /// <param name="value">A reference to the last node, if the key matched</param>
        /// <returns>Whether the last key matched</returns>
        public bool TryGetLastValue(in TKey key, out TValue? value)
        {
            if (_count == 0 || !_buffer[_count - 1].Key.Equals(key))
            {
                value = default;
                return false;
            }
            value = _buffer[_count - 1].Value;
            return true;
        }

        private void CheckAndGrow()
        {
            if (_count >= int.MaxValue)
            {
                throw new OverflowException("Element limit reached");
            }

            if (_count == _buffer.Length)
            {
                Grow();
            }
            ++_version;
        }

        private const int DEFAULT_CAPACITY = 16;
        private void Grow()
        {
            int newcapacity = _buffer.Length == 0 ?  DEFAULT_CAPACITY : 2 * _buffer.Length;
            if ((uint) newcapacity > int.MaxValue)
            {
                newcapacity = int.MaxValue;
            }
            Capacity = newcapacity;
        }

        IEnumerator<YARGKeyValuePair<TKey, TValue>> IEnumerable<YARGKeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<YARGKeyValuePair<TKey, TValue>>) this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<YARGKeyValuePair<TKey, TValue>>, IEnumerator
        {
            private readonly YARGManagedSortedList<TKey, TValue> _map;
            private int _index;
            private readonly int _version;

            internal Enumerator(YARGManagedSortedList<TKey, TValue> map)
            {
                _map = map;
                _index = -1;
                _version = map._version;
            }

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _map._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }

                ++_index;
                return _index < _map._count;
            }

            public readonly YARGKeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_version != _map._version || _index < 0 || _index >= _map._count)
                    {
                        throw new InvalidOperationException("Enum Operation not possible");
                    }
                    return _map._buffer[_index];
                }
            }

            readonly object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _map._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }

                _index = -1;
            }
        }
    }
}
