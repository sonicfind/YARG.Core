using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace YARG.Core.Containers
{
    [DebuggerDisplay("Count: {_count}")]
    public class YargManagedList<T> : IEnumerable<T>, IDisposable
        where T : new()
    {
        protected T[] _buffer;
        protected int _count;
        protected int _version;

        /// <summary>
        /// The number of elements within the list
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// The capacity of the list where elements will reside
        /// </summary>
        public int Capacity
        {
            get
            {
                return _buffer.Length;
            }
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
        public Span<T> Span
        {
            get
            {
                return new Span<T>(_buffer, 0, _count);
            }
        }

        /// <summary>
        /// The direct arrau for the underlying data. Use carefully.
        /// </summary>
        public T[] Data
        {
            get
            {
                return _buffer;
            }
        }

        public YargManagedList()
        {
            _buffer = Array.Empty<T>();
            _count = 0;
            _version = 0;
        }

        public YargManagedList(YargManagedList<T> source)
        {
            _buffer = new T[source._count];
            _count = source._count;
            _version = source._version;
            Array.Copy(source._buffer, _buffer, _count);
        }

        public void CopyFrom(YargManagedList<T> source)
        {
            if (source._count > _buffer.Length)
            {
                for (int i = 0; i < _count; ++i)
                {
                    _buffer[i] = default!;
                }
                _buffer = new T[source._count];
                Array.Copy(source._buffer, _buffer, source._count);
            }
            else
            {
                Array.Copy(source._buffer, _buffer, source._count);
                for (int i = source.Count; i < _count; ++i)
                {
                    _buffer[i] = default!;
                }
            }
            _count = source._count;
            _version++;
        }

        public YargManagedList<T> MoveFrom(YargManagedList<T> source)
        {
            for (int i = 0; i < _count; ++i)
            {
                _buffer[i] = default!;
            }

            _buffer = source._buffer;
            _count = source._count;
            _version = source._version;

            source._buffer = Array.Empty<T>();
            source._count = 0;
            source._version = 0;
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
        /// Clears every node present in the list and sets count to zero
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; ++i)
            {
                _buffer[i] = default!;
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
        public void AddRange(T[] values, int offset, int count)
        {
            if (count < 0 || int.MaxValue - count < _count)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            CheckAndGrow(count);
            Array.Copy(values, offset, _buffer, _count, count);
            _count += count;
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
        public void Insert(int index, in T value)
        {
            CheckAndGrow();
            if (index < _count)
            {
                Array.Copy(_buffer, index, _buffer, index + 1, _count - index);
            }
            ++_count;
            _buffer[index] = value;
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
            _buffer[_count] = default!;
            ++_version;
        }

        /// <summary>
        /// Removes the node present at the provided array offset index
        /// </summary>
        /// <param name="index">The offset into the inner array buffer</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }

            --_count;
            Array.Copy(_buffer, index + 1, _buffer, index, _count - index);
            _buffer[_count] = default!;
            ++_version;
        }

        /// <summary>
        /// Returns a reference to the value at the provided index
        /// </summary>
        /// <param name="index">Array index in the list</param>
        /// <returns>The value by reference</returns>
        public ref T this[int index]
        {
            get
            {
                return ref _buffer[index];
            }
        }

        /// <summary>
        /// Returns a reference to the value at the provided index
        /// </summary>
        /// <param name="index">Array index in the list</param>
        /// <returns>The value by reference</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was below 0 or >= count</exception>
        public ref T At(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new ArgumentOutOfRangeException();
            }
            return ref _buffer[index];
        }

        private const int DEFAULT_CAPACITY = 16;
        protected void CheckAndGrow(int offset = 1)
        {
            if (_count >= int.MaxValue)
            {
                throw new OverflowException("Element limit reached");
            }

            if (_count > _buffer.Length - offset)
            {
                int newcapacity = _buffer.Length == 0 ? DEFAULT_CAPACITY : 2 * _buffer.Length;
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
            for (int i = 0; i < _count; ++i)
            {
                _buffer[i] = default!;
            }
            _buffer = Array.Empty<T>();
            _version = 0;
            _count = 0;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly YargManagedList<T> _map;
            private int _index;
            private readonly int _version;

            internal Enumerator(YargManagedList<T> map)
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

            public readonly T Current
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

            readonly object IEnumerator.Current => Current!;

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
