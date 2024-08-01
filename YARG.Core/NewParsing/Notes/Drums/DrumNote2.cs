using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct DrumNote2<TConfig> : IInstrumentNote
        where TConfig : unmanaged, IDrumPadConfig
    {
        public DualTime Bass;
        public bool IsDoubleBass;
        public bool IsFlammed;
        public TConfig Pads;

        public DualTime this[int lane]
        {
            readonly get
            {
                return lane switch
                {
                    0 or
                    1 => Bass,
                    _ => Pads[lane - 2],
                };
            }
            set
            {
                switch (lane)
                {
                    case 0:
                        Bass = value;
                        IsDoubleBass = false;
                        break;
                    case 1:
                        Bass = value;
                        IsDoubleBass = true;
                        break;
                    default:
                        Pads[lane - 2] = value;
                        break;
                }
            }
        }

        public readonly int GetNumActiveLanes()
        {
            int numActive = Bass.IsActive() ? 1 : 0;
            for (int i = 0; i < Pads.NumPads; ++i)
            {
                bool active = Pads[i].IsActive();
                numActive += Unsafe.As<bool, byte>(ref active);
            }
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Bass;
            for (int i = 0; i < Pads.NumPads; ++i)
            {
                var padDuration = Pads[i];
                if (padDuration > sustain)
                {
                    sustain = padDuration;
                }
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Bass.IsActive())
            {
                if (!IsDoubleBass)
                {
                    stringBuilder.Append($"Bass: {Bass.Ticks} | ");
                }
                else
                {
                    stringBuilder.Append($"DoubleBass: {Bass.Ticks} | ");
                }
            }
            stringBuilder.Append(Pads.ToString());
            return stringBuilder.ToString();
        }
    }
}
