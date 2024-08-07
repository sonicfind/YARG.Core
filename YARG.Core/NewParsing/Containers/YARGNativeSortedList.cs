﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YARG.Core.NewParsing
{
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

        public int Count => _count;

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

        public Span<YARGKeyValuePair<TKey, TValue>> Span => new(_buffer, _count);
        public YARGKeyValuePair<TKey, TValue>* Data => _buffer;
        public YARGKeyValuePair<TKey, TValue>* End => _buffer + _count;

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

        public void Add(TKey key, in TValue value)
        {
            if (!Try_Add(key, in value))
            {
                throw new ArgumentException($"A key of value {key} already exists");
            }
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

        public TValue* Append(TKey key)
        {
            CheckAndGrow();
            var node = _buffer + _count++;
            node->Key = key;
            node->Value = DEFAULT_VALUE;
            return &node->Value;
        }

        public TValue* Append(TKey key, in TValue value)
        {
            CheckAndGrow();
            var node = _buffer + _count++;
            node->Key = key;
            node->Value = value;
            return &node->Value;
        }

        public void Append_NoReturn(TKey key, in TValue value)
        {
            CheckAndGrow();
            var node = _buffer + _count++;
            node->Key = key;
            node->Value = value;
        }

        public TValue* GetLastOrAppend(TKey key)
        {
            if (_count == 0 || _buffer[_count - 1] < key)
            {
                return Append(key);
            }
            return &_buffer[_count - 1].Value;
        }

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

        public bool TryAppend(in TKey key, out TValue* value)
        {
            bool append = _count == 0 || !_buffer[_count - 1].Key.Equals(key);
            value = append ? Append(key) : &_buffer[_count - 1].Value;
            return append;
        }

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
            var position = _buffer + index;
            int amount = (_count - index) * sizeof(YARGKeyValuePair<TKey, TValue>);
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

        public TValue* this[in TKey key] => FindOrEmplaceValue(in key);

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

        public TValue* FindOrEmplaceValue(in TKey key, int startIndex = 0)
        {
            int index = FindOrEmplaceIndex(key, startIndex);
            return &_buffer[index].Value;
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
                value = DEFAULT_VALUE;
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

            if (_buffer == null)
            {
                return ~0;
            }

            var lo = _buffer + startIndex;
            var hi = _buffer + Count - (startIndex + 1);
            while (lo <= hi)
            {
                var curr = lo + ((hi - lo) >> 1);
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

        public TValue* At(in TKey key)
        {
            int index = Find(key);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }
            return &_buffer[index].Value;
        }

        public YARGKeyValuePair<TKey, TValue>* ElementAtIndex(int index)
        {
            if (index < 0 || _count <= index)
            {
                throw new IndexOutOfRangeException();
            }
            return &_buffer[index];
        }

        public TValue* Last()
        {
            return &_buffer[_count - 1].Value;
        }

        public TValue* TraverseBackwardsUntil(in TKey key)
        {
            var curr = _buffer + _count - 1;
            while (curr > _buffer && key.CompareTo(curr->Key) < 0)
            {
                --curr;
            }
            return &curr->Value;
        }

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
