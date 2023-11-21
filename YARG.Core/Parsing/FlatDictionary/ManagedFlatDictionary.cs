using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace YARG.Core.Parsing
{
    /// <summary>
    /// A general purpose subtype of FlatDictionary.
    /// </summary>
    /// <remarks>The object type must support default construction</remarks>
    /// <typeparam name="TKey">The key used to order the objects</typeparam>
    /// <typeparam name="TValue">The objects to place at each new position</typeparam>
    public class ManagedFlatDictionary<TKey, TValue> : FlatDictionary<TKey, TValue>
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TValue : new()
    {
        private FlatMapNode<TKey, TValue>[] _buffer = Array.Empty<FlatMapNode<TKey, TValue>>();

        public override int Capacity
        {
            get => _capacity;
            set
            {
                if (_count <= value && value != _capacity)
                {
                    if (value > 0)
                    {
                        Array.Resize(ref _buffer, value);
                        _capacity = _buffer.Length;
                    }
                    else
                    {
                        _buffer = Array.Empty<FlatMapNode<TKey, TValue>>();
                        _capacity = 0;
                    }
                    ++_version;
                }
            }
        }

        public override Span<FlatMapNode<TKey, TValue>> Span => new(_buffer, 0, _count);

        public ManagedFlatDictionary() { }

        public ManagedFlatDictionary(int capacity)
        {
            Capacity = capacity;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _buffer = Array.Empty<FlatMapNode<TKey, TValue>>();
                _disposed = true;
            }
        }

        public override void Clear()
        {
            for (int i = 0; i < _count; ++i)
                _buffer[i] = default;

            if (_count > 0)
                _version++;
            _count = 0;
        }

        public override ref TValue Add(TKey position)
        {
            return ref Add(position, new());
        }

        public override void Add_NoReturn(TKey position)
        {
            Add_NoReturn(position, new());
        }

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public override void Insert(int index, TKey position, ref TValue obj)
        {
            CheckAndGrow();
            if (index < _count)
            {
                Array.Copy(_buffer, index, _buffer, index + 1, _count - index);
            }
            ++_count;

            ref var node = ref _buffer[index];
            node.position = position;
            node.obj = obj;
        }

        public override ref TValue Traverse_Backwards_Until(TKey key)
        {
            int index = _count - 1;
            while (index > 0 && key.CompareTo(_buffer[index].position) < 0)
                --index;
            return ref _buffer[index].obj;
        }

        public override void RemoveAt(int index)
        {
            if (index < _count)
            {
                _buffer[index] = default;
                --_count;
                if (index < _count)
                {
                    Array.Copy(_buffer, index + 1, _buffer, index, _count - index);
                }
                ++_version;
            }
            else
                throw new IndexOutOfRangeException();
        }

        public override void Pop()
        {
            if (_count == 0)
                throw new InvalidOperationException();

            --_count;
            ++_version;
            _buffer[_count] = default;
        }

        public override void Reset()
        {
            _buffer = Array.Empty<FlatMapNode<TKey, TValue>>();
            base.Reset();
        }

        protected override int Find_or_emplace_index(int searchIndex, TKey position)
        {
            int index = Find(searchIndex, position);
            if (index < 0)
            {
                index = ~index;
                Insert(index, position, new());
            }
            return index;
        }

        public override ref FlatMapNode<TKey, TValue> At_index(int index)
        {
            return ref _buffer[index];
        }

        protected override ref FlatMapNode<TKey, TValue> GetRef(int index)
        {
            return ref _buffer[index];
        }
    }
}
