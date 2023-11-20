using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YARG.Core.IO
{
    public class SngFileStream : Stream
    {
        private const int KEY_MASK = 0xFF;

        private static readonly int VECTOR_MASK = SngMask.VECTORBYTE_COUNT - 1;
        private static readonly int VECTOR_SHIFT;
        private static readonly int NUM_VECTORS_MASK = SngMask.NUMVECTORS - 1;

        static SngFileStream()
        {
            int val = SngMask.VECTORBYTE_COUNT;
            while (val > 1)
            {
                VECTOR_SHIFT++;
                val >>= 1;
            }
        }

        public static DisposableArray<byte> LoadFile(string file, long fileSize, long position, SngMask mask)
        {
            using FileStream filestream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            if (filestream.Seek(position, SeekOrigin.Begin) != position)
                throw new EndOfStreamException();

            // `Using` modifier ensures that, in the presence of an exception,
            // the memory allocated to hold the file will be cleared
            using var data = DisposableArray<byte>.Create(filestream, (int)fileSize);
            int loopCount = (data.Length - SngMask.VECTORBYTE_COUNT) >> VECTOR_SHIFT;
            Parallel.For(0, loopCount, i =>
            {
                unsafe
                {
                    var pos = data.Ptr + (i << VECTOR_SHIFT);
                    var result = Vector.Xor(Unsafe.AsRef<Vector<byte>>(pos), mask.Vectors[i & NUM_VECTORS_MASK]);
                    Unsafe.CopyBlock(pos, &result, (uint)SngMask.VECTORBYTE_COUNT);
                }
            });

            for (int buffIndex = loopCount << VECTOR_SHIFT; buffIndex < data.Length; buffIndex++)
            {
                unsafe
                {
                    data.Ptr[buffIndex] ^= mask.Keys.Ptr[buffIndex & KEY_MASK];
                }
            }

            // Counteracts the above "using" declaration on the array so that
            // the data stays alive after the function exits
            return data.Clone();
        }

        // 128kiB
        private const int BUFFER_SIZE = 128 * 1024;
        private const int SEEK_MODULUS = BUFFER_SIZE - 1;
        private const int SEEK_MODULUS_MINUS = ~SEEK_MODULUS;
        

        private readonly FileStream _filestream;
        private readonly long fileSize;
        private readonly long initialOffset;

        private readonly SngMask mask;
        private readonly DisposableArray<byte> dataBuffer = new(BUFFER_SIZE);
        

        private int bufferPosition;
        private long _position;
        private bool disposedStream;

        public override bool CanRead => _filestream.CanRead;
        public override bool CanWrite => false;
        public override bool CanSeek => _filestream.CanSeek;
        public override long Length => fileSize;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > fileSize) throw new ArgumentOutOfRangeException();

                _position = value;
                if (value == fileSize)
                    return;

                _filestream.Seek(_position + initialOffset, SeekOrigin.Begin);
                bufferPosition = (int)(value & SEEK_MODULUS);
                UpdateBuffer();
            }
        }

        public SngFileStream(string file, long fileSize, long position, SngMask mask)
        {
            _filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            this.fileSize = fileSize;
            this.mask = mask;
            initialOffset = position;
            _filestream.Seek(position, SeekOrigin.Begin);
            UpdateBuffer();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();

            if (buffer == null)
                throw new ArgumentNullException();

            if (buffer.Length < offset + count)
                throw new ArgumentException();

            if (_position == fileSize)
                return 0;

            int read = 0;
            long bytesLeftInSection = dataBuffer.Length - bufferPosition;
            if (bytesLeftInSection > fileSize - _position)
                bytesLeftInSection = fileSize - _position;

            while (read < count)
            {
                int readCount = count - read;
                if (readCount > bytesLeftInSection)
                    readCount = (int)bytesLeftInSection;

                Unsafe.CopyBlock(ref buffer[offset + read], ref dataBuffer[bufferPosition], (uint) readCount);

                read += readCount;
                _position += readCount;
                bufferPosition += readCount;

                if (bufferPosition < dataBuffer.Length || _position == fileSize)
                    break;

                bufferPosition = 0;
                bytesLeftInSection = UpdateBuffer();
            }
            return read;
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
                    Position = fileSize + offset;
                    break;
            }
            return _position;
        }

        public override void Flush()
        {
            _filestream.Flush();
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
            if (!disposedStream)
            {
                if (disposing)
                {
                    _filestream.Dispose();
                    dataBuffer.Dispose();
                    mask.Dispose();
                }
                disposedStream = true;
            }
        }
        

        private long UpdateBuffer()
        {
            int readCount = BUFFER_SIZE - bufferPosition;
            if (readCount > fileSize - _position)
                readCount = (int)(fileSize - _position);

            var buffer = dataBuffer.Slice(bufferPosition, readCount);
            if (_filestream.Read(buffer) != readCount)
                throw new Exception("Seek error in SNGPKG subfile");

            int buffIndex = bufferPosition;

            // buffIndex % 256
            int key = buffIndex & KEY_MASK;
            {
                int count = SngMask.VECTORBYTE_COUNT - (buffIndex & VECTOR_MASK);
                if (count > readCount)
                    count = readCount;

                // Checks if we first need to align to VECTORBYTE_COUNT boundaries
                // Can't use the vector magic otherwise
                if (count != SngMask.VECTORBYTE_COUNT)
                {
                    unsafe
                    {
                        // Safe as the indices are guaranteed within range
                        for (int i = 0; i < count; ++i)
                            dataBuffer.Ptr[buffIndex++] ^= mask.Keys.Ptr[key++];
                    }

                    if (key == 256)
                        key = 0;
                }
            }

            // Sets what vector in the vector array member variable (pointer really) to start with
            int vectorIndex = (key & ~VECTOR_MASK) >> VECTOR_SHIFT;

            int end = bufferPosition + readCount;
            int vectorMax = end - SngMask.VECTORBYTE_COUNT;

            // Truncates last group that has a length less than VECTORBYTE_COUNT
            int loopCount = (vectorMax - buffIndex) >> VECTOR_SHIFT;

            Parallel.For(0, loopCount, i =>
            {
                unsafe
                {
                    // Aligned to VECTORBYTE_COUNT boundary
                    var pos = dataBuffer.Ptr + (i << VECTOR_SHIFT) + buffIndex;

                    // Vectors are an abstraction over a fixed set of memory.
                    // Think of it like a union with system-specific size.
                    //
                    // As a result, you can cast the address of a vector to any of its underlying types -
                    // or, in our case, the reverse, thanks to the DisposableArray buffer's inner fixed memory location.
                    // No copying required, except for Xor's paramaters (unless CLR removes that)
                    var result = Vector.Xor(*(Vector<byte>*)pos, mask.Vectors[(vectorIndex + i) & NUM_VECTORS_MASK]);
                    Unsafe.CopyBlock(pos, &result, (uint) SngMask.VECTORBYTE_COUNT);
                }
            });

            buffIndex += loopCount << VECTOR_SHIFT;
            while (buffIndex < end)
            {
                unsafe
                {
                    // Safe as the indices are guaranteed within range
                    dataBuffer.Ptr[buffIndex] ^= mask.Keys.Ptr[buffIndex & 255];
                }
                buffIndex++;
            }

            return readCount;
        }
    }
}
