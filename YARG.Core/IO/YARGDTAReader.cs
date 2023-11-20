using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public sealed class YARGDTAReader : IDisposable
    {
        public static YARGDTAReader? TryCreate(CONFileListing listing, CONFile file)
        {
            try
            {
                using var bytes = listing.LoadAllBytes(file);
                return TryCreate(bytes);
            }
            catch (Exception ex)
            {
                YargTrace.LogException(ex, $"Error while loading {listing.ConFile.FullName}");
                return null;
            }
        }

        public static YARGDTAReader? TryCreate(string filename)
        {
            try
            {
                using var bytes = DisposableArray<byte>.Create(filename);
                return TryCreate(bytes);
            }
            catch (Exception ex)
            {
                YargTrace.LogException(ex, $"Error while loading {filename}");
                return null;
            }
        }

        private static YARGDTAReader? TryCreate(DisposableArray<byte> data)
        {
            if ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF))
            {
                YargTrace.LogError("UTF-16 & UTF-32 are not supported for .dta files");
                return null;
            }

            int position = data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF ? 3 : 0;
            return new YARGDTAReader(data.Clone(), position);
        }

        private readonly YARGTextContainer<byte> container;
        private readonly unsafe byte*[] nodeEnds = new byte*[16];
        private int nodeEndIndex = 0;
        private bool disposedValue;
        public Encoding encoding;

        private YARGDTAReader(DisposableArray<byte> data, int position)
        {
            container = new YARGTextContainer<byte>(data, position);
            encoding = position == 3 ? Encoding.UTF8 : YARGTextContainer.Latin1;
            SkipWhitespace();
        }

        public YARGDTAReader Clone()
        {
            return new YARGDTAReader(this);
        }

        private YARGDTAReader(YARGDTAReader reader)
        {
            container = reader.container.Clone();
            encoding = reader.encoding;
            unsafe
            {
                nodeEnds[0] = container.End;
            }
        }

        public char SkipWhitespace()
        {
            unsafe
            {
                while (container.Position < container.End)
                {
                    char ch = (char) *container.Position;
                    if (!ch.IsAsciiWhitespace() && ch != ';')
                        return ch;

                    ++container.Position;
                    if (!ch.IsAsciiWhitespace())
                    {
                        // In comment
                        while (container.Position < container.End)
                        {
                            if (*container.Position++ == '\n')
                                break;
                        }
                    }
                }
            }
            return (char) 0;
        }

        public string GetNameOfNode()
        {
            unsafe
            {
                char ch = (char) container.Current;
                if (ch == '(')
                    return string.Empty;

                bool hasApostrophe = true;
                if (ch != '\'')
                {
                    if (container.Position[-1] != '(')
                        throw new Exception("Invalid name call");
                    hasApostrophe = false;
                }
                else
                {
                    ++container.Position;
                    ch = (char) container.Current;
                }

                var start = container.Position;
                while (ch != '\'')
                {
                    if (ch.IsAsciiWhitespace())
                    {
                        if (hasApostrophe)
                            throw new Exception("Invalid name format");
                        break;
                    }
                    ++container.Position;
                    ch = (char) container.Current;
                }

                var end = container.Position++;
                SkipWhitespace();
                return Encoding.UTF8.GetString(start, (int) (end - start));
            }
        }

        private enum TextScopeState
        {
            None,
            Squirlies,
            Quotes,
            Apostrophes
        }
        
        public string ExtractText()
        {
            char ch = (char)container.Current;
            var state = ch switch
            {
                '{'  => TextScopeState.Squirlies,
                '\"' => TextScopeState.Quotes,
                '\'' => TextScopeState.Apostrophes,
                _    => TextScopeState.None
            };

            unsafe
            {
                if (state != TextScopeState.None)
                    ++container.Position;

                var start = container.Position++;
                // Loop til the end of the text is found
                while (container.Position < container.Next)
                {
                    ch = (char) *container.Position;
                    if (ch == '{')
                        throw new Exception("Text error - no { braces allowed");

                    if (ch == '}')
                    {
                        if (state == TextScopeState.Squirlies)
                            break;
                        throw new Exception("Text error - no \'}\' allowed");
                    }
                    else if (ch == '\"')
                    {
                        if (state == TextScopeState.Quotes)
                            break;
                        if (state != TextScopeState.Squirlies)
                            throw new Exception("Text error - no quotes allowed");
                    }
                    else if (ch == '\'')
                    {
                        if (state == TextScopeState.Apostrophes)
                            break;
                        if (state == TextScopeState.None)
                            throw new Exception("Text error - no apostrophes allowed");
                    }
                    else if (ch.IsAsciiWhitespace())
                    {
                        if (state == TextScopeState.None)
                            break;
                        if (state == TextScopeState.Apostrophes)
                            throw new Exception("Text error - no whitespace allowed");
                    }
                    ++container.Position;
                }

                var end = container.Position;
                if (container.Position != container.Next)
                {
                    ++container.Position;
                    SkipWhitespace();
                }
                else if (state != TextScopeState.None)
                    throw new Exception("Improper end to text");

                return encoding.GetString(start, (int) (end - start)).Replace("\\q", "\"");
            }
        }

        public List<int> ExtractList_Int()
        {
            List<int> values = new();
            while (container.Current != ')')
                values.Add(ExtractInt32());
            return values;
        }

        public List<float> ExtractList_Float()
        {
            List<float> values = new();
            while (container.Current != ')')
                values.Add(ExtractFloat());
            return values;
        }

        public List<string> ExtractList_String()
        {
            List<string> strings = new();
            while (container.Current != ')')
                strings.Add(ExtractText());
            return strings;
        }

        public bool StartNode()
        {
            unsafe
            {
                if (container.Position == container.End || * container.Position != '(')
                    return false;

                if (nodeEndIndex == nodeEnds.Length - 1)
                    throw new InvalidOperationException("Maximum Scope count exceeded");

                ++container.Position;
                SkipWhitespace();
                
                int scopeLevel = 1;
                bool inApostropes = false;
                bool inQuotes = false;
                bool inComment = false;

                var pos = container.Position;
                while (scopeLevel >= 1 && pos < container.End)
                {
                    if (inComment)
                    {
                        if (*pos == '\n')
                            inComment = false;
                    }
                    else if (*pos == '\"')
                    {
                        if (inApostropes)
                            throw new Exception("Ah hell nah wtf");
                        inQuotes = !inQuotes;
                    }
                    else if (!inQuotes)
                    {
                        if (!inApostropes)
                        {
                            if (*pos == '(')
                                ++scopeLevel;
                            else if (*pos == ')')
                                --scopeLevel;
                            else if (*pos == '\'')
                                inApostropes = true;
                            else if (*pos == ';')
                                inComment = true;
                        }
                        else if (*pos == '\'')
                            inApostropes = false;
                    }
                    ++pos;
                }
                container.Next = nodeEnds[++nodeEndIndex] = pos - 1;
                return true;
            }
        }

        public void EndNode()
        {
            unsafe
            {
                container.Position = container.Next + 1;
                if (nodeEndIndex > 0)
                {
                    container.Next = nodeEnds[--nodeEndIndex];
                    SkipWhitespace();
                }
            }
        }

        public bool ExtractBoolean()
        {
            bool result = container.ExtractBoolean();
            SkipWhitespace();
            return result;
        }

        public short ExtractInt16()
        {
            short result = container.ExtractInt16();
            SkipWhitespace();
            return result;
        }

        public ushort ExtractUInt16()
        {
            ushort result = container.ExtractUInt16();
            SkipWhitespace();
            return result;
        }

        public int ExtractInt32()
        {
            int result = container.ExtractInt32();
            SkipWhitespace();
            return result;
        }
        public uint ExtractUInt32()
        {
            uint result = container.ExtractUInt32();
            SkipWhitespace();
            return result;
        }

        public long ExtractInt64()
        {
            long result = container.ExtractInt64();
            SkipWhitespace();
            return result;
        }

        public ulong ExtractUInt64()
        {
            ulong result = container.ExtractUInt64();
            SkipWhitespace();
            return result;
        }

        public float ExtractFloat()
        {
            float result = container.ExtractFloat();
            SkipWhitespace();
            return result;
        }

        public double ExtractDouble()
        {
            double result = container.ExtractDouble();
            SkipWhitespace();
            return result;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                container.Dispose();
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~YARGDTAReader()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    };
}
