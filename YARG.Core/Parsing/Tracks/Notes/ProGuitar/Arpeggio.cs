using System;

namespace YARG.Core.Parsing.ProGuitar
{
    public struct Arpeggio<TFretConfig>
        where TFretConfig : IProFretConfig, new()
    {
        private const int NUMSTRINGS = 6;
        private static readonly TFretConfig CONFIG = new();
        static Arpeggio() { }

        private unsafe fixed int strings[6];
        public DualTime Length;

        public int this[int lane]
        {
            get
            {
                if (0 <= lane && lane < NUMSTRINGS)
                {
                    unsafe
                    {
                        return strings[lane];
                    }
                }
                throw new IndexOutOfRangeException();
            }

            set
            {
                if (0 <= lane && lane < NUMSTRINGS)
                {
                    CONFIG.ThrowIfOutOfRange(value);
                    unsafe
                    {
                        strings[lane] = value;
                    }
                }
                else
                    throw new IndexOutOfRangeException();
            }
        }

    }
}
