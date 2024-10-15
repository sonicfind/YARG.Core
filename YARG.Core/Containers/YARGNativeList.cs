using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YARG.Core.Containers
{
    public unsafe struct YARGNativeList<T> : IEnumerable<T>, IDisposable
        where T : unmanaged
    {
        public static readonly YARGNativeList<T> Default = new()
        {
            _buffer = null,
            _capacity = 0,
            _count = 0,
            _version = 0
        };

        private T* _buffer;
        private int _capacity;
        private int _count;
        private int _version;

        /// <summary>
        /// The number of elements within the list
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// The capacity of the list where elements will reside
        /// </summary>
        public int Capacity
        {
            readonly get => _capacity;
            set
            {
                if (_version == -1)
                {
                    throw new ObjectDisposedException(typeof(T).Name);
                }

                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        int size = value * sizeof(T);
                        if (_buffer != null)
                        {
                            _buffer = (T*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) size);
                        }
                        else
                        {
                            _buffer = (T*) Marshal.AllocHGlobal(size);
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

        public readonly long MemoryUsage => _capacity * sizeof(T);

        /// <summary>
        /// The span view of the data up to <see cref="Count"/>
        /// </summary>
        public readonly Span<T> Span => new(_buffer, _count);

        /// <summary>
        /// The direct pointer for the underlying data. Use carefully.
        /// </summary>
        public readonly T* Data => _buffer;

        /// <summary>
        /// The direct pointer to the end position of the underlying data. Use carefully.
        /// </summary>
        public readonly T* End => _buffer + _count;

        /// <summary>
        /// Returns whether count is zero
        /// </summary>
        public readonly bool IsEmpty()
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
        /// Adds the given value to the end of the list
        /// </summary>
        /// <param name="value">The value to insert</param>
        public void Add(in T value)
        {
            CheckAndGrow();
            _buffer[_count++] = value;
        }

        /// <summary>
        /// Copies over the provided buffer of values to the end of the lsit
        /// </summary>
        /// <param name="values">The buffer containing the data to copy</param>
        /// <param name="count">The number of elements to copy from the buffer</param>
        public void AddRange(T* values, int count)
        {
            if (count < 0 || int.MaxValue - count < _count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            CheckAndGrow(count);
            long size = count * sizeof(T);
            Buffer.MemoryCopy(_buffer + _count, values, size, size);
            _count += count;
        }

        /// <summary>
        /// Forcibly inserts a value at the positional index.
        /// </summary>
        /// <remarks>
        /// Does not check for correct ordering on forced insertion
        /// </remarks>
        /// <param name="index">The position to place the node - an array offset.</param>
        /// <param name="value">The value to insert</param>
        public void Insert(int index, in T value)
        {
            CheckAndGrow();
            var position = _buffer + index;
            if (index < _count)
            {
                int leftover = (_count - index) * sizeof(T);
                Buffer.MemoryCopy(position, position + 1, leftover, leftover);
            }
            *position = value;
            ++_count;
        }

        /// <summary>
        /// Removes the value present at the provided array offset index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        /// <returns>Whether the index was valid</returns>
        public void Remove(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }

            --_count;
            var position = _buffer + index;
            int amount = (_count - index) * sizeof(T);
            Buffer.MemoryCopy(position + 1, position, amount, amount);
        }

        /// <summary>
        /// Returns a reference to the value at the provided index
        /// </summary>
        /// <param name="index">Array index in the list</param>
        /// <returns>The value by reference</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was below 0 or >= count</exception>
        public ref T this[int index]
        {
            get
            {
                if (index < 0 || _count <= index)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return ref _buffer[index];
            }
        }

        private const int DEFAULT_CAPACITY = 16;
        private void CheckAndGrow(int offset = 1)
        {
            if (_count >= int.MaxValue)
            {
                throw new OverflowException("Element limit reached");
            }

            if (_count > _capacity - offset)
            {
                int newcapacity = _capacity == 0 ? DEFAULT_CAPACITY : 2 * _capacity;
                while (0 < newcapacity && newcapacity - offset < _count)
                {
                    newcapacity *= 2;
                }

                if ((uint) newcapacity > int.MaxValue)
                {
                    newcapacity = int.MaxValue;
                }
                Capacity = newcapacity;
            }
            ++_version;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
            }
            _buffer = null;
            _version = 0;
            _capacity = 0;
            _count = 0;
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly YARGNativeList<T> _list;
            private readonly int _version;
            private int _index;

            internal Enumerator(YARGNativeList<T> list)
            {
                _list = list;
                _version = list._version;
                _index = -1;
            }

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }

                ++_index;
                return _index < _list.Count;
            }

            public readonly T Current
            {
                get
                {
                    if (_version != _list._version || _index < 0 || _index >= _list._count)
                    {
                        throw new InvalidOperationException("Enum Operation not possible");
                    }
                    return _list._buffer[_index];
                }
            }

            readonly object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }
                _index = -1;
            }
        }
    }
}
