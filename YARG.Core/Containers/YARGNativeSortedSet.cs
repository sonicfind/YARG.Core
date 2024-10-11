﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YARG.Core.Containers
{
    /// <summary>
    /// Represents a sorted collection of values, specialized for unmanaged types.
    /// </summary>
    /// <remarks>
    /// Unlike the built-in Set container, values are stored in a single array of data.
    /// Restricting the types with the <see langword="unmanaged"/> constraint allows the container to utilize
    /// native memory tricks to provide substantial performance benefits. <br></br>
    /// These tricks allow it dodge interacting with GC's heap management, providing pleasant locality characteristics through fixed memory and manual disposability.<br></br>
    /// </remarks>
    /// <typeparam name="TValue">The type of sortable value to hold</typeparam>
    [DebuggerDisplay("Count: {_count}")]
    public unsafe class YARGNativeSortedSet<TValue> : IEnumerable<TValue>, IDisposable
        where TValue : unmanaged, IEquatable<TValue>, IComparable<TValue>
    {
        private TValue* _buffer = null;
        private int _capacity;
        private int _count;
        private int _version;

        /// <summary>
        /// The number of values within the list
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// The capacity of the list where values will reside
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (_version == -1)
                {
                    throw new ObjectDisposedException(typeof(TValue).Name);
                }

                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        int size = value * sizeof(TValue);
                        if (_buffer != null)
                        {
                            _buffer = (TValue*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) size);
                        }
                        else
                        {
                            _buffer = (TValue*) Marshal.AllocHGlobal(size);
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
        public Span<TValue> Span => new(_buffer, _count);

        /// <summary>
        /// The direct pointer for the underlying data. Use carefully.
        /// </summary>
        public TValue* Data => _buffer;

        /// <summary>
        /// The direct pointer to the end position of the underlying data. Use carefully.
        /// </summary>
        public TValue* End => _buffer + _count;

        public YARGNativeSortedSet() { }

        /// <summary>
        /// Transfers all the data to a new instance of the list, leaving the current one in its default state.
        /// </summary>
        /// <remarks>This is only to be used to dodge double-frees from any sort of conversions with readonly instances</remarks>
        public YARGNativeSortedSet(YARGNativeSortedSet<TValue> original)
        {
            _buffer = original._buffer;
            _count = original._count;
            _capacity = original._capacity;
            _version = original._version;
            original._buffer = null;
            original._count = 0;
            original._capacity = 0;
            original._version = 0;
        }

        /// <summary>
        /// Transfers all the data from the source into the current instance, leaving the source in a default state.
        /// </summary>
        /// <remarks>Prior data held by the current instance will get disposed before the transfer</remarks>
        public YARGNativeSortedSet<TValue> StealData(YARGNativeSortedSet<TValue> source)
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
        public YARGNativeSortedSet<TValue> CopyData(YARGNativeSortedSet<TValue> source)
        {
            int bytes = source._count * sizeof(TValue);
            _buffer = (TValue*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) bytes);
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
        /// Due to the unmanaged nature of the generic, simply setting count to zero is enough.
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
        /// Appends a new value to the end of the set if the set does not already end with the value
        /// </summary>
        /// <remarks>This does not do any checks in regards to ordering.</remarks>
        /// <param name="value">The value to add</param>
        /// <returns>Whether the value was appended to the set</returns>
        public bool Push(in TValue value)
        {
            if (_count > 0 && _buffer[_count - 1].Equals(value))
            {
                return false;
            }

            CheckAndGrow();
            _buffer[_count++] = value;
            return true;
        }

        /// <summary>
        /// Removes the last value from the list
        /// </summary>
        /// <returns>The value that was removed</returns>
        /// <exception cref="InvalidOperationException">The list has no elements to remove</exception>
        public TValue Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            --_count;
            ++_version;
            return _buffer[_count];
        }

        /// <summary>
        /// Inserts the given value into the set if not already present
        /// </summary>
        /// <param name="value">The value to insert</param>
        /// <returns>Whether the value was added</returns>
        public bool Add(in TValue value)
        {
            int index = Find(value);
            if (index >= 0)
            {
                return false;
            }
            Insert(~index, in value);
            return true;
        }

        /// <summary>
        /// Forcibly inserts a value at the positional index.
        /// </summary>
        /// <remarks>
        /// Does not check for correct ordering on forced insertion
        /// </remarks>
        /// <param name="index">The position to place the node - an array offset.</param>
        /// <param name="value">The value to insert</param>
        public void Insert(int index, in TValue value)
        {
            CheckAndGrow();
            var position = _buffer + index;
            if (index < _count)
            {
                int leftover = (_count - index) * sizeof(TValue);
                Buffer.MemoryCopy(position, position + 1, leftover, leftover);
            }
            *position = value;
            ++_count;
        }

        /// <summary>
        /// Removes the value from the list if present
        /// </summary>
        /// <param name="value">The value to query for</param>
        /// <returns>Whether the value was found and removed</returns>
        public bool Remove(in TValue value)
        {
            int index = Find(value);
            if (index < 0)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes the value present at the provided array offset index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }

            --_count;
            var position = _buffer + index;
            int amount = (_count - index) * sizeof(TValue);
            Buffer.MemoryCopy(position + 1, position, amount, amount);
        }

        /// <summary>
        /// Searches for the provided value and returns the array positional index.
        /// </summary>
        /// <remarks>Performs a binary search</remarks>
        /// <param name="value">The value to find</param>
        /// <returns>The index of the matching value. If it was not found, the index where it would go is returned, but bit-flipped.</returns>
        public int Find(in TValue value)
        {
            if (_buffer == null)
            {
                return ~0;
            }

            var lo = _buffer + 0;
            var hi = _buffer + Count - 1;
            while (lo <= hi)
            {
                var curr = lo + (hi - lo >> 1);
                int order = curr->CompareTo(value);
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
        /// Returns whether the set contains the value
        /// </summary>
        /// <param name="value">Value to find</param>
        public bool Contains(in TValue value)
        {
            return Find(in value) >= 0;
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

        ~YARGNativeSortedSet()
        {
            _Dispose();
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return ((IEnumerable<TValue>) this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<TValue>, IEnumerator
        {
            private readonly YARGNativeSortedSet<TValue> _set;
            private readonly int _version;
            private int _index;

            internal Enumerator(YARGNativeSortedSet<TValue> set)
            {
                _set = set;
                _version = set._version;
                _index = -1;
            }

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _set._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }

                ++_index;
                return _index < _set.Count;
            }

            public readonly TValue Current
            {
                get
                {
                    if (_version != _set._version || _index < 0 || _index >= _set._count)
                    {
                        throw new InvalidOperationException("Enum Operation not possible");
                    }
                    return _set._buffer[_index];
                }
            }

            readonly object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _set._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }
                _index = -1;
            }
        }
    }
}