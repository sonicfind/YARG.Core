using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YARG.Core.IO;

namespace YARG.Core.Parsing
{
    /// <summary>
    /// A subtype of FlatDictionary made specifically to hold explicitly unmanaged types.
    /// </summary>
    /// <remarks>
    /// With the unamanaged restriction on both the keys and values, this subtype can utilize a bunch
    /// of marshalling and manual pointer work to handle the data. This can grant a reasonable memory
    /// and speed benefit as it can rely on fixed memory with true random access.
    /// </remarks>
    /// <typeparam name="TKey">The unmanaged key used to order the objects</typeparam>
    /// <typeparam name="TValue">The unmanaged objects to place at each new position</typeparam>
    public class NativeFlatDictionary<TKey, TValue> : FlatDictionary<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        private static TValue BASE;

        private DisposableArray<FlatMapNode<TKey, TValue>>? _buffer = null;

        public override int Capacity
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
                                _buffer = DisposableArray<FlatMapNode<TKey, TValue>>.Realloc(_buffer, value);
                            else
                                _buffer = new DisposableArray<FlatMapNode<TKey, TValue>>(value);
                        }
                        _capacity = value;
                        ++_version;
                    }
                    else
                        Dispose(true);
                }
            }
        }

        public override unsafe Span<FlatMapNode<TKey, TValue>> Span
        {
            get
            {
                if (_buffer != null)
                    return new Span<FlatMapNode<TKey, TValue>>(_buffer.Ptr, _count);
                throw new InvalidOperationException();
            }
        }

        public unsafe FlatMapNode<TKey, TValue>* Data
        {
            get
            {
                if (_buffer != null)
                    return _buffer.Ptr;
                throw new InvalidOperationException();
            }
        }

        public NativeFlatDictionary() { }

        public NativeFlatDictionary(int capacity)
        {
            Capacity = capacity;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _buffer?.Dispose();
                _disposed = true;
            }
        }

        ~NativeFlatDictionary()
        {
            Dispose(false);
        }

        public override void Clear()
        {
            if (_count > 0)
                _version++;
            _count = 0;
        }

        public override ref TValue Add(TKey key)
        {
            return ref Add(key, ref BASE);
        }

        public override void Add_NoReturn(TKey key)
        {
            Add_NoReturn(key, ref BASE);
        }

        /// <remarks>
        /// Note: does NOT check for correct key ordering on forced insertion. Unsafe.
        /// </remarks>
        public override void Insert(int index, TKey key, ref TValue obj)
        {
            if (index < 0 || index > _count)
                throw new IndexOutOfRangeException();

            CheckAndGrow();
            unsafe
            {
                var position = _buffer!.Ptr + index;
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

        public override ref FlatMapNode<TKey, TValue> At_index(int index)
        {
            if (0 <= index && index < _count)
            {
                unsafe
                {
                    return ref _buffer!.Ptr[index];
                }
            }
            throw new IndexOutOfRangeException();
        }

        public override ref TValue Traverse_Backwards_Until(TKey key)
        {
            unsafe
            {
                var position = _buffer!.Ptr + _count - 1;
                while (position > _buffer!.Ptr && key.CompareTo(position->position) < 0)
                    --position;
                return ref position->obj;
            }
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || _count <= index)
                throw new IndexOutOfRangeException();

            unsafe
            {
                var position = _buffer!.Ptr + index;
                int leftover = _count - index;

                if (leftover > 1)
                {
                    leftover *= sizeof(FlatMapNode<TKey, TValue>);
                    Buffer.MemoryCopy(position + 1, position, leftover, leftover - sizeof(FlatMapNode<TKey, TValue>));
                }
            }

            --_count;
            ++_version;
        }

        public override void Pop()
        {
            if (_count == 0)
                throw new InvalidOperationException();

            --_count;
            ++_version;
        }

        public override void Reset()
        {
            _buffer?.Dispose();
            _buffer = null;
            base.Reset();
        }

        protected override int Find_or_emplace_index(int searchIndex, TKey key)
        {
            int index = Find(searchIndex, key);
            if (index < 0)
            {
                index = ~index;
                Insert(index, key, ref BASE);
            }
            return index;
        }

        protected override ref FlatMapNode<TKey, TValue> GetRef(int index)
        {
            unsafe
            {
                return ref _buffer!.Ptr[index];
            }
        }
    }
}
