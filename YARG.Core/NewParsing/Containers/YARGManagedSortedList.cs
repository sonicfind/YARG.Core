using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public sealed class YARGManagedSortedList<TKey, TValue> : YARGSortedList<TKey, TValue>, IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : new()
    {
        private YARGKeyValuePair<TKey, TValue>[] _buffer = Array.Empty<YARGKeyValuePair<TKey, TValue>>();

        public override int Capacity
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

        public override Span<YARGKeyValuePair<TKey, TValue>> Span => new(_buffer, 0, _count);

        public YARGManagedSortedList() { }

        public YARGManagedSortedList(int capacity)
        {
            Capacity = capacity;
        }

        public override void Clear()
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

        public override ref TValue Append(TKey key)
        {
            return ref Append(key, new TValue());
        }

        public override ref TValue Append(TKey key, TValue value)
        {
            CheckAndGrow();
            int index = _count++;

            ref var node = ref _buffer[index];
            node.Key = key;
            node.Value = value;
            return ref node.Value;
        }

        public override ref TValue Append(TKey key, in TValue value)
        {
            CheckAndGrow();
            int index = _count++;

            ref var node = ref _buffer[index];
            node.Key = key;
            node.Value = value;
            return ref node.Value;
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
            if (index < _count)
            {
                Array.Copy(_buffer, index, _buffer, index + 1, _count - index);
            }
            ++_count;

            ref var node = ref _buffer[index];
            node.Key = key;
            node.Value = value;
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
            _buffer[_count] = default;
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

        public override ref TValue GetLastOrAppend(TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return ref Append(key);
            }
            return ref _buffer[_count - 1].Value;
        }

        public override int Find(int startIndex, TKey key)
        {
            int lo = startIndex;
            int hi = Count - (startIndex + 1);
            while (lo <= hi)
            {
                int curr = lo + ((hi - lo) >> 1);
                int order = _buffer[curr].CompareTo(key);
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

        public override bool ValidateLastKey(TKey key)
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

            if (_count == _buffer.Length)
            {
                Grow();
            }
            ++_version;
        }

        private void Grow()
        {
            int newcapacity = _buffer.Length == 0 ? DEFAULT_CAPACITY : 2 * _buffer.Length;
            if ((uint) newcapacity > int.MaxValue)
            {
                newcapacity = int.MaxValue;
            }
            Capacity = newcapacity;
        }

        public IEnumerator GetEnumerator() { return new Enumerator(this); }

        IEnumerator<YARGKeyValuePair<TKey, TValue>> IEnumerable<YARGKeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<YARGKeyValuePair<TKey, TValue>>) this).GetEnumerator();
        }

        public struct Enumerator : IEnumerator<YARGKeyValuePair<TKey, TValue>>, IEnumerator
        {
            private readonly YARGManagedSortedList<TKey, TValue> _map;
            private int _index;
            private readonly int _version;
            private YARGKeyValuePair<TKey, TValue> _current;

            internal Enumerator(YARGManagedSortedList<TKey, TValue> map)
            {
                _map = map;
                _index = -1;
                _version = map._version;
                _current = default;
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
                if ((uint) _index == (uint) _map._count)
                {
                    _current = default;
                    return false;
                }
                _current = _map.ElementAtIndex(_index);
                return true;
            }

            public readonly YARGKeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_version != _map._version || _index < 0 || _index >= _map._count)
                    {
                        throw new InvalidOperationException("Enum Operation not possible");
                    }
                    return _current;
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
                _current = default;
            }
        }
    }
}
