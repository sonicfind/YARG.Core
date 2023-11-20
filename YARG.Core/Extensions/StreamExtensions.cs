using System;
using System.Buffers.Binary;
using System.IO;

namespace YARG.Core.Extensions
{
    public static class StreamExtensions
    {
        #region Stream
        public static TType ReadLE<TType>(this Stream stream)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = stackalloc byte[sizeof(TType)];
                if (stream.Read(new Span<byte>(buffer, sizeof(TType))) != sizeof(TType))
                    throw new EndOfStreamException($"Not enough data in the stream to read {typeof(TType)}!");

                // Checks System endianness
                if (!BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                return *(TType*) buffer;
            }
        }

        public static TType ReadBE<TType>(this Stream stream)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = stackalloc byte[sizeof(TType)];
                if (stream.Read(new Span<byte>(buffer, sizeof(TType))) != sizeof(TType))
                    throw new EndOfStreamException($"Not enough data in the stream to read {typeof(TType)}!");

                // Checks System endianness
                if (BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                return *(TType*) buffer;
            }
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, length) != length)
                throw new EndOfStreamException($"Not enough data in the stream to read {length} bytes!");

            return buffer;
        }

        public static void WriteLE<TType>(this Stream stream, TType value)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = (byte*) &value;
                if (!BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                stream.Write(new Span<byte>(buffer, sizeof(TType)));
            }
        }

        public static void WriteBE<TType>(this Stream stream, TType value)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = (byte*) &value;
                if (BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                stream.Write(new Span<byte>(buffer, sizeof(TType)));
            }
        }
        #endregion

        #region BinaryReader
        public static TType ReadLE<TType>(this BinaryReader reader)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = stackalloc byte[sizeof(TType)];
                if (reader.Read(new Span<byte>(buffer, sizeof(TType))) != sizeof(TType))
                    throw new EndOfStreamException($"Not enough data in the reader to read {typeof(TType)}!");

                // Checks System endianness
                if (!BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                return *(TType*) buffer;
            }
        }

        public static TType ReadBE<TType>(this BinaryReader stream)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = stackalloc byte[sizeof(TType)];
                if (stream.Read(new Span<byte>(buffer, sizeof(TType))) != sizeof(TType))
                    throw new EndOfStreamException($"Not enough data in the reader to read {typeof(TType)}!");

                // Checks System endianness
                if (BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                return *(TType*) buffer;
            }
        }
        #endregion

        #region BinaryWriter
        public static void WriteLE<TType>(this BinaryWriter writer, TType value)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = (byte*) &value;
                if (!BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                writer.Write(new Span<byte>(buffer, sizeof(TType)));
            }
        }

        public static void WriteBE<TType>(this BinaryWriter writer, TType value)
            where TType : unmanaged, IComparable, IComparable<TType>, IConvertible, IEquatable<TType>, IFormattable
        {
            unsafe
            {
                byte* buffer = (byte*) &value;
                if (BitConverter.IsLittleEndian)
                {
                    int half = sizeof(TType) >> 1;
                    for (int i = 0, j = sizeof(TType) - 1; i < half; ++i, --j)
                        (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
                }
                writer.Write(new Span<byte>(buffer, sizeof(TType)));
            }
        }
        #endregion
    }
}
