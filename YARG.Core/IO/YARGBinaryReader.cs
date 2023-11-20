using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public enum Endianness
    {
        LittleEndian = 0,
        BigEndian = 1,
    };

    public sealed class HandleCounter<TType> : Counter, IDisposable
        where TType : unmanaged
    {
        private readonly GCHandle Handle;
        private bool _disposed;

        public unsafe TType* Ptr => (TType*)Handle.AddrOfPinnedObject();

        public HandleCounter(TType[] data)
        {
            Handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Handle.Free();
                _disposed = true;
            }
        }

        ~HandleCounter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class YARGBinaryReader : IDisposable
    {
        private readonly HandleCounter<byte>? _counter;
        private readonly DisposableArray<byte>? _buffer;
        private readonly unsafe byte* _end;
        private unsafe byte* _position;
        private bool disposedValue;

        public YARGBinaryReader(byte[] data)
            : this(data, 0, data.Length) { }

        public YARGBinaryReader(byte[] data, long offset, long length)
        {
            if (offset < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _counter = new HandleCounter<byte>(data);
            unsafe
            {
                _position = _counter.Ptr + offset;
                _end = _position + length;
            }
        }

        public YARGBinaryReader(Stream stream, long length)
        {
            _buffer = DisposableArray<byte>.Create(stream, (int) length);
            unsafe
            {
                _position = _buffer.Ptr;
                _end = _position + length;
            }
        }

        public unsafe YARGBinaryReader(byte* data, long length)
        {
            unsafe
            {
                _position = data;
                _end = _position + length;
            }
        }

        public YARGBinaryReader(MemoryStream stream, long length)
            : this(stream.GetBuffer(), stream.Position, length)
        {
            stream.Position += length;
        }

        public unsafe YARGBinaryReader(UnmanagedMemoryStream stream, long length)
            : this(stream.PositionPointer, length)
        {
            stream.Position += length;
        }

        public YARGBinaryReader Slice(int length)
        {
            if (disposedValue)
                throw new InvalidOperationException();
            return new YARGBinaryReader(this, length);
        }

        private YARGBinaryReader(YARGBinaryReader other, long length)
        {
            if (other._counter != null)
            {
                _counter = other._counter;
                _counter.Increment();
            }
            else if (other._buffer != null)
                _buffer = other._buffer.Clone();

            unsafe
            {
                _position = other._position;
                _end = _position + length;
            }
            other.Move(length);
        }

        public void Move(long amount)
        {
            unsafe
            {
                _position += amount;
                if (_position > _end)
                    throw new ArgumentOutOfRangeException("amount");
            }
        }

        public byte ReadByte()
        {
            if (disposedValue)
                throw new InvalidOperationException();

            unsafe
            {
                if (_position < _end)
                    return *_position++;
                throw new EndOfStreamException();
            }
        }

        public sbyte ReadSByte()
        {
            return (sbyte) ReadByte();
        }

        public bool ReadBoolean()
        {
            return ReadByte() > 0;
        }

        public TType Read<TType>(Endianness endianness = Endianness.LittleEndian)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            if (disposedValue)
                throw new InvalidOperationException();

            unsafe
            {
                byte* pos = _position;
                int size = sizeof(TType);
                Move(size);

                // If the memory layout of the host system matches the layout of
                // the value to be parsed from the file, we only require a cast
                if ((endianness == Endianness.LittleEndian) == BitConverter.IsLittleEndian)
                    return *(TType*) pos;

                // Reminder: _position moved
                pos = _position;
                // Otherwise, we have to flip the bytes
                byte* bytes = stackalloc byte[size];
                for (int i = 0; i < size; ++i)
                    bytes[i] = *--pos;

                return *(TType*) bytes;
            }
        }

        public bool ReadBytes(byte[] bytes)
        {
            if (disposedValue)
                throw new InvalidOperationException();

            unsafe
            {
                if (_position + bytes.Length > _end)
                    return false;

                Unsafe.CopyBlock(ref bytes[0], ref *_position, (uint) bytes.Length);
                _position += bytes.Length;
                return true;
            }
        }

        public byte[] ReadBytes(int length)
        {
            byte[] bytes = new byte[length];
            if (!ReadBytes(bytes))
                throw new Exception("Length of section exceeds bounds");
            return bytes;
        }

        public string ReadLEBString()
        {
            int length = ReadLEB();
            return length > 0 ? Encoding.UTF8.GetString(ReadSpan(length)) : string.Empty;
        }

        public int ReadLEB()
        {
            if (disposedValue)
                throw new InvalidOperationException();

            uint result = 0;
            byte byteReadJustNow;

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;
                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int) result;
                }
            }

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1111u)
            {
                throw new Exception("LEB value exceeds max allowed");
            }

            result |= (uint) byteReadJustNow << MaxBytesWithoutOverflow * 7;
            return (int) result;
        }

        public ReadOnlySpan<byte> ReadSpan(int length)
        {
            if (disposedValue)
                throw new InvalidOperationException();

            unsafe
            {
                var pos = _position;
                Move(length);
                return new ReadOnlySpan<byte>(pos, length);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    _buffer?.Dispose();

                if (_counter != null)
                {
                    _counter.Decrement();
                    if (_counter.Count == 0)
                        _counter.Dispose();
                }
                disposedValue = true;
            }
        }

        ~YARGBinaryReader()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
