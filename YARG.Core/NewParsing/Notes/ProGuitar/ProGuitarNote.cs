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

        public int GetNumActiveLanes()
        {
            int numActive = 0;
            unsafe
            {
                fixed (ProGuitarString<TProFretConfig>* strings = &String_1)
                {
                    for (int i = 0; i < NUMSTRINGS; ++i)
                    {
                        bool active = strings[i].IsActive();
                        numActive += Unsafe.As<bool, byte>(ref active);
                    }
                }
            }
            return numActive;
        }

        public DualTime GetLongestSustain()
        {
            DualTime sustain = default;
            unsafe
            {
                fixed (ProGuitarString<TProFretConfig>* strings = &String_2)
                {
                    for (int i = 0; i < NUMSTRINGS; ++i)
                    {
                        ref var str = ref strings[i];
                        if (str.Fret >= 0)
                        {
                            var dur = strings[i].Duration;
                            if (dur > sustain)
                                sustain = dur;
                        }
                    }
                }
            }
            return sustain;
        }
    }
}
