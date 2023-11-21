using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace YARG.Core.Chart.FlatDictionary
{
    [DebuggerDisplay("{position} | {obj.ToString()}")]
    public struct FlatMapNode<TKey, TValue>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        public TKey position;
        public TValue obj;
        public static bool operator <(FlatMapNode<TKey, TValue> node, TKey position) { return node.position.CompareTo(position) < 0; }
        public static bool operator >(FlatMapNode<TKey, TValue> node, TKey position) { return node.position.CompareTo(position) > 0; }
    }

    /// <summary>
    /// A custom dictionary container type specialized for direct accesss through references
    /// instead of copies.
    /// </summary>
    /// <remarks>
    /// If we're going to try to modify the values present in the container, regardless of whether
    /// the Object is a reference type OR value type, then we need the ability to get direct access
    /// to the objects owned by the container.
    /// <br></br>
    /// <br>
    /// Of course, this increases the risk of dangling pointers when it comes to using value types,
    /// but so long as the reference variable has a limited lifetime, that should be a non-issue.
    /// </br>
    /// <br></br>
    /// <br>In this specific use-case, this behavior can be explicitly useful for modify note objects at
    /// certain positions during a parse. This allows the notes to be inserted into the dictionary at
    /// the start of parse instead of the end, giving more freedom to how parsers can be constructed.
    /// </br>
    /// <br></br>
    /// <br>
    /// Note: while it functions as a dictionary by name, it also maintains the ability to quickly add values
    /// to the end of the list. This, again, is specifically for charrt parsing, where the behavior guarantees
    /// the ordering to be as such.
    /// </br>
    /// </remarks>
    /// <typeparam name="TKey">The key used to order the objects</typeparam>
    /// <typeparam name="TValue">The objects to place at each new position</typeparam>
    public abstract class FlatDictionary<TKey, TValue> : IDisposable, IEnumerable<FlatMapNode<TKey, TValue>>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        protected const int DEFAULTCAPACITY = 16;
        protected int _count;
        protected int _capacity;
        protected int _version;
        protected bool _disposed;

        public int Count => _count;

        public abstract int Capacity { get; set; }

        public abstract Span<FlatMapNode<TKey, TValue>> Span { get; }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ref TValue Add(TKey position, TValue obj)
        {
            CheckAndGrow();
            int index = _count++;

            ref var node = ref GetRef(index);
            node.position = position;
            node.obj = obj;
            return ref node.obj;
        }

        public ref TValue Add(TKey position, ref TValue obj)
        {
            CheckAndGrow();
            int index = _count++;

            ref var node = ref GetRef(index);
            node.position = position;
            node.obj = obj;
            return ref node.obj;
        }

        public void Add_NoReturn(TKey position, TValue obj)
        {
            Add_NoReturn(position, ref obj);
        }

        public void Add_NoReturn(TKey key, ref TValue obj)
        {
            CheckAndGrow();
            int index = _count++;
            unsafe
            {
                ref var node = ref GetRef(index);
                node.position = key;
                node.obj = obj;
            }
        }

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public void Insert(int index, TKey position, TValue obj)
        {
            Insert(index, position, ref obj);
        }

        public ref TValue this[TKey position] { get { return ref Find_Or_Insert(0, position); } }

        public int Find_Or_Insert_index(int searchIndex, TKey position) { return Find_or_emplace_index(searchIndex, position); }

        public ref TValue Find_Or_Insert(int searchIndex, TKey position)
        {
            int index = Find_or_emplace_index(searchIndex, position);
            return ref GetRef(index).obj;
        }

        public bool Try_Insert(TKey position, TValue obj)
        {
            int index = Find(0, position);
            if (index >= 0)
                return false;

            index = ~index;
            Insert(index, position, ref obj);
            return true;
        }

        public ref TValue At(TKey position)
        {
            int index = Find(0, position);
            if (0 <= index)
                return ref GetRef(index).obj;
            throw new KeyNotFoundException();
        }

        public ref TValue Get_Or_Add_Last(TKey position)
        {
            if (_count == 0)
                return ref Add(position);

            ref var node = ref GetRef(_count - 1);
            if (node < position)
                return ref Add(position);

            return ref node.obj;
        }

        public ref TValue Last() { return ref GetRef(_count - 1).obj; }

        public bool ValidateLastKey(TKey position)
        {
            return _count > 0 && GetRef(_count - 1).position.Equals(position);
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
                int order = GetRef(curr).position.CompareTo(position);
                if (order == 0) return curr;
                if (order < 0)
                    lo = curr + 1;
                else
                    hi = curr - 1;
            }
            return ~lo;
        }

        public bool IsEmpty() { return _count == 0; }

        public void TrimExcess()
        {
            Capacity = _count;
        }

        public virtual void Reset()
        {
            _count = 0;
            _capacity = 0;
            _version = 0;
        }

        public abstract ref TValue Add(TKey position);

        public abstract void Add_NoReturn(TKey position);

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public abstract void Insert(int index, TKey position, ref TValue obj);

        public abstract ref FlatMapNode<TKey, TValue> At_index(int index);

        public abstract void RemoveAt(int index);

        public abstract void Pop();

        public abstract ref TValue Traverse_Backwards_Until(TKey position);

        public abstract void Clear();

        protected abstract int Find_or_emplace_index(int searchIndex, TKey position);

        protected abstract ref FlatMapNode<TKey, TValue> GetRef(int index);

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

        private void Grow()
        {
            int newcapacity = _capacity == 0 ? DEFAULTCAPACITY : 2 * _capacity;
            if ((uint) newcapacity > int.MaxValue) newcapacity = int.MaxValue;
            Capacity = newcapacity;
        }

        public IEnumerator GetEnumerator() { return new Enumerator(this); }

        IEnumerator<FlatMapNode<TKey, TValue>> IEnumerable<FlatMapNode<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<FlatMapNode<TKey, TValue>>) this).GetEnumerator();
        }

        public struct Enumerator : IEnumerator<FlatMapNode<TKey, TValue>>, IEnumerator
        {
            private readonly FlatDictionary<TKey, TValue> _map;
            private int _index;
            private readonly int _version;
            private FlatMapNode<TKey, TValue> _current;

            internal Enumerator(FlatDictionary<TKey, TValue> map)
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

            public FlatMapNode<TKey, TValue> Current => _current;

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
