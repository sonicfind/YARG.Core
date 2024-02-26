using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public sealed unsafe class YARGNativeSortedList<TKey, TValue> : YARGSortedList<TKey, TValue>, IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        private static readonly int SIZEOF_PAIR = sizeof(YARGKeyValuePair<TKey, TValue>);

        private YARGKeyValuePair<TKey, TValue>* _buffer = null;
        private int _capacity;
        private bool _disposed;

        public override int Capacity
        {
            get => _capacity;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        int size = value * SIZEOF_PAIR;
                        if (_buffer != null)
                        {
                            _buffer = (YARGKeyValuePair<TKey, TValue>*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr)size);
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

        public override Span<YARGKeyValuePair<TKey, TValue>> Span => new (_buffer, _count);

        public YARGKeyValuePair<TKey, TValue>* Data => _buffer;
        public YARGKeyValuePair<TKey, TValue>* End => _buffer + _count;

        public YARGNativeSortedList() { }

        public YARGNativeSortedList(int capacity)
        {
            Capacity = capacity;
        }

        public override void Clear()
        {
            if (_count > 0)
            {
                _version++;
            }
            _count = 0;
        }

        protected override void Dispose(bool _)
        {
            if (!_disposed)
            {
                if (_buffer != null)
                {
                    Marshal.FreeHGlobal((IntPtr) _buffer);
                }
                _buffer = null!;
                _version = 0;
                _capacity = 0;
                _count = 0;
                _disposed = true;
            }
        }

        ~YARGNativeSortedList()
        {
            Dispose(false);
        }

        public override ref TValue Append(TKey key)
        {
            CheckAndGrow();
            int index = _count++;

            var node = _buffer + index;
            node->Key = key;
            node->Value = default;
            return ref node->Value;
        }

        public override ref TValue Append(TKey key, TValue value)
        {
            CheckAndGrow();
            int index = _count++;

            var node = _buffer + index;
            node->Key = key;
            node->Value = value;
            return ref node->Value;
        }

        public override ref TValue Append(TKey key, in TValue value)
        {
            CheckAndGrow();
            int index = _count++;

            var node = _buffer + index;
            node->Key = key;
            node->Value = value;
            return ref node->Value;
        }

        public override int FindOrEmplaceIndex(int startIndex, TKey key)
        {
            int index = Find(startIndex, key);
            if (index < 0)
            {
                index = ~index;
                Insert_Forced(index, key, new());
            }
            return index;
        }

        public override ref TValue FindOrEmplaceValue(int startIndex, TKey key)
        {
            int index = FindOrEmplaceIndex(startIndex, key);
            return ref _buffer[index].Value;
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public override void Insert_Forced(int index, TKey key, in TValue value)
        {
            CheckAndGrow();
            var position = _buffer + index;
            if (index < _count)
            {
                int leftover = (_count - index) * SIZEOF_PAIR;
                Buffer.MemoryCopy(position, position + 1, leftover, leftover);
            }

            ++_count;
            position->Key = key;
            position->Value = value;
        }

        public override bool TryGetValue(TKey key, out TValue value)
        {
            int index = Find(0, key);
            if (index < 0)
            {
                value = default!;
                return false;
            }
            value = _buffer[index].Value;
            return true;
        }

        public override void Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            --_count;
            ++_version;
        }

        public override ref TValue At(TKey key)
        {
            int index = Find(0, key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            return ref _buffer[index].Value;
        }

        public override ref YARGKeyValuePair<TKey, TValue> ElementAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new IndexOutOfRangeException();
            }
            return ref _buffer[index];
        }

        public override bool RemoveAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                return false;
            }

            --_count;
            var position = _buffer + index;
            int amount = (_count - index) * SIZEOF_PAIR;
            Buffer.MemoryCopy(position + 1, position, amount, amount);
            return true;
        }

        public override ref TValue GetLastOrAppend(TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return ref Append(key);
            }
            return ref _buffer[_count - 1].Value;
        }

        public override int Find(int startIndex, in TKey key)
        {
            var lo = _buffer + startIndex;
            var hi = _buffer + Count - (startIndex + 1);
            while (lo <= hi)
            {
                var curr = lo + ((hi - lo) >> 1);
                int order = curr->CompareTo(key);
                if (order == 0)
                {
                    return (int)(curr - _buffer);
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
            return ~(int)(lo - _buffer);
        }

        public override bool ValidateLastKey(in TKey key)
        {
            return _count > 0 && _buffer[_count - 1].Equals(key);
        }

        public override ref TValue Last() { return ref _buffer[_count - 1].Value; }

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

        private void Grow()
        {
            int newcapacity = _capacity == 0 ? DEFAULT_CAPACITY : 2 * _capacity;
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
            private readonly YARGNativeSortedList<TKey, TValue> _map;
            private readonly YARGKeyValuePair<TKey, TValue>* _end;
            private readonly int _version;
            
            private YARGKeyValuePair<TKey, TValue>* _current;

            internal Enumerator(YARGNativeSortedList<TKey, TValue> map)
            {
                _map = map;
                _end = map._buffer != null ? map._buffer + map.Count : null;
                _version = map._version;
                _current = null;
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

                if (_end == null)
                {
                    return false;
                }

                if (_current != null)
                {
                    ++_current;
                }
                else
                {
                    _current = _map._buffer;
                }
                return _current < _end;
            }

            public readonly YARGKeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_version != _map._version || _current == null || _current >= _end)
                    {
                        throw new InvalidOperationException("Enum Operation not possible");
                    }
                    return *_current;
                }
            }

            readonly object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _map._version)
                {
                    throw new InvalidOperationException("Enum failed - Sorted List was updated");
                }
                _current = null;
            }
        }
    }
}
