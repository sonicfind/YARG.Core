using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YARG.Core.NewParsing
{
    [DebuggerDisplay("Count: {_count}")]
    public unsafe class YARGNativeSortedSet<TValue> : IEnumerable<TValue>, IDisposable
        where TValue : unmanaged, IEquatable<TValue>, IComparable<TValue>
    {
        private TValue* _buffer = null;
        private int _capacity;
        private int _count;
        private int _version;

        public int Count => _count;

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

        public Span<TValue> Span => new(_buffer, _count);
        public TValue* Data => _buffer;
        public TValue* End => _buffer + _count;

        public YARGNativeSortedSet<TValue> MoveToNewSet()
        {
            var newList = new YARGNativeSortedSet<TValue>()
            {
                _buffer = _buffer,
                _count = _count,
                _capacity = _capacity,
                _version = _version,
            };

            _buffer = null;
            _count = 0;
            _capacity = 0;
            _version = 0;
            return newList;
        }

        public bool IsEmpty()
        {
            return _count == 0;
        }

        public void TrimExcess()
        {
            Capacity = _count;
        }

        public void Clear()
        {
            if (_count > 0)
            {
                _version++;
            }
            _count = 0;
        }

        public void Add(in TValue value)
        {
            if (!Try_Add(in value))
            {
                throw new ArgumentException($"A value of {value} already exists");
            }
        }

        public bool Try_Add(in TValue value)
        {
            int index = Find(in value);
            if (index >= 0)
            {
                return false;
            }

            index = ~index;
            Insert_Forced(index, in value);
            return true;
        }

        public void Append(in TValue value)
        {
            CheckAndGrow();
            _buffer[_count++] = value;
        }

        public bool TryAppend(in TValue value)
        {
            if (_count > 0 && _buffer[_count - 1].Equals(value))
            {
                return false;
            }

            CheckAndGrow();
            _buffer[_count++] = value;
            return true;
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, TValue value)
        {
            Insert_Forced(index, in value);
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, in TValue value)
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

        public bool Remove(in TValue value)
        {
            int index = Find(value);
            return RemoveAtIndex(index);
        }

        public bool RemoveAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                return false;
            }

            --_count;
            var position = _buffer + index;
            int amount = (_count - index) * sizeof(TValue);
            Buffer.MemoryCopy(position + 1, position, amount, amount);
            return true;
        }

        public void Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            --_count;
            ++_version;
        }

        public int Find(in TValue value, int startIndex = 0)
        {
            if (startIndex < 0 || _count < startIndex)
            {
                throw new IndexOutOfRangeException();
            }

            if (_buffer == null)
            {
                return ~0;
            }

            var lo = _buffer + startIndex;
            var hi = _buffer + Count - (startIndex + 1);
            while (lo <= hi)
            {
                var curr = lo + ((hi - lo) >> 1);
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

        public bool Contains(in TValue value)
        {
            return Find(in value) >= 0;
        }

        public TValue* ElementAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new IndexOutOfRangeException();
            }
            return _buffer + index;
        }

        public TValue* Last()
        {
            return _buffer + _count - 1;
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
