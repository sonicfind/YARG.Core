using System;
using System.Collections.Generic;

namespace YARG.Core.Containers
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
    public sealed class YARGManagedSortedList<TKey, TValue> : YARGManagedList<(TKey Key, TValue Value)>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : new()
    {
        public YARGManagedSortedList() { }

        public YARGManagedSortedList(YARGManagedSortedList<TKey, TValue> list)
            : base(list) { }

        /// <summary>
        /// Appends a new node with the given key - the value of the node being defaulted.
        /// </summary>
        /// <remarks>This does not do any checks in regards to ordering.</remarks>
        /// <param name="key">The key to assign to the new node</param>
        /// <returns>A reference to the value from the newly created node</returns>
        public ref TValue Add(TKey key)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = new();
            return ref node.Value;
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
        public ref TValue GetLastOrAdd(in TKey key)
        {
            if (_count > 0)
            {
                ref var node = ref _buffer[_count - 1];
                if (node.Key.CompareTo(key) >= 0)
                {
                    return ref node.Value;
                }
            }
            return ref Add(key);
        }

        /// <summary>
        /// If the last node of the list matches the key provided, that node's value will get
        /// replace by the value provided to the method.
        /// Otherwise, a node is appended to the list with said value.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <param name="value">The value to use as the last value of the list</param>
        public void AddOrUpdate(in TKey key, in TValue value)
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
        public bool TryAdd(in TKey key)
        {
            if (_count > 0 && _buffer[_count - 1].Key.Equals(key))
            {
                return false;
            }
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = new();
            return true;
        }

        /// <summary>
        /// Returns the index of the node that contains a key that matches the one provided key, starting at the provided index.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The index of the node with the matching key. If one was not found, it returns the index where it would go, but bit-flipped.</returns>
        public int Find(in TKey key)
        {
            int lo = 0;
            int hi = _count - 1;
            while (lo <= hi)
            {
                int curr = lo + (hi - lo >> 1);
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
                Insert(index, (key, new TValue()));
            }
            return index;
        }

        /// <summary>
        /// Returns the index of the node from the list that contains a key that matches the one provided, starting from the provided index.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <remarks>Undefined behavior will occur if the key and index are out of sync</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The index of the node with the matching key</returns>
        public ref TValue FindOrEmplace(in TKey key)
        {
            return ref _buffer[FindOrEmplaceIndex(key)].Value;
        }

        /// <summary>
        /// Removes the node with the given key if one exists
        /// </summary>
        /// <param name="key">The key to query for</param>
        /// <returns>Whether a node was found and removed</returns>
        public bool Remove(in TKey key)
        {
            int index = Find(key);
            if (index < 0)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Returns a reference to the value from the node with a matching key.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>A reference of the node with the matching key.</returns>
        /// <exception cref="KeyNotFoundException">A node with the provided key does not exist</exception>
        public ref TValue GetValue(in TKey key)
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
        public bool TryGetLastValue(in TKey key, out TValue value)
        {
            if (_count > 0)
            {
                ref var node = ref _buffer[_count - 1];
                if (node.Key.Equals(key))
                {
                    value = node.Value;
                    return true;
                }
            }
            value = default!;
            return false;
        }
    }
}
