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

    public struct ProGuitarNote<TProFret> : IInstrumentNote
        where TProFret : unmanaged, IProFret
    {
        public ProGuitarString<TProFret> String_1;
        public ProGuitarString<TProFret> String_2;
        public ProGuitarString<TProFret> String_3;
        public ProGuitarString<TProFret> String_4;
        public ProGuitarString<TProFret> String_5;
        public ProGuitarString<TProFret> String_6;

        public bool HOPO;
        public ProSlide Slide;
        public EmphasisType Emphasis;

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
            var sustain = DualTime.Zero;
            if (String_1.Fret.Value >= 0 && String_1.Duration > sustain)
            {
                sustain = String_1.Duration;
            }
            if (String_2.Fret.Value >= 0 && String_2.Duration > sustain)
            {
                sustain = String_2.Duration;
            }
            if (String_3.Fret.Value >= 0 && String_3.Duration > sustain)
            {
                sustain = String_3.Duration;
            }
            if (String_4.Fret.Value >= 0 && String_4.Duration > sustain)
            {
                sustain = String_4.Duration;
            }
            if (String_5.Fret.Value >= 0 && String_5.Duration > sustain)
            {
                sustain = String_5.Duration;
            }
            if (String_6.Fret.Value >= 0 && String_6.Duration > sustain)
            {
                sustain = String_6.Duration;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (String_1.IsActive())
            {
                stringBuilder.Append($"[0]: {String_1} | ");
            }
            if (String_2.IsActive())
            {
                stringBuilder.Append($"[1]: {String_2} | ");
            }
            if (String_3.IsActive())
            {
                stringBuilder.Append($"[2]: {String_3} | ");
            }
            if (String_4.IsActive())
            {
                stringBuilder.Append($"[3]: {String_4} | ");
            }
            if (String_5.IsActive())
            {
                stringBuilder.Append($"[4]: {String_5} | ");
            }
            if (String_6.IsActive())
            {
                stringBuilder.Append($"[5]: {String_6} | ");
            }
            if (HOPO)
            {
                stringBuilder.Append("HOPO | ");
            }
            if (Slide != ProSlide.None)
            {
                stringBuilder.Append($"{Slide} | ");
            }
            if (Emphasis != EmphasisType.None)
            {
                stringBuilder.Append(Emphasis.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}
