using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.IO
{
    public unsafe struct YARGTextContainer<TChar>
        where TChar : unmanaged, IConvertible
    {
        private FixedArray<TChar>* _data;
        private Encoding _encoding;
        private long _position;

        public long Position
        {
            readonly get { return _position; }
            set { _position = value; }
        }

        public ref Encoding Encoding => ref _encoding;

        public readonly long Length => _data->Length;

        public readonly TChar* PositionPointer => _data->Ptr + _position;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TChar* GetBuffer() { return _data->Ptr; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetCurrentCharacter() { return _data->At(_position).ToInt32(null); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int Get() { return (*_data)[_position].ToInt32(null); }

        public readonly int this[long index] => (*_data)[_position + index].ToInt32(null);

        public readonly int At(long index) => _data->At(_position + index).ToInt32(null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsAtEnd() { return _position >= _data->Length; }

        public YARGTextContainer(FixedArray<TChar>* data, Encoding encoding)
        {
            _data = data;
            _encoding = encoding;
            _position = 0;
        }
    }
}
