using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
    [DebuggerDisplay("Count: {_count}")]
    public sealed unsafe class YARGNativeSortedList<TKey, TValue> : IDisposable, IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        // This pattern of copying a pre-defined value is faster than default construction
        // Note: except possibly for types of 16 bytes or less, idk
        private static readonly TValue DEFAULT_VALUE = default;

        private YARGKeyValuePair<TKey, TValue>* _buffer = null;
        private int _capacity;
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
            get => _capacity;
            set
            {
                if (_version == -1)
                {
                    throw new ObjectDisposedException(typeof(YARGNativeSortedList<TKey, TValue>).Name);
                }

                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        int size = value * sizeof(YARGKeyValuePair<TKey, TValue>);
                        if (_buffer != null)
                        {
                            _buffer = (YARGKeyValuePair<TKey, TValue>*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) size);
                        }
                        else
                        {
                            _buffer = (YARGKeyValuePair<TKey, TValue>*) Marshal.AllocHGlobal(size);
                        }
                        ++_version;
                    }
                    else
                    {
                        if (_buffer != null)
                        {
                            Marshal.FreeHGlobal((IntPtr) _buffer);
                        }
                        _buffer = null;
                        _version = 0;
                    }
                    _capacity = value;
                }
            }
        }

        /// <summary>
        /// The span view of the data up to <see cref="Count"/>
        /// </summary>
        public Span<YARGKeyValuePair<TKey, TValue>> Span => new(_buffer, _count);

        /// <summary>
        /// The direct pointer for the underlying data. Use carefully.
        /// </summary>
        public YARGKeyValuePair<TKey, TValue>* Data => _buffer;

        /// <summary>
        /// The direct pointer to the end position of the underlying data. Use carefully.
        /// </summary>
        public YARGKeyValuePair<TKey, TValue>* End => _buffer + _count;

        /// <summary>
        /// Transfers all the data from the source into the current instance, leaving the source in a default state.
        /// </summary>
        /// <remarks>Prior data held by the current instance will get disposed before the transfer</remarks>
        public YARGNativeSortedList<TKey, TValue> StealData(YARGNativeSortedList<TKey, TValue> source)
        {
            _Dispose();
            _buffer = source._buffer;
            _count = source._count;
            _capacity = source._capacity;
            _version = source._version;
            source._buffer = null;
            source._count = 0;
            source._capacity = 0;
            source._version = 0;
            return this;
        }

        /// <summary>
        /// Fills the current instance with data copied from the source
        /// </summary>
        public YARGNativeSortedList<TKey, TValue> CopyData(YARGNativeSortedList<TKey, TValue> source)
        {
            int bytes = source._count * sizeof(YARGKeyValuePair<TKey, TValue>);
            _buffer = (YARGKeyValuePair<TKey, TValue>*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) bytes);
            _count = source._count;
            _capacity = source._count;
            _version++;
            Buffer.MemoryCopy(_buffer, source._buffer, bytes, bytes);
            return this;
        }

        /// <summary>
        /// Returns whether count is zero
        /// </summary>
        public bool IsEmpty()
        {
            return _count == 0;
        }

        /// <summary>
        /// Shrinks the buffer down to match the number of elements
        /// </summary>
        public void TrimExcess()
        {
            Capacity = _count;
        }

        /// <summary>
        /// Sets Count to zero
        /// </summary>
        /// <remarks>
        /// Due to the unmanaged nature of the generic, and to how new nodes are overridden on append, simply setting count to zero is enough.
        /// If the type requires extra disposal behavior, that must be handled extrernally before calling this method.
        /// </remarks>
        public void Clear()
        {
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
        /// <returns>The pointer to the value from the newly created node</returns>
        public TValue* Append(in TKey key)
        {
            CheckAndGrow();
            var node = _buffer + _count++;
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
        public void Append(in TKey key, in TValue value)
        {
            CheckAndGrow();
            var node = _buffer + _count++;
            node->Key = key;
            node->Value = value;
        }

        /// <summary>
        /// Returns the value of the last node in the list.
        /// If the last node's key does not match, a new one is appended to the list with a defaulted value variable.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>The pointer to the last value in the list</returns>
        public TValue* GetLastOrAppend(in TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return Append(key);
            }
            return &_buffer[_count - 1].Value;
        }

        /// <summary>
        /// Returns the value of the last node in the list, alongside whether said node was appended in the operation.
        /// A new node will be appended if the last key does not match the one provided.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <param name="value">The pointer to the last node in the list</param>
        /// <returns>Whether a node was appeneded to the list</returns>
        public bool GetLastOrAppend(in TKey key, out TValue* value)
        {
            bool append = _count == 0 || !_buffer[_count - 1].Key.Equals(key);
            value = append ? Append(key) : &_buffer[_count - 1].Value;
            return append;
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
            var node = _buffer + _count - 1;
            node->Key = key;
            node->Value = value;
        }

        /// <summary>
        /// Attempts to add a new node to the end of the list with the provided key.
        /// </summary>
        /// <param name="key">The key to query and possibly append</param>
        /// <returns>Whether a new node was created</returns>
        public bool TryAppend(in TKey key)
        {
            if (_count > 0 && _buffer[_count - 1].Key.Equals(key))
            {
                return false;
            }

            CheckAndGrow();
            var node = _buffer + _count++;
            node->Key = key;
            node->Value = DEFAULT_VALUE;
            return true;
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
        public void Insert(int index, in TKey key, in TValue value)
        {
            if (index < 0 || _count < index)
            {
                throw new ArgumentOutOfRangeException();
            }

            CheckAndGrow();
            var position = _buffer + index;
            if (index < _count)
            {
                int leftover = (_count - index) * sizeof(YARGKeyValuePair<TKey, TValue>);
                Buffer.MemoryCopy(position, position + 1, leftover, leftover);
            }

            position->Key = key;
            position->Value = value;
            ++_count;
        }

        /// <summary>
        /// Removes the node with the given key if one exists
        /// </summary>
        /// <param name="key">The key to query for</param>
        /// <returns>Whether a node was found and removed</returns>
        public bool Remove(in TKey key)
        {
            int index = Find(in key);
            if (index < 0)
            {
                return false;
            }
            RemoveAtIndex(index);
            return true;
        }

        /// <summary>
        /// Removes the node present at the provided array offset index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        public void RemoveAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }

            --_count;
            var position = _buffer + index;
            int amount = (_count - index) * sizeof(YARGKeyValuePair<TKey, TValue>);
            Buffer.MemoryCopy(position + 1, position, amount, amount);
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
        /// Returns the index of the node from the list that contains a key that matches the one provided.
        /// If a node with that key does not exist, a new one is created in its appropriate spot first.
        /// </summary>
        /// <remarks>Undefined behavior will occur if the key and index are out of sync</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The index of the node with the matching key</returns>
        public int FindOrEmplaceIndex(in TKey key)
        {
            int index = Find(in key);
            if (index < 0)
            {
                index = ~index;
                Insert(index, in key, in DEFAULT_VALUE);
            }
            return index;
        }

        /// <summary>
        /// Returns the index of the node that contains a key that matches the one provided.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to find</param>
        /// <returns>The index of the node with the matching key. If one was not found, the index where it would go is returned, but bit-flipped.</returns>
        public int Find(in TKey key)
        {
            if (_buffer == null)
            {
                return ~0;
            }

            var lo = _buffer;
            var hi = _buffer + Count - 1;
            while (lo <= hi)
            {
                var curr = lo + (hi - lo >> 1);
                int order = curr->Key.CompareTo(key);
                if (order == 0)
                {
                    return (int) (curr - _buffer);
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
            return ~(int) (lo - _buffer);
        }

        /// <summary>
        /// Returns the pointer to the value from the node with a matching key.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="key">The key to query for and possibly emplace in the list</param>
        /// <returns>The pointer of the node with the matching key.</returns>
        /// <exception cref="KeyNotFoundException">A node with the provided key does not exist</exception>
        public TValue* At(in TKey key)
        {
            int index = Find(in key);
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
        public TValue* TraverseBackwardsUntil(in TKey key)
        {
            var curr = _buffer + _count - 1;
            while (curr > _buffer && key.CompareTo(curr->Key) < 0)
            {
                --curr;
            }
            return &curr->Value;
        }

        /// <summary>
        /// Returns whether the last node in the list contains a matching key.
        /// If true, the pointer to the value in the node will be provided.
        /// </summary>
        /// <param name="key">The key to linearly search for</param>
        /// <param name="valuePtr">The pointer to the last node, if the key matched</param>
        /// <returns>Whether the last key matched</returns>
        public bool TryGetLastValue(in TKey key, out TValue* valuePtr)
        {
            if (_count == 0 || !_buffer[_count - 1].Key.Equals(key))
            {
                valuePtr = null;
                return false;
            }
            valuePtr = &_buffer[_count - 1].Value;
            return true;
        }

        private void CheckAndGrow()
        {
            if (_count >= int.MaxValue)
            {
                throw new OverflowException("Element limit reached");
            }

            if (_count == _capacity)
            {
                Grow();
            }
            ++_version;
        }

        private const int DEFAULT_CAPACITY = 16;
        private void Grow()
        {
            int newcapacity = _capacity == 0 ? DEFAULT_CAPACITY : 2 * _capacity;
            if ((uint) newcapacity > int.MaxValue)
            {
                newcapacity = int.MaxValue;
            }
            Capacity = newcapacity;
        }

        private void _Dispose()
        {
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
            }
            _buffer = null;
            _version = -1;
            _capacity = 0;
            _count = 0;
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        ~YARGNativeSortedList()
        {
            _Dispose();
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
            private readonly YARGNativeSortedList<TKey, TValue> _map;
            private readonly int _version;
            private int _index;

            internal Enumerator(YARGNativeSortedList<TKey, TValue> map)
            {
                _map = map;
                _version = map._version;
                _index = -1;
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
                return _index < _map.Count;
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
