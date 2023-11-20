using System;
using System.Text;
using YARG.Core.Extensions;

namespace YARG.Core.IO
{
    public sealed class ByteStringDecoder : IStringDecoder<byte>
    {
        private static readonly UTF8Encoding UTF8 = new(true, true);
        private Encoding encoding = UTF8;

        public unsafe string Decode(byte* start, int length)
        {
            try
            {
                return encoding.GetString(start, length);
            }
            catch
            {
                encoding = YARGTextContainer.Latin1;
                return encoding.GetString(start, length);
            }
        }
    }

    public struct CharStringDecoder : IStringDecoder<char>
    {
        public unsafe string Decode(char* start, int length)
        {
            return new string(start, 0, length);
        }
    }

    public interface IStringDecoder<TChar>
        where TChar : unmanaged, IConvertible
    {
        public unsafe string Decode(TChar* start, int length);
    } 
}
