using System;
using System.Collections.Generic;

namespace YARG.Core.Containers
{
    /// <summary>
    /// Represents a sorted collection of keys and values, specialized for unmanaged types.
    /// </summary>
    /// <remarks>
    /// Unlike the built-in Dictionary container, the keys and values are stored local to eachother in a single array of data.
    /// Restricting the types with the <see langword="unmanaged"/> constraint allows the container to utilize
    /// native memory tricks to provide substantial performance benefits. <br></br>
    /// These tricks allow it dodge interacting with GC's heap management, providing pleasant locality characteristics through fixed memory and manual disposability.<br></br>
    /// As a side effect however, using this container requires familiarity with pointers, as most functions that give access to values within it do so through direct pointers.
    /// </remarks>
    /// <typeparam name="TKey">The type to use for determining sorting order</typeparam>
    /// <typeparam name="TValue">The value that gets mapped to keys</typeparam>
    public sealed class YargNativeSortedList<TKey, TValue> : YargNativeList<(TKey Key, TValue Value)>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        // This pattern of copying a pre-defined value is faster than default construction
        // Note: except possibly for types of 16 bytes or fewer, idk
        private static readonly TValue DEFAULT_VALUE = default;

        public YargNativeSortedList() { }

        public YargNativeSortedList(YargNativeSortedList<TKey, TValue> list)
            : base(list) { }

        /// <summary>
        /// Appends a new node with the given key - the value of the node being defaulted.
        /// </summary>
        /// <remarks>This does not do any checks with regards to ordering.</remarks>
        /// <param name="key">The key to assign to the new node</param>
        /// <returns>The pointer to the value from the newly created node</returns>
        public unsafe TValue* Add(in TKey key)
        {
            CheckAndGrow();
            var node = _buffer + (_count++);
            node->Key = key;
            node->Value = DEFAULT_VALUE;
            return &node->Value;
        }

        /// <summary>
        /// Appends a new node with the given key and value to the end of the list
        /// </summary>
        /// <remarks>This does not do any checks in regards to ordering.</remarks>
        /// <param name="key">The key to assign to the new node</param>
        /// <param name="value">The value to assign to the new node</param>
        public void Add(in TKey key, in TValue value)
        {
            CheckAndGrow();
            unsafe
            {
                ref var node = ref _buffer[_count++];
                node.Key = key;
                node.Value = value;
            }
        }

        /// <summary>
        /// Returns the value of the last node in the list.
        /// If the last node's key does not match, a new one is appended to the list with a defaulted value variable.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>The pointer to the last value in the list</returns>
        public unsafe TValue* GetLastOrAdd(in TKey key)
        {
            if (_count > 0)
            {
                var node = _buffer + _count - 1;
                if (node->Key.CompareTo(key) >= 0)
                {
                    return &node->Value;
                }
            }
            return Add(key);
        }

        /// <summary>
        /// Returns the value of the last node in the list, alongside whether said node was appended in the operation.
        /// A new node will be appended if the last key does not match the one provided.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <param name="value">The pointer to the last node in the list</param>
        /// <returns>Whether a node was appeneded to the list</returns>
        public unsafe bool GetLastOrAdd(in TKey key, out TValue* value)
        {
            if (_count > 0)
            {
                var node = _buffer + _count - 1;
                if (node->Key.CompareTo(key) >= 0)
                {
                    value = &node->Value;
                    return false;
                }
            }
            value = Add(key);
            return true;
        }

        /// <summary>
        /// If the last node of the list matches the key provided, that node's value will get
        /// replace by the value provided to the method.
        /// Otherwise, a node is appended to the list with said value.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <param name="value">The value to use as the last value of the list</param>
        public unsafe void AddOrUpdate(in TKey key, in TValue value)
        {
            if (_count == 0 || _buffer[_count - 1].Key.CompareTo(key) < 0)
            {
                CheckAndGrow();
                ++_count;
            }
            ref var node = ref _buffer[_count - 1];
            node.Key = key;
            node.Value = value;
        }

        /// <summary>
        /// Attempts to add a new node to the end of the list with the provided key.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>Whether a new node was created</returns>
        public unsafe bool TryAdd(in TKey key)
        {
            if (_count > 0 && _buffer[_count - 1].Key.CompareTo(key) >= 0)
            {
                return false;
            }

            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = DEFAULT_VALUE;
            return true;
        }

        /// <summary>
        /// Returns the index of the node that contains a key that matches the one provided.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to find</param>
        /// <param name="lo">The lowest point in the list to search from</param>
        /// <param name="hi">The exclusively highest point in the list to search from</param>
        /// <returns>The index of the node with the matching key. If one was not found, the index where it would go is returned, but bit-flipped.</returns>
        public long Find(in TKey key, long lo = 0, long hi = long.MaxValue)
        {
            if (hi > _count)
            {
                hi = _count;
            }
            hi--;

            while (lo <= hi)
            {
                long curr = lo + (hi - lo >> 1);
                int order;
                unsafe
                {
                    order = _buffer[curr].Key.CompareTo(key);
                }

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
        /// Returns the index of the node from the list that contains a key that matches the one provided.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <remarks>Undefined behavior will occur if the key and index are out of sync</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The index of the node with the matching key</returns>
        public long FindOrEmplaceIndex(in TKey key)
        {
            long index = Find(in key);
            if (index < 0)
            {
                index = ~index;
                Insert(index, (key,DEFAULT_VALUE));
            }
            return index;
        }

        /// <summary>
        /// Returns the index of the node from the list that contains a key that matches the one provided.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <remarks>Undefined behavior will occur if the key and index are out of sync</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The value with the matching key (emplaced if the key did nor exist)</returns>
        public unsafe TValue* FindOrEmplace(in TKey key)
        {
            return &_buffer[FindOrEmplaceIndex(in key)].Value;
        }

        /// <summary>
        /// Removes the node with the given key if one exists
        /// </summary>
        /// <param name="key">The key to query for</param>
        /// <returns>Whether a node was found and removed</returns>
        public bool Remove(in TKey key)
        {
            long index = Find(key);
            if (index < 0)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Returns a pointer to the value from the node with a matching key.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>A reference of the node with the matching key.</returns>
        /// <exception cref="KeyNotFoundException">A node with the provided key does not exist</exception>
        public unsafe TValue* GetValue(in TKey key)
        {
            long index = Find(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            return &_buffer[index].Value;
        }

        /// <summary>
        /// Returns the pointer to value from the node with the key that matches the one provided.
        /// </summary>
        /// <remarks>
        /// This function linearly searches from the end of the list backwards.
        /// Therefore, it should only be used when that behavior results in better performance over a binary search.<br></br>
        /// For OUR case, this is mainly restricted to midi files.<br></br><br></br>
        /// Note: this should only be used used if the key exists, but it will stop for anything equal to OR less than the key.
        /// </remarks>
        /// <param name="key">The key to linearly search for</param>
        /// <returns>The reference to the node with the matching key</returns>
        public unsafe TValue* TraverseBackwardsUntil(in TKey key)
        {
            long index = _count - 1;
            while (index > 0 && key.CompareTo(_buffer[index].Key) < 0)
            {
                --index;
            }
            return &_buffer[index].Value;
        }

        /// <summary>
        /// Returns whether the last node in the list contains a matching key.
        /// If true, the pointer to the value in the node will be provided.
        /// </summary>
        /// <param name="key">The key to linearly search for</param>
        /// <param name="value">The pointer to the last node, if the key matched</param>
        /// <returns>Whether the last key matched</returns>
        public unsafe bool TryGetLastValue(in TKey key, out TValue* value)
        {
            if (_count > 0)
            {
                var node = _buffer + _count - 1;
                if (node->Key.Equals(key))
                {
                    value = &node->Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
    }
}
