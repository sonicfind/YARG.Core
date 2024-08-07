using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace YARG.Core.NewParsing
{
    [DebuggerDisplay("Count: {_count}")]
    public sealed class YARGManagedSortedList<TKey, TValue> : IEnumerable<YARGKeyValuePair<TKey, TValue>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : new()
    {
        private YARGKeyValuePair<TKey, TValue>[] _buffer = Array.Empty<YARGKeyValuePair<TKey, TValue>>();
        private int _count;
        private int _version;

        public int Count => _count;

        public int Capacity
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

        public Span<YARGKeyValuePair<TKey, TValue>> Span => new(_buffer, 0, _count);

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

        public void Add(TKey key, TValue value)
        {
            if (!Try_Add(key, in value))
            {
                throw new ArgumentException($"A value of key of value {key} already exists");
            }
        }

        public void Add(TKey key, in TValue value)
        {
            if (!Try_Add(key, in value))
            {
                throw new ArgumentException($"A value of key of value {key} already exists");
            }
        }

        public bool Try_Add(TKey key, TValue value)
        {
            return Try_Add(key, in value);
        }

        public bool Try_Add(TKey key, in TValue value)
        {
            int index = Find(key);
            if (index >= 0)
            {
                return false;
            }

            index = ~index;
            Insert_Forced(index, key, in value);
            return true;
        }

        public ref TValue Append(TKey key)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = new TValue();
            return ref node.Value;
        }

        public ref TValue Append(TKey key, TValue value)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = value;
            return ref node.Value;
        }

        public ref TValue Append(TKey key, in TValue value)
        {
            CheckAndGrow();
            ref var node = ref _buffer[_count++];
            node.Key = key;
            node.Value = value;
            return ref node.Value;
        }

        public ref TValue GetLastOrAppend(TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return ref Append(key);
            }
            return ref _buffer[_count - 1].Value;
        }

        public void AppendOrUpdate(in TKey key, in TValue value)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                CheckAndGrow();
                ++_count;
            }
            ref var node = ref _buffer [_count - 1];
            node.Key = key;
            node.Value = value;
        }

        public bool TryAppend(in TKey key, out TValue? value)
        {
            if (_count != 0 && _buffer[_count - 1].Key.Equals(key))
            {
                value = _buffer[_count - 1].Value;
                return false;
            }
            value = Append(key);
            return true;
        }

        public bool TryAppend(in TKey key)
        {
            bool append = _count == 0 || !_buffer[_count - 1].Key.Equals(key);
            if (append)
            {
                Append(key);
            }
            return append;
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, TKey key, TValue value)
        {
            Insert_Forced(index, key, in value);
        }

        /// <remarks>
        /// Does not check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert_Forced(int index, TKey key, in TValue value)
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

        public bool Remove(in TKey key)
        {
            int index = Find(key);
            return RemoveAtIndex(index);
        }

        public bool RemoveAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                return false;
            }

            --_count;
            Array.Copy(_buffer, index + 1, _buffer, index, _count - index);
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
            _buffer[_count] = default;
        }

        public ref TValue this[in TKey key] => ref FindOrEmplaceValue(key);

        public int FindOrEmplaceIndex(in TKey key, int startIndex = 0)
        {
            int index = Find(key, startIndex);
            if (index < 0)
            {
                index = ~index;
                Insert_Forced(index, key, new());
            }
            return index;
        }

        public ref TValue FindOrEmplaceValue(in TKey key, int startIndex = 0)
        {
            int index = FindOrEmplaceIndex(key, startIndex);
            return ref _buffer[index].Value;
        }

        public bool ContainsKey(in TKey key, int startIndex = 0)
        {
            return Find(key, startIndex) >= 0;
        }

        public bool TryGetValue(in TKey key, out TValue value)
        {
            int index = Find(key);
            if (index < 0)
            {
                value = default!;
                return false;
            }
            value = _buffer[index].Value;
            return true;
        }

        public int Find(in TKey key, int startIndex = 0)
        {
            if (startIndex < 0 || _count < startIndex)
            {
                throw new IndexOutOfRangeException();
            }

            int lo = startIndex;
            int hi = _count - (startIndex + 1);
            while (lo <= hi)
            {
                int curr = lo + ((hi - lo) >> 1);
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

        public ref TValue At(in TKey key)
        {
            int index = Find(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            return ref _buffer[index].Value;
        }

        public ref YARGKeyValuePair<TKey, TValue> ElementAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new IndexOutOfRangeException();
            }
            return ref _buffer[index];
        }

        public ref TValue Last()
        {
            if (_count == 0)
            {
                throw new IndexOutOfRangeException();
            }
            return ref _buffer[_count - 1].Value;
        }

        public ref TValue TraverseBackwardsUntil(in TKey key)
        {
            int index = _count - 1;
            while (index > 0 && key.CompareTo(_buffer[index].Key) < 0)
            {
                --index;
            }
            return ref _buffer[index].Value;
        }

        public bool TryGetLastValue(in TKey key, out TValue? value)
        {
            if (_count == 0 || !_buffer[_count - 1].Key.Equals(key))
            {
                value = default;
                return false;
            }
            value = _buffer[_count - 1].Value;
            return true;
        }

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

        private const int DEFAULT_CAPACITY = 16;
        private void Grow()
        {
            int newcapacity = _buffer.Length == 0 ?  DEFAULT_CAPACITY : 2 * _buffer.Length;
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
            private readonly YARGManagedSortedList<TKey, TValue> _map;
            private int _index;
            private readonly int _version;

            internal Enumerator(YARGManagedSortedList<TKey, TValue> map)
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
