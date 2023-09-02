using System;
using System.Collections;
using System.Collections.Generic;

namespace YARG.Core.Chart.FlatDictionary
{
    public struct FlatMapNode<TKey, TObj>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        public TKey position;
        public TObj obj;
        public static bool operator <(FlatMapNode<TKey, TObj> node, TKey position) { return node.position.CompareTo(position) < 0; }
        public static bool operator >(FlatMapNode<TKey, TObj> node, TKey position) { return node.position.CompareTo(position) > 0; }
    }

    public class FlatDictionary<TKey, TObj> : IDisposable, IEnumerable<FlatMapNode<TKey, TObj>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TObj : new()
    {
        private const int DEFAULTCAPACITY = 16;
        private int _count;
        private int _capacity;
        private int _version;
        private bool _disposed;
        private FlatMapNode<TKey, TObj>[] _buffer = Array.Empty<FlatMapNode<TKey, TObj>>();
        public int Count { get { return _count; } }
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (value < _count)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value != _capacity)
                {
                    if (value > 0)
                    {
                        if (value > int.MaxValue)
                            value = int.MaxValue;
                        Array.Resize(ref _buffer, value);
                        _capacity = _buffer.Length;
                    }
                    else
                    {
                        _buffer = Array.Empty<FlatMapNode<TKey, TObj>>();
                        _capacity = 0;
                    }
                    ++_version;
                }
            }
        }

        public FlatDictionary() { }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _buffer = Array.Empty<FlatMapNode<TKey, TObj>>();
            }
            _disposed = true;
        }

        ~FlatDictionary()
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
            for (int i = 0; i < _count; ++i)
                _buffer[i] = default;

            if (_count > 0)
                _version++;
            _count = 0;
        }

        public bool IsEmpty() { return _count == 0; }

        protected void CheckAndGrow()
        {
            if (_count == int.MaxValue)
                throw new OverflowException("Element limit reached");

            if (_count == _capacity)
                Grow();

            ++_version;
        }

        protected void Grow()
        {
            int newcapacity = _capacity == 0 ? DEFAULTCAPACITY : 2 * _capacity;
            if ((uint) newcapacity > int.MaxValue) newcapacity = int.MaxValue;
            Capacity = newcapacity;
        }

        public void TrimExcess()
        {
            if (_count < _capacity)
                Capacity = _count;
        }

        public ref TObj Add(TKey position)
        {
            return ref Add(position, new());
        }

        public ref TObj Add(TKey position, ref TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            ref var node = ref _buffer[index];
            node.position = position;
            node.obj = obj;
            return ref node.obj;
        }

        public ref TObj Add(TKey position, TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            ref var node = ref _buffer[index];
            node.position = position;
            node.obj = obj;
            return ref node.obj;
        }

        public void Add_NoReturn(TKey position)
        {
            Add_NoReturn(position, new());
        }

        public void Add_NoReturn(TKey position, TObj obj)
        {
            CheckAndGrow();
            int index = _count++;
            ref var node = ref _buffer[index];
            node.position = position;
            node.obj = obj;
        }

        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) _count)
                throw new IndexOutOfRangeException();

            _buffer[index] = default;
            --_count;
            if (index < _count)
            {
                Array.Copy(_buffer, index + 1, _buffer, index, _count - index);
            }
            ++_version;
        }

        public void Pop()
        {
            if (_count == 0)
                throw new Exception("Pop on emtpy map");
            _buffer[_count - 1] = default;
            --_count;
            ++_version;
        }

        public ref TObj this[TKey position] { get { return ref Find_Or_Add(0, position); } }

        public int Find_Or_Add_index(int searchIndex, TKey position) { return Find_or_emplace_index(searchIndex, position); }

        public ref TObj Find_Or_Add(int searchIndex, TKey position)
        {
            int index = Find_or_emplace_index(searchIndex, position);
            return ref _buffer[index].obj;
        }

        protected int Find_or_emplace_index(int searchIndex, TKey position)
        {
            int index = Find(searchIndex, position);
            if (index < 0)
            {
                CheckAndGrow();
                index = ~index;
                if (index < _count)
                {
                    Array.Copy(_buffer, index, _buffer, index + 1, _count - index);
                }
                ++_count;
                ref var node = ref _buffer[index];
                node.position = position;
                node.obj = new();
            }
            return index;
        }

        public ref TObj At(TKey position)
        {
            int index = Find(0, position);
            if (index < 0)
                throw new KeyNotFoundException();
            return ref _buffer[index].obj;
        }

        public ref FlatMapNode<TKey, TObj> At_index(int index)
        {
            return ref _buffer[index];
        }

        public ref TObj Get_Or_Add_Last(TKey position)
        {
            if (_count == 0)
                return ref Add(position);

            ref var node = ref _buffer[_count - 1];
            if (node < position)
                return ref Add(position);

            return ref node.obj;
        }

        public ref TObj Traverse_Backwards_Until(TKey position)
        {
            int index = _count;
            while (index > 0)
                if (_buffer[--index].position.CompareTo(position) <= 0)
                    break;
            return ref _buffer[index].obj;
        }

        public ref TObj Last() { return ref _buffer[_count - 1].obj; }

        public bool ValidateLastKey(TKey position)
        {
            return _count > 0 && _buffer[_count - 1].position.Equals(position);
        }

        public bool Contains(TKey position) { return Contains(0, position); }

        public bool Contains(int searchIndex, TKey position) { return Find(searchIndex, position) >= 0; }

        public int Find(int searchIndex, TKey position)
        {
            int lo = searchIndex;
            int hi = Count - (searchIndex + 1);
            while (lo <= hi)
            {
                int curr = lo + ((hi - lo) >> 1);
                int order = _buffer[curr].position.CompareTo(position);
                if (order == 0) return curr;
                if (order < 0)
                    lo = curr + 1;
                else
                    hi = curr - 1;
            }
            return ~lo;
        }

        public (FlatMapNode<TKey, TObj>[], int) Data => new(_buffer, _count);

        public IEnumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator<FlatMapNode<TKey, TObj>> IEnumerable<FlatMapNode<TKey, TObj>>.GetEnumerator()
        {
            return ((IEnumerable<FlatMapNode<TKey, TObj>>) this).GetEnumerator();
        }
        public struct Enumerator : IEnumerator<FlatMapNode<TKey, TObj>>, IEnumerator
        {
            private readonly FlatDictionary<TKey, TObj> _map;
            private int _index;
            private readonly int _version;
            private FlatMapNode<TKey, TObj> _current;

            internal Enumerator(FlatDictionary<TKey, TObj> map)
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
