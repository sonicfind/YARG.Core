using System;
using System.Text;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public static class YARGTextContainer
    {
        public static readonly Encoding Latin1 = Encoding.GetEncoding(28591);
        public static readonly Encoding UTF8Strict = new UTF8Encoding(false, true);
    }

    public sealed class YARGTextContainer<TChar> : IDisposable
        where TChar : unmanaged, IConvertible
    {
        private readonly HandleCounter<TChar>? _counter;
        private readonly DisposableArray<TChar>? _buffer;
        private bool _disposed;

        public readonly unsafe TChar* End;
        public unsafe TChar* Position;
        public unsafe TChar* Next;

        public TChar Current
        {
            get
            {
                unsafe
                {
                    if (Position < End)
                        return *Position;
                    throw new InvalidOperationException("End of file reached");
                }
            }
        }

        public YARGTextContainer(TChar[] data, int position = 0)
        {
            _counter = new HandleCounter<TChar>(data);
            unsafe
            {
                Position = _counter.Ptr + position;
                Next = Position;
                End = Position + data.Length;
            }
        }

        public unsafe YARGTextContainer(DisposableArray<TChar> data, int position = 0)
            : this(data.Ptr + position, data.Length - position)
        {
            _buffer = data;
        }

        public unsafe YARGTextContainer(TChar* data, long length)
        {
            unsafe
            {
                Position = data;
                Next = data;
                End = data + length;
            }
        }

        public YARGTextContainer<TChar> Clone()
        {
            return new YARGTextContainer<TChar>(this);
        }

        private YARGTextContainer(YARGTextContainer<TChar> other)
        {
            if (other._buffer != null)
                _buffer = other._buffer.Clone();
            else
            {
                _counter = other._counter;
                _counter?.Increment();
            }

            unsafe
            {
                Position = other.Position;
                Next = other.Next;
                End = other.End;
            }
        }

        /// <summary>
        /// Returns whether the character at the current position matches the value
        /// provided in the paramater
        /// </summary>
        /// <param name="cmp">Character to test against</param>
        /// <returns>Result of the Equals operation</returns>
        /// <exception cref="InvalidOperationException">If the position is out of range</exception>
        public bool IsCurrentCharacter(char cmp)
        {
            unsafe
            {
                if (Position < End)
                    return Position->ToChar(null).Equals(cmp);
                throw new InvalidOperationException("At or past end of buffer");
            }
        }

        /// <summary>
        /// Returns whether the position of the container is at or surpasses the end
        /// </summary>
        /// <returns>... Probably obvious</returns>
        public bool IsAtEnd()
        {
            unsafe
            {
                return Position >= End;
            }
        }

        private const char LAST_DIGIT_SIGNED = '7';
        private const char LAST_DIGIT_UNSIGNED = '5';

        private const short SHORT_MAX = short.MaxValue / 10;
        public bool ExtractInt16(out short value)
        {
            bool result = InternalExtractSigned(out long tmp, short.MaxValue, short.MinValue, SHORT_MAX);
            value = (short)tmp;
            return result;
        }

        private const int INT_MAX = int.MaxValue / 10;
        public bool ExtractInt32(out int value)
        {
            bool result = InternalExtractSigned(out long tmp, int.MaxValue, int.MinValue, INT_MAX);
            value = (int)tmp;
            return result;
        }

        private const long LONG_MAX = long.MaxValue / 10;
        public bool ExtractInt64(out long value)
        {
            return InternalExtractSigned(out value, long.MaxValue, long.MinValue, LONG_MAX);
        }

        private const ushort USHORT_MAX = ushort.MaxValue / 10;
        public bool ExtractUInt16(out ushort value)
        {
            bool result = InternalExtractUnsigned(out ulong tmp, ushort.MaxValue, USHORT_MAX);
            value = (ushort) tmp;
            return result;
        }

        private const uint UINT_MAX = uint.MaxValue / 10;
        public bool ExtractUInt32(out uint value)
        {
            bool result = InternalExtractUnsigned(out ulong tmp, uint.MaxValue, UINT_MAX);
            value = (uint) tmp;
            return result;
        }

        private const ulong ULONG_MAX = ulong.MaxValue / 10;
        public bool ExtractUInt64(out ulong value)
        {
            return InternalExtractUnsigned(out value, ulong.MaxValue, ULONG_MAX);
        }

        public bool ExtractFloat(out float value)
        {
            bool result = ExtractDouble(out double tmp);
            value = (float) tmp;
            return result;
        }

        public bool ExtractDouble(out double value)
        {
            value = 0;
            unsafe
            {
                if (Position >= Next)
                    return false;

                char ch = Position->ToChar(null);
                double sign = ch == '-' ? -1 : 1;
                if (ch == '-' || ch == '+')
                {
                    ++Position;
                    if (Position == Next)
                        return false;

                    ch = Position->ToChar(null);
                }

                if (!ch.IsAsciiDigit() && ch != '.')
                    return false;

                while (ch.IsAsciiDigit())
                {
                    value *= 10;
                    value += ch - '0';

                    if (++Position < Next)
                        ch = Position->ToChar(null);
                    else
                        break;
                }

                if (ch == '.')
                {
                    if (++Position < Next)
                    {
                        double divisor = 1;
                        ch = Position->ToChar(null);
                        while (ch.IsAsciiDigit())
                        {
                            divisor *= 10;
                            value += (ch - '0') / divisor;

                            if (++Position < Next)
                                ch = Position->ToChar(null);
                            else
                                break;
                        }
                    }
                }
                value *= sign;
            }
            return true;
        }

        public bool ExtractBoolean()
        {
            unsafe
            {
                return Position < Next && Position->ToChar(null) switch
                {
                    '1' => true,
                    't' or
                    'T' => Position + 4 <= Next &&
                        (Position[1].ToChar(null).ToAsciiLower() == 'r') &&
                        (Position[2].ToChar(null).ToAsciiLower() == 'u') &&
                        (Position[3].ToChar(null).ToAsciiLower() == 'e'),
                    _ => false
                };
            }
        }

        public short ExtractInt16()
        {
            if (ExtractInt16(out short value))
                return value;
            throw new Exception("Data for Int16 not present");
        }

        public ushort ExtractUInt16()
        {
            if (ExtractUInt16(out ushort value))
                return value;
            throw new Exception("Data for UInt16 not present");
        }

        public int ExtractInt32()
        {
            if (ExtractInt32(out int value))
                return value;
            throw new Exception("Data for Int32 not present");
        }

        public uint ExtractUInt32()
        {
            if (ExtractUInt32(out uint value))
                return value;
            throw new Exception("Data for UInt32 not present");
        }

        public long ExtractInt64()
        {
            if (ExtractInt64(out long value))
                return value;
            throw new Exception("Data for Int64 not present");
        }

        public ulong ExtractUInt64()
        {
            if (ExtractUInt64(out ulong value))
                return value;
            throw new Exception("Data for UInt64 not present");
        }

        public float ExtractFloat()
        {
            if (ExtractFloat(out float value))
                return value;
            throw new Exception("Data for Float not present");
        }

        public double ExtractDouble()
        {
            if (ExtractDouble(out double value))
                return value;
            throw new Exception("Data for Double not present");
        }

        private void SkipDigits()
        {
            unsafe
            {
                while (Position < Next)
                {
                    char ch = Position->ToChar(null);
                    if (!ch.IsAsciiDigit())
                        break;
                    ++Position;
                }
            }
        }

        private bool InternalExtractSigned(out long value, long hardMax, long hardMin, long softMax)
        {
            value = 0;
            unsafe
            {
                if (Position >= Next)
                    return false;

                char ch = Position->ToChar(null);
                long sign = 1;
                switch (ch)
                {
                    case '-':
                        sign = -1;
                        goto case '+';
                    case '+':
                        if (++Position == Next)
                            return false;

                        ch = Position->ToChar(null);
                        break;
                }

                if (!ch.IsAsciiDigit())
                    return false;

                while (true)
                {
                    value += ch - '0';
                    if (++Position < Next)
                    {
                        ch = Position->ToChar(null);
                        if (ch.IsAsciiDigit())
                        {
                            if (value < softMax || value == softMax && ch <= LAST_DIGIT_SIGNED)
                            {
                                value *= 10;
                                continue;
                            }
                            value = sign == -1 ? hardMin : hardMax;
                            SkipDigits();
                            return true;
                        }
                    }
                    value *= sign;
                    return true;
                }
            }
        }

        private bool InternalExtractUnsigned(out ulong value, ulong hardMax, ulong softMax)
        {
            value = 0;
            unsafe
            {
                if (Position >= Next)
                    return false;

                char ch = Position->ToChar(null);
                if (ch == '+')
                {
                    if (++Position == Next)
                        return false;

                    ch = Position->ToChar(null);
                }

                if (!ch.IsAsciiDigit())
                    return false;

                while (true)
                {
                    value += (ulong) (ch - '0');
                    if (++Position < Next)
                    {
                        ch = Position->ToChar(null);
                        if (ch.IsAsciiDigit())
                        {
                            if (value < softMax || value == softMax && ch <= LAST_DIGIT_UNSIGNED)
                            {
                                value *= 10;
                                continue;
                            }
                            value = hardMax;
                            SkipDigits();
                        }
                    }
                    break;
                }
            }
            return true;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _counter?.Dispose();
                _buffer?.Dispose();
                _disposed = true;
            }
        }

        ~YARGTextContainer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
