using System;
using System.Text;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public static class YARGTextLoader
    {
        private static readonly UTF32Encoding UTF32BE = new(true, false);

        public static YARGTextReader<byte, ByteStringDecoder>? TryLoadByteText(DisposableArray<byte> file)
        {
            if ((file[0] == 0xFF && file[1] == 0xFE) || (file[0] == 0xFE && file[1] == 0xFF))
                return null;

            int position = file[0] == 0xEF && file[1] == 0xBB && file[2] == 0xBF ? 3 : 0;
            unsafe
            {
                var container = new YARGTextContainer<byte>(file.Ptr + position, file.Length - position);
                return new YARGTextReader<byte, ByteStringDecoder>(container);
            }
        }

        public static YARGTextReader<char, CharStringDecoder> LoadCharText(DisposableArray<byte> file)
        {
            int offset, charSize;
            Encoding encoding;

            // Note: anything other than a basic cast will require a new buffer of data
            // Modifying the data provided to the function would effect the song hash

            if (file[2] != 0)
            {
                // UTF-16 encoding, endian-correct, so we can use a basic cast
                if ((file[0] == 0xFF) == BitConverter.IsLittleEndian)
                {
                    unsafe
                    {
                        // Using declaration here would be malformed
                        var container = new YARGTextContainer<char>((char*)(file.Ptr + 2), (file.Length - 2) >> 1);
                        return new YARGTextReader<char, CharStringDecoder>(container);
                    }
                }

                offset = 2;
                charSize = 2;
                // Reaching here means we need to flip the endian
                encoding = BitConverter.IsLittleEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
            }
            else
            {
                offset = 3;
                charSize = 4;
                encoding = file[0] == 0xFF ? Encoding.UTF32 : UTF32BE;
            }

            int bytes = file.Length - offset;
            // Using declaration to ensure we clear the memory on any error
            using DisposableArray<char> charData = new(bytes / charSize);
            unsafe
            {
                encoding.GetChars(file.Ptr + offset, bytes, charData.Ptr, charData.Length);
            }
            // Clone to counteract the "using" declaration
            return new YARGTextReader<char, CharStringDecoder>(new YARGTextContainer<char>(charData.Clone()));
        }
    }

    public static class YARGTextReader
    {
        /// <summary>
        /// Skips all whitespace starting at the current position of the provided container,
        /// until the end of the current line.
        /// </summary>
        /// <remarks>"=" is included as a whitespace character; "\n" is not</remarks>
        /// <typeparam name="TChar">Type of data contained</typeparam>
        /// <param name="container">Buffer of data</param>
        /// <returns>The current character that halted skipping, or 0 if at EoF</returns>
        public static char SkipWhitespace<TChar>(YARGTextContainer<TChar> container)
            where TChar : unmanaged, IConvertible
        {
            unsafe
            {
                while (container.Position < container.End)
                {
                    char ch = container.Position->ToChar(null);
                    if (ch.IsAsciiWhitespace())
                    {
                        if (ch == '\n')
                            return '\n';
                    }
                    else if (ch != '=')
                        return ch;
                    ++container.Position;
                }
            }
            return (char) 0;
        }
    }

    public sealed class YARGTextReader<TChar, TDecoder> : IDisposable
        where TChar : unmanaged, IConvertible
        where TDecoder : IStringDecoder<TChar>, new()
    {
        private readonly TDecoder decoder = new();
        public readonly YARGTextContainer<TChar> Container;

        public YARGTextReader(YARGTextContainer<TChar> container)
        {
            Container = container;
            SkipWhitespace();
            SetNextPointer();
            unsafe
            {
                if (Container.Position->ToChar(null) == '\n')
                    GotoNextLine();
            }
        }

        public char SkipWhitespace()
        {
            return YARGTextReader.SkipWhitespace(Container);
        }

        public void GotoNextLine()
        {
            char curr;
            unsafe
            {
                do
                {
                    Container.Position = Container.Next;
                    if (Container.Position >= Container.End)
                        break;

                    ++Container.Position;
                    curr = SkipWhitespace();
                    if (Container.Position == Container.End)
                        break;

                    if (curr == '{')
                    {
                        ++Container.Position;
                        curr = SkipWhitespace();
                    }

                    SetNextPointer();
                } while (curr == '\n' || curr == '/' && CheckNextCharacter(Container, '/'));
            }
        }

        private static bool CheckNextCharacter(YARGTextContainer<TChar> container, char cmp)
        {
            unsafe
            {
                if (container.Position + 1 < container.End)
                    return container.Position[1].ToChar(null) == cmp;
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Jumps the reader to the line that has the character at the start.
        /// </summary>
        /// <remarks>
        /// If no line of such exists after the reader's current position,
        /// the current position will be set to the end of the file.
        /// </remarks>
        /// <param name="stopCharacter">Character to search for</param>
        public void SkipLinesUntil(char stopCharacter)
        {
            GotoNextLine();
            unsafe
            {
                while (Container.Position < Container.End)
                {
                    if (Container.Position->ToChar(null) == stopCharacter)
                    {
                        // Checks that the character starts the line
                        var point = Container.Position - 1;
                        while (point > Container.Position)
                        {
                            char character = point->ToChar(null);
                            if (!character.IsAsciiWhitespace() || character == '\n')
                                break;
                            --point;
                        }

                        if (point->ToChar(null) == '\n')
                            break;
                    }
                    ++Container.Position;
                }
            }
            SetNextPointer();
        }

        private void SetNextPointer()
        {
            unsafe
            {
                var next = Container.Position;
                while (next < Container.End && next->ToChar(null) != '\n')
                    ++next;
                Container.Next = next;
            }
        }

        public string ExtractModifierName()
        {
            unsafe
            {
                var start = Container.Position;
                while (Container.Position < Container.End)
                {
                    char b = Container.Position->ToChar(null);
                    if (b.IsAsciiWhitespace() || b == '=')
                        break;
                    ++Container.Position;
                }

                int length = (int) (Container.Position - start);
                SkipWhitespace();
                return decoder.Decode(start, length);
            }
        }

        /// <summary>
        /// Grabs the string representation of the current line (starting at the current position)
        /// </summary>
        /// <returns>The line</returns>
        public string PeekLine()
        {
            unsafe
            {
                return decoder.Decode(Container.Position, (int) (Container.Next - Container.Position)).TrimEnd();
            }
        }


        /// <summary>
        /// Extracts a string of text from a modifier or event (removing quotes if appropriate)
        /// </summary>
        /// <param name="isChartFile">Determines whether to remove leading and following quotation marks; True = yes</param>
        /// <returns>The resulting text</returns>
        public string ExtractText(bool isChartFile)
        {
            unsafe
            {
                var stringBegin = Container.Position;
                var stringEnd = Container.Next;
                if (stringEnd[-1].ToChar(null) == '\r')
                    --stringEnd;

                if (isChartFile && Container.Position->ToChar(null) == '\"')
                {
                    var end = stringEnd - 1;
                    while (Container.Position + 1 < end && end->ToChar(null).IsAsciiWhitespace())
                        --end;

                    if (Container.Position < end && end->ToChar(null) == '\"' && end[-1].ToChar(null) != '\\')
                    {
                        ++stringBegin;
                        stringEnd = end;
                    }
                }

                if (stringEnd < stringBegin)
                    return string.Empty;

                while (stringBegin < stringEnd && stringEnd[-1].ToChar(null).IsAsciiWhitespace())
                    --stringEnd;

                Container.Position = Container.Next;
                return decoder.Decode(stringBegin, (int) (stringEnd - stringBegin));
            }
        }

        /// <summary>
        /// Extracts a boolean and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The boolean</returns>
        public bool ExtractBoolean()
        {
            bool result = Container.ExtractBoolean();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a short and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The short</returns>
        public short ExtractInt16()
        {
            short result = Container.ExtractInt16();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a ushort and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The ushort</returns>
        public ushort ExtractUInt16()
        {
            ushort result = Container.ExtractUInt16();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a int and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The int</returns>
        public int ExtractInt32()
        {
            int result = Container.ExtractInt32();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a uint and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The uint</returns>
        public uint ExtractUInt32()
        {
            uint result = Container.ExtractUInt32();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a long and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The long</returns>
        public long ExtractInt64()
        {
            long result = Container.ExtractInt64();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a ulong and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The ulong</returns>
        public ulong ExtractUInt64()
        {
            ulong result = Container.ExtractUInt64();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a float and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The float</returns>
        public float ExtractFloat()
        {
            float result = Container.ExtractFloat();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Extracts a double and skips the following whitespace
        /// </summary>
        /// <remarks>Throws if no value could be parsed</remarks>
        /// <returns>The double</returns>
        public double ExtractDouble()
        {
            double result = Container.ExtractDouble();
            SkipWhitespace();
            return result;
        }

        /// <summary>
        /// Attempts to extracts a short and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractInt16(out short value)
        {
            if (!Container.ExtractInt16(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a ushort and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractUInt16(out ushort value)
        {
            if (!Container.ExtractUInt16(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts an int and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractInt32(out int value)
        {
            if (!Container.ExtractInt32(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a uint and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractUInt32(out uint value)
        {
            if (!Container.ExtractUInt32(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a long and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractInt64(out long value)
        {
            if (!Container.ExtractInt64(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a ulong and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractUInt64(out ulong value)
        {
            if (!Container.ExtractUInt64(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a float and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractFloat(out float value)
        {
            if (!Container.ExtractFloat(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        /// <summary>
        /// Attempts to extracts a double and skip following whitespace
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <returns>Whether a value was extracted</returns>
        public bool ExtractDouble(out double value)
        {
            if (!Container.ExtractDouble(out value))
                return false;
            SkipWhitespace();
            return true;
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
