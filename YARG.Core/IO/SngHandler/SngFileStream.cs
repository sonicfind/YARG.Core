using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public class SngFileStream : Stream
    {
        //                1MB
        private const int BUFFER_SIZE = 1024 * 1024;
        private const int SEEK_MODULUS = BUFFER_SIZE - 1;
        private const int SEEK_MODULUS_MINUS = ~SEEK_MODULUS;

        private readonly string _name;
        private readonly SngTracker _tracker;
        private readonly SngFileListing _listing;
        private readonly FixedArray<byte> _dataBuffer = FixedArray<byte>.Alloc(BUFFER_SIZE);

        private long _bufferPosition = 0;
        private long _position = 0;
        private bool _disposed = false;

        public override bool CanRead => _tracker.Stream.CanRead;
        public override bool CanWrite => false;
        public override bool CanSeek => _tracker.Stream.CanSeek;
        public override long Length => _listing.Length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _listing.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _position = value;
                if (value < _listing.Length)
                {
                    _bufferPosition = (int) (value & SEEK_MODULUS);
                    UpdateBuffer();
                }
            }
        }

        public string Name => _name;

        public SngFileStream(string name, in SngFileListing listing, SngTracker tracker)
        {
            _name = name;
            _listing = listing;
            _tracker = tracker.AddOwner();
            try
            {
                UpdateBuffer();
            }
            catch
            {
                _tracker.Dispose();
                _dataBuffer.Dispose();
                throw;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if (buffer.Length < offset + count)
            {
                throw new ArgumentException();
            }

            if (_position == _listing.Length)
            {
                return 0;
            }

            long bytesLeftInSection = _dataBuffer.Length - _bufferPosition;
            if (bytesLeftInSection > _listing.Length - _position)
            {
                bytesLeftInSection = _listing.Length - _position;
            }

            long read = 0;
            for (long i = 0; i < count;)
            {
                long readCount = count - i;
                if (readCount > bytesLeftInSection)
                {
                    readCount = bytesLeftInSection;
                }

                Unsafe.CopyBlock(ref buffer[offset + i], ref _dataBuffer[_bufferPosition], (uint) readCount);
                _position += readCount;
                _bufferPosition += readCount;
                read += readCount;

                if (_bufferPosition < _dataBuffer.Length || _position == _listing.Length)
                {
                    break;
                }

                i += readCount;
                _bufferPosition = 0;

                long leftoverRead = count - i;
                long remaining = _listing.Length - _position;
                // Read the rest directly into the destination memory instead of the buffer
                if (leftoverRead >= remaining)
                {
                    unsafe
                    {
                        fixed (byte* buf = buffer)
                        {
                            byte* curr = buf + i;
                            LockedRead(new Span<byte>(curr, (int) remaining));
                            _position = _listing.Length;
                            DecryptVectorized(curr, _tracker.Mask.Ptr, curr + remaining);
                        }
                    }
                    read += remaining;
                    break;
                }

                // Read directly into the vectorizable leftover portion of the destination.
                // Anything left after will utilize the buffer to allow future `Read` calls to function properly
                if (leftoverRead >= BUFFER_SIZE)
                {
                    long amount = leftoverRead - (leftoverRead % BUFFER_SIZE);
                    unsafe
                    {
                        fixed (byte* buf = buffer)
                        {
                            byte* curr = buf + i;
                            LockedRead(new Span<byte>(curr, (int) amount));
                            _position += amount;
                            DecryptVectorized(curr, _tracker.Mask.Ptr, curr + amount);
                        }
                    }
                    read += amount;
                }

                bytesLeftInSection = UpdateBuffer();
            }
            return (int)read;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _listing.Length + offset;
                    break;
            }
            return _position;
        }

        public override void Flush()
        {
            lock (_tracker.Stream)
            {
                _tracker.Stream.Flush();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _dataBuffer.Dispose();
                _tracker.Dispose();
                _disposed = true;
            }
        }

        // We make a local copy to grant direct access to the Keys pointer
        // without having to make a `fixed` call
        private unsafe long UpdateBuffer()
        {
            long readCount = BUFFER_SIZE - _bufferPosition;
            if (readCount > _listing.Length - _position)
            {
                readCount = _listing.Length - _position;
            }

            LockedRead(_dataBuffer.Slice(_bufferPosition, readCount));

            byte* end = _dataBuffer.Ptr + _bufferPosition + readCount;
            long buffIndex = _bufferPosition;

            // If the current position doesn't align with the sse type, we need to process as many bytes
            // as necessary until we do (or until we reach the end of the stream, whichever first)
            while (_dataBuffer.Ptr + buffIndex < end)
            {
                long key_index = buffIndex % Vector<byte>.Count;
                if (key_index == 0)
                {
                    break;
                }
                _dataBuffer[buffIndex++] ^= _tracker.Mask[key_index];
            }

            DecryptVectorized(_dataBuffer.Ptr + buffIndex, _tracker.Mask.Ptr + (buffIndex % SngMask.MASK_SIZE), end);
            return readCount;
        }

        private unsafe void LockedRead(Span<byte> data)
        {
            lock (_tracker.Stream)
            {
                _tracker.Stream.Position = _position + _listing.Position;
                if (_tracker.Stream.Read(data) != data.Length)
                {
                    throw new IOException("Read error in SNGPKG subfile");
                }
            }
        }

        public static unsafe void DecryptVectorized(byte* position, byte* key_position, byte* end)
        {
            while (position + 16 * Vector<byte>.Count <= end)
            {
                var vecPosition = (Vector<byte>*) position;
                var vecKeys = (Vector<byte>*) key_position;
                for (int i = 0; i < 16; ++i)
                {
                    vecPosition[i] ^= vecKeys[i];
                }
                position += 16 * Vector<byte>.Count;
            }

            if (position + 8 * Vector<byte>.Count <= end)
            {
                var vecPosition = (Vector<byte>*) position;
                var vecKeys = (Vector<byte>*) key_position;
                for (int i = 0; i < 8; ++i)
                {
                    vecPosition[i] ^= vecKeys[i];
                }
                position += 8 * Vector<byte>.Count;
                key_position += 8 * Vector<byte>.Count;
            }

            if (position + 4 * Vector<byte>.Count <= end)
            {
                var vecPosition = (Vector<byte>*) position;
                var vecKeys = (Vector<byte>*) key_position;
                vecPosition[0] ^= vecKeys[0];
                vecPosition[1] ^= vecKeys[1];
                vecPosition[2] ^= vecKeys[2];
                vecPosition[3] ^= vecKeys[3];
                position += 4 * Vector<byte>.Count;
                key_position += 4 * Vector<byte>.Count;
            }

            if (position + 2 * Vector<byte>.Count <= end)
            {
                var vecPosition = (Vector<byte>*) position;
                var vecKeys = (Vector<byte>*) key_position;
                vecPosition[0] ^= vecKeys[0];
                vecPosition[1] ^= vecKeys[1];
                position += 2 * Vector<byte>.Count;
                key_position += 2 * Vector<byte>.Count;
            }

            if (position + Vector<byte>.Count <= end)
            {
                *(Vector<byte>*) position ^= *(Vector<byte>*) key_position;
                position += Vector<byte>.Count;
                key_position += Vector<byte>.Count;
            }

            while (position + sizeof(ulong) <= end)
            {
                *(ulong*) position ^= *(ulong*) key_position;
                position += sizeof(ulong);
                key_position += sizeof(ulong);
            }

            if (position + sizeof(uint) <= end)
            {
                *(uint*) position ^= *(uint*) key_position;
                position += sizeof(uint);
                key_position += sizeof(uint);
            }

            if (position + sizeof(ushort) <= end)
            {
                *(ushort*) position ^= *(ushort*) key_position;
                position += sizeof(ushort);
                key_position += sizeof(ushort);
            }

            if (position < end)
            {
                *position ^= *key_position;
            }
        }
    }
}
