using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YARG.Core.Containers
{
    [DebuggerDisplay("Count: {_count}")]
    public unsafe class YargNativeList<T> : IEnumerable<T>, IDisposable
        where T : unmanaged
    {
        private   long _capacity;
        private   long _version;
        protected T*   _buffer;
        protected long _count;

        /// <summary>
        /// The number of elements within the list
        /// </summary>
        public long Count => _count;

        /// <summary>
        /// The capacity of the list where elements will reside
        /// </summary>
        public long Capacity
        {
            get => _capacity;
            set
            {
                if (_count > value || value == _capacity)
                {
                    return;
                }

                if (value > 0)
                {
                    long size = value * sizeof(T);
                    if (_buffer != null)
                    {
                        _buffer = (T*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) size);
                    }
                    else
                    {
                        _buffer = (T*) Marshal.AllocHGlobal((IntPtr) size);
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

        /// <summary>
        /// The span view of the data up to <see cref="Count"/>
        /// </summary>
        public Span<T> Span => new(_buffer, (int)_count);

        public long CountInBytes => _count * sizeof(T);

        /// <summary>
        /// The span view of all elements as bytes
        /// </summary>
        public Span<byte> SpanAsBytes => new(_buffer, (int)CountInBytes);

        /// <summary>
        /// The direct pointer for the underlying data. Use carefully.
        /// </summary>
        public T* Data => _buffer;

        public YargNativeList()
        {
            _buffer = null;
            _capacity = 0;
            _count = 0;
            _version = 0;
        }

        public YargNativeList(YargNativeList<T> source)
        {
            _capacity = source._count;
            _count = source._count;
            _version = source._version;
            long bytes = _count * sizeof(T);
            _buffer = (T*) Marshal.AllocHGlobal((IntPtr) bytes);
            Buffer.MemoryCopy(source._buffer, _buffer, bytes, bytes);
        }

        public void CopyFrom(YargNativeList<T> source)
        {
            long byteCount = source._count * sizeof(T);
            if (source._count > _capacity)
            {
                _buffer = (T*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) byteCount);
                _capacity = source._count;
            }
            Buffer.MemoryCopy(source._buffer, _buffer, _capacity * sizeof(T), byteCount);
            _count = source._count;
            _version++;
        }

        public void MoveFrom(YargNativeList<T> source)
        {
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
            }

            _buffer = source._buffer;
            _capacity = source._capacity;
            _count = source._count;
            _version = source._version;
            source._buffer = null;
            source._capacity = 0;
            source._count = 0;
            source._version = 0;
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
        /// Due to the unmanaged nature of the generic, and to how new values lead to overriding buffer locations, simply setting count to zero is enough.
        /// If the type requires extra disposal behavior, that must be handled externally before calling this method.
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
        /// Copies over the provided buffer of values to the end of the list
        /// </summary>
        /// <param name="values">The buffer containing the data to copy</param>
        /// <param name="count">The number of elements to copy from the buffer</param>
        public void AddRange(T* values, long count)
        {
            if (count < 0 || long.MaxValue - count < _count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            CheckAndGrow(count);
            long size = count * sizeof(T);
            Buffer.MemoryCopy(_buffer + _count, values, size, size);
            _count += count;
        }

        /// <summary>
        /// Inserts a value into the provided index.
        /// </summary>
        /// <param name="index">The position to place the value</param>
        /// <param name="value">The value to insert</param>
        public void Insert(long index, in T value)
        {
            CheckAndGrow();
            var position = _buffer + index;
            if (index < _count)
            {
                long leftover = (_count - index) * sizeof(T);
                Buffer.MemoryCopy(position, position + 1, leftover, leftover);
            }
            *position = value;
            ++_count;
        }

        /// <summary>
        /// Removes the last value in the list
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
        /// Removes the value present at the provided index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        /// <returns>Whether the index was valid</returns>
        public void RemoveAt(long index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }

            --_count;
            var position = _buffer + index;
            long amount = (_count - index) * sizeof(T);
            Buffer.MemoryCopy(position + 1, position, amount, amount);
            ++_version;
        }

        /// <summary>
        /// Returns a reference to the value at the provided index
        /// </summary>
        /// <param name="index">Array index in the list</param>
        /// <returns>The value by reference</returns>
        public ref T this[long index] => ref _buffer[index];

        /// <summary>
        /// Returns a reference to the value at the provided index
        /// </summary>
        /// <param name="index">Array index in the list</param>
        /// <returns>The value by reference</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was below 0 or >= count</exception>
        public ref T At(long index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }
            return ref _buffer[index];
        }

        private const long DEFAULT_CAPACITY = 16;
        protected void CheckAndGrow(long offset = 1)
        {
            if (_count >= long.MaxValue)
            {
                throw new OverflowException("Element limit reached");
            }

            if (_count > _capacity - offset)
            {
                long newCapacity = _capacity == 0 ? DEFAULT_CAPACITY : 2 * _capacity;
                while (0 < newCapacity && newCapacity - offset < _count)
                {
                    newCapacity *= 2;
                }

                if ((ulong) newCapacity > long.MaxValue)
                {
                    newCapacity = long.MaxValue;
                }
                Capacity = newCapacity;
            }
            ++_version;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
                _buffer = null;
            }
            _version = 0;
            _capacity = 0;
            _count = 0;
        }

        ~YargNativeList()
        {
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        private struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly YargNativeList<T> _list;
            private readonly long _version;
            private long _index;

            internal Enumerator(YargNativeList<T> list)
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
