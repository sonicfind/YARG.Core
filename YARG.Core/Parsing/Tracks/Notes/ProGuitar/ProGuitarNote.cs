using System;
using System.Runtime.CompilerServices;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
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

    public struct ProGuitarNote<TFretConfig> : INote
        where TFretConfig : IProFretConfig, new()
    {
        private const int NUMSTRINGS = 6;
        public ProGuitarString<TFretConfig> String_1;
        public ProGuitarString<TFretConfig> String_2;
        public ProGuitarString<TFretConfig> String_3;
        public ProGuitarString<TFretConfig> String_4;
        public ProGuitarString<TFretConfig> String_5;
        public ProGuitarString<TFretConfig> String_6;
        public ref ProGuitarString<TFretConfig> this[int lane]
        {
            get
            {
                if (0 <= lane && lane < 6)
                {
                    unsafe
                    {
                        fixed (ProGuitarString<TFretConfig>* strings = &String_1)
                        {
                            return ref strings[lane];
                        }
                    }
                }
                throw new IndexOutOfRangeException();
            }
        }

        public bool HOPO;
        public bool ForceNumbering;
        public ProSlide Slide;
        public EmphasisType Emphasis;

        public ProSlide WheelSlide()
        {
            if (Slide == ProSlide.None)
                Slide = ProSlide.Normal;
            else if (Slide == ProSlide.Normal)
                Slide = ProSlide.Reversed;
            else
                Slide = ProSlide.None;
            return Slide;
        }

        public EmphasisType WheelEmphasis()
        {
            if (Emphasis == EmphasisType.None)
                Emphasis = EmphasisType.High;
            else if (Emphasis == EmphasisType.High)
                Emphasis = EmphasisType.Middle;
            else if (Emphasis == EmphasisType.Middle)
                Emphasis = EmphasisType.Low;
            else
                Emphasis = EmphasisType.None;
            return Emphasis;
        }

        public int GetNumActiveNotes()
        {
            int numActive = 0;
            unsafe
            {
                fixed (ProGuitarString<TFretConfig>* strings = &String_1)
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

        public long GetLongestSustain()
        {
            long sustain = String_1.Duration;
            unsafe
            {
                fixed (ProGuitarString<TFretConfig>* strings = &String_2)
                {
                    for (int i = 1; i < NUMSTRINGS; ++i)
                    {
                        long dur = strings[i].Duration;
                        if (dur > sustain)
                            sustain = dur;
                    }
                }
            }
            return sustain;
        }
    }
}
