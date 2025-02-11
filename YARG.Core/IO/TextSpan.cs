using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.IO
{
    public unsafe struct TextSpan
    {
        public static readonly TextSpan Empty = new()
        {
            ptr = null,
            length = 0
        };

        public byte* ptr;
        public int   length;

        public readonly bool IsEmpty => ptr == null || length == 0;

        public readonly ReadOnlySpan<byte> Span => new Span<byte>(ptr, (int)length);

        public readonly byte this[int index]
        {
            get
            {
                if (index < 0 || index >= length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return ptr[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool SequenceEqual(in TextSpan str)
        {
            return Span.SequenceEqual(str.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool SequenceEqual(in ReadOnlySpan<byte> str)
        {
            return Span.SequenceEqual(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string GetString(Encoding encoding)
        {
            return ptr != null ? encoding.GetString(ptr, (int)length) : string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string GetString(Encoding encoding, long offset, long length)
        {
            if (offset < 0 || offset + length > this.length)
            {
                throw new ArgumentOutOfRangeException();
            }
            return ptr != null ? encoding.GetString(ptr + offset, (int)length) : string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string GetValidatedString(ref Encoding encoding)
        {
            return GetValidatedString(ref encoding, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string GetValidatedString(ref Encoding encoding, long offset, long length)
        {
            if (offset < 0 || offset + length > this.length)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (ptr == null)
            {
                return string.Empty;
            }

            try
            {
                return encoding.GetString(ptr + offset, (int)length);
            }
            catch
            {
                if (encoding != YARGTextReader.UTF8Strict)
                {
                    throw;
                }
                encoding = YARGTextReader.Latin1;
                return encoding.GetString(ptr + offset, (int)length);
            }
        }

        public readonly bool StartsWith(in ReadOnlySpan<byte> str)
        {
            return Span.StartsWith(str);
        }

        public readonly override string ToString()
        {
            return GetString(Encoding.ASCII);
        }
    }
}
