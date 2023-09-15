using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace YARG.Core.Chart.FlatDictionary
{
    public class NativeFlatDictionary<TKey, TObj> : IDisposable, IEnumerable<FlatMapNode<TKey, TObj>>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TObj : unmanaged
    {
        private static readonly int SIZEOFNODE;
        private static TObj BASE;
        static NativeFlatDictionary()
        {
            unsafe
            {
                SIZEOFNODE = sizeof(FlatMapNode<TKey, TObj>);
            }
        }

        private const int DEFAULTCAPACITY = 16;
        private int _count;
        private int _capacity;
        private int _version;
        private bool _disposed;
        private unsafe FlatMapNode<TKey, TObj>* _buffer = null;

        public int Count { get { return _count; } }
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        unsafe
                        {
                            if (_buffer != null)
                                _buffer = (FlatMapNode<TKey, TObj>*) Marshal.ReAllocHGlobal((IntPtr) _buffer, (IntPtr) (value * SIZEOFNODE));
                            else
                                _buffer = (FlatMapNode<TKey, TObj>*) Marshal.AllocHGlobal(value * SIZEOFNODE);
                        }
                        _capacity = value;
                    }
                    else
                    {
                        unsafe
                        {
                            Marshal.FreeHGlobal((IntPtr) _buffer);
                            _buffer = null;
                        }
                        _capacity = 0;
                    }
                    ++_version;
                }
            }
        }

        public NativeFlatDictionary() { }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            unsafe
            {
                Marshal.FreeHGlobal((IntPtr) _buffer);
                _buffer = null;
            }
            _disposed = true;
        }

        ~NativeFlatDictionary()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            if (_count > 0)
                _version++;
            _count = 0;
        }

        public bool IsEmpty() { return _count == 0; }

        protected void CheckAndGrow()
        {
            if (_count < int.MaxValue)
            {
                if (_count == _capacity)
                    Grow();

                ++_version;
            }
            else
                throw new OverflowException("Element limit reached");
        }

        protected void Grow()
        {
            int newcapacity = _capacity == 0 ? DEFAULTCAPACITY : 2 * _capacity;
            if ((uint) newcapacity > int.MaxValue) newcapacity = int.MaxValue;
            Capacity = newcapacity;
        }

        public void TrimExcess()
        {
            Capacity = _count;
        }

        public ref TObj Add(TKey key)
        {
            return ref Add(key, ref BASE);
        }

        public ref TObj Add(TKey key, ref TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            unsafe
            {
                ref var node = ref _buffer[index];
                node.position = key;
                node.obj = obj;
                return ref node.obj;
            }
        }

        public ref TObj Add(TKey key, TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            unsafe
            {
                ref var node = ref _buffer[index];
                node.position = key;
                node.obj = obj;
                return ref node.obj;
            }
        }

        public void Add_NoReturn(TKey key)
        {
            Add_NoReturn(key, ref BASE);
        }

        public void Add_NoReturn(TKey key, ref TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            unsafe
            {
                ref var node = ref _buffer[index];
                node.position = key;
                node.obj = obj;
            }
        }

        public void Add_NoReturn(TKey key, TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            unsafe
            {
                ref var node = ref _buffer[index];
                node.position = key;
                node.obj = obj;
            }
        }

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert(int index, TKey key, TObj obj)
        {
            Insert(index, key, ref obj);
        }

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert(int index, TKey key, ref TObj obj)
        {
            CheckAndGrow();
            unsafe
            {
                var position = _buffer + index;
                if (index < _count)
                {
                    int leftover = _count - index;
                    Buffer.MemoryCopy(position, position + 1, leftover, leftover);
                }
                ++_count;
                position->position = key;
                position->obj = obj;
            }
        }

        public void RemoveAt(int index)
        {
            if (0 <= index && index < _count)
            {
                unsafe
                {
                    var position = _buffer + index;
                    int leftover = _count - index;

                    if (leftover > 1)
                    {
                        leftover *= SIZEOFNODE;
                        Buffer.MemoryCopy(position + 1, position, leftover, leftover - SIZEOFNODE);
                    }
                }

                --_count;
                ++_version;
            }
            else
                throw new IndexOutOfRangeException();
        }

        public void Pop()
        {
            if (_count > 0)
            {
                --_count;
                ++_version;
            }
            else
                throw new Exception("Pop on emtpy map");
        }

        public ref TObj this[TKey key] { get { return ref Find_Or_Add(0, key); } }

        public int Find_Or_Add_index(int searchIndex, TKey key) { return Find_or_emplace_index(searchIndex, key); }

        public ref TObj Find_Or_Add(int searchIndex, TKey key)
        {
            int index = Find_or_emplace_index(searchIndex, key);
            unsafe
            {
                return ref _buffer[index].obj;
            }
        }

        protected int Find_or_emplace_index(int searchIndex, TKey key)
        {
            int index = Find(searchIndex, key);
            if (index < 0)
            {
                index = ~index;
                Insert(index, key, ref BASE);
            }
            return index;
        }

        public ref TObj At(TKey position)
        {
            int index = Find(0, position);
            if (0 <= index)
            {
                unsafe
                {
                    return ref _buffer[index].obj;
                }
            }
            throw new KeyNotFoundException();
        }

        public ref FlatMapNode<TKey, TObj> At_index(int index)
        {
            unsafe
            {
                if (0 <= index && index < _count)
                    return ref _buffer[index];
                throw new IndexOutOfRangeException();
            }
        }

        public ref TObj Get_Or_Add_Last(TKey key)
        {
            if (_count == 0)
                return ref Add(key);

            unsafe
            {
                ref var node = ref _buffer[_count - 1];
                if (node.position.CompareTo(key) < 0)
                    return ref Add(key);

                return ref node.obj;
            }
        }

        public ref TObj Traverse_Backwards_Until(TKey key)
        {
            unsafe
            {
                var position = _buffer + _count - 1;
                while (position > _buffer && key.CompareTo(position->position) < 0)
                    --position;
                return ref position->obj;
            }
        }

        public ref TObj Last()
        {
            unsafe
            {
                return ref _buffer[_count - 1].obj;
            }
        }

        public bool ValidateLastKey(TKey key)
        {
            unsafe
            {
                return _count > 0 && _buffer[_count - 1].position.Equals(key);
            }
        }

        public bool Contains(TKey key) { return Contains(0, key); }

        public bool Contains(int searchIndex, TKey key) { return Find(searchIndex, key) >= 0; }

        public int Find(int searchIndex, TKey key)
        {
            int lo = searchIndex;
            int hi = Count - (searchIndex + 1);
            while (lo <= hi)
            {
                int curr = lo + ((hi - lo) >> 1);
                int order;
                unsafe
                {
                    order = _buffer[curr].position.CompareTo(key);
                }
                if (order == 0) return curr;
                if (order < 0)
                    lo = curr + 1;
                else
                    hi = curr - 1;
            }
            return ~lo;
        }

        public Span<FlatMapNode<TKey, TObj>> Span
        {
            get
            {
                unsafe
                {
                    return new(_buffer, _count);
                }
            }
        }

        public unsafe FlatMapNode<TKey, TObj>* Data => _buffer;

        public IEnumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<FlatMapNode<TKey, TObj>> IEnumerable<FlatMapNode<TKey, TObj>>.GetEnumerator()
        {
            return ((IEnumerable<FlatMapNode<TKey, TObj>>) this).GetEnumerator();
        }
        public struct Enumerator : IEnumerator<FlatMapNode<TKey, TObj>>, IEnumerator
        {
            private readonly NativeFlatDictionary<TKey, TObj> _map;
            private int _index;
            private readonly int _version;
            private FlatMapNode<TKey, TObj> _current;

            internal Enumerator(NativeFlatDictionary<TKey, TObj> map)
            {
                _map = map;
                _index = 0;
                _version = map._version;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var localMap = _map;
                if (_version == localMap._version && ((uint) _index < (uint) localMap._count))
                {
                    _current = localMap.At_index(_index++);
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _map._version)
                    throw new InvalidOperationException("Enum failed - Map was updated");
                _index = _map._count + 1;
                _current = default;
                return false;
            }

            public FlatMapNode<TKey, TObj> Current => _current;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _map._count + 1)
                        throw new InvalidOperationException("Enum Operation not possible");
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _map._version)
                    throw new InvalidOperationException("Enum failed - Map was updated");
                _index = 0;
                _current = default;
            }
        }
    }
}
