using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum ProSlide
    {
        None,
        Normal,
        Reversed
    };

    public enum EmphasisType
    {
        None,
        High,
        Middle,
        Low
    };

    public struct ProGuitarNote<TProFretConfig> : IInstrumentNote
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        public const int NUMSTRINGS = 6;

        public ProGuitarString<TProFretConfig> String_1;
        public ProGuitarString<TProFretConfig> String_2;
        public ProGuitarString<TProFretConfig> String_3;
        public ProGuitarString<TProFretConfig> String_4;
        public ProGuitarString<TProFretConfig> String_5;
        public ProGuitarString<TProFretConfig> String_6;

        public bool HOPO;
        public bool ForceNumbering;
        public ProSlide Slide;
        public EmphasisType Emphasis;

        public ref ProGuitarString<TProFretConfig> this[int lane]
        {
            get
            {
                if (lane < 0 || 6 <= lane)
                {
                    throw new ArgumentOutOfRangeException(nameof(lane));
                }

                unsafe
                {
                    fixed (ProGuitarString<TProFretConfig>* strings = &String_1)
                    {
                        return ref strings[lane];
                    }
                }
            }
        }

        public ProSlide WheelSlide()
        {
            Slide = Slide switch
            {
                ProSlide.None => ProSlide.Normal,
                ProSlide.Normal => ProSlide.Reversed,
                _ => ProSlide.None,
            };
            return Slide;
        }

        public EmphasisType WheelEmphasis()
        {
            Emphasis = Emphasis switch
            {
                EmphasisType.None => EmphasisType.High,
                EmphasisType.High => EmphasisType.Middle,
                EmphasisType.Middle => EmphasisType.Low,
                _ => EmphasisType.None,
            };
            return Emphasis;
        }

        public readonly unsafe int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = String_1.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = String_2.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = String_3.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = String_4.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = String_5.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = String_6.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            DualTime sustain = default;
            if (String_1.Fret >= 0 && String_1.Duration > sustain)
            {
                sustain = String_1.Duration;
            }
            if (String_2.Fret >= 0 && String_2.Duration > sustain)
            {
                sustain = String_2.Duration;
            }
            if (String_3.Fret >= 0 && String_3.Duration > sustain)
            {
                sustain = String_3.Duration;
            }
            if (String_4.Fret >= 0 && String_4.Duration > sustain)
            {
                sustain = String_4.Duration;
            }
            if (String_5.Fret >= 0 && String_5.Duration > sustain)
            {
                sustain = String_5.Duration;
            }
            if (String_6.Fret >= 0 && String_6.Duration > sustain)
            {
                sustain = String_6.Duration;
            }
            return sustain;
        }
    }
}
