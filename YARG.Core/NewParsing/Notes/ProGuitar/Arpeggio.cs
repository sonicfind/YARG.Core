using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct Arpeggio<TFretConfig>
        where TFretConfig : unmanaged, IProFretConfig
    {
        private unsafe fixed int strings[6];
        private DualTime _length;

        public DualTime Length
        {
            readonly get => _length;
            set
            {
                _length = DualTime.Normalize(value);
            }
        }

        public int this[int lane]
        {
            get
            {
                if (lane < 0 || ProGuitarNote<TFretConfig>.NUMSTRINGS <= lane)
                {
                    throw new IndexOutOfRangeException();
                }

                unsafe
                {
                    return strings[lane];
                }
            }

            set
            {
                if (lane < 0 || ProGuitarNote<TFretConfig>.NUMSTRINGS <= lane)
                {
                    throw new IndexOutOfRangeException();
                }

                unsafe
                {
                    strings[lane] = ProGuitarString<TFretConfig>.CONFIG.ValidateFret(lane);
                }
            }
        }
    }
}
