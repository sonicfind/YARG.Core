using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct SixFretGuitar : IGuitarNote, IDotChartLoadable
    {
        public DualTime Open;
        public DualTime Black1;
        public DualTime Black2;
        public DualTime Black3;
        public DualTime White1;
        public DualTime White2;
        public DualTime White3;
        private GuitarState _state;

        public GuitarState State
        {
            readonly get => _state;
            set => _state = value;
        }

        public bool SetFromDotChart(int lane, in DualTime length)
        {
            switch (lane)
            {
                case 0: White1 = length; break;
                case 1: White2 = length; break;
                case 2: White3 = length; break;
                case 3: Black1 = length; break;
                case 4: Black2 = length; break;
                case 5:
                    if (_state == GuitarState.Natural)
                    {
                        _state = GuitarState.Forced;
                    }
                    break;
                case 6: _state = GuitarState.Tap; break;
                case 7: Open = length; break;
                case 8: Black3 = length; break;
                default:
                    return false;
            }
            return true;
        }

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Open.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Black1.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Black2.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Black3.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = White1.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = White2.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = White3.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Open;
            if (Black1 > sustain)
            {
                sustain = Black1;
            }
            if (Black2 > sustain)
            {
                sustain = Black2;
            }
            if (Black3 > sustain)
            {
                sustain = Black3;
            }
            if (White1 > sustain)
            {
                sustain = White1;
            }
            if (White2 > sustain)
            {
                sustain = White2;
            }
            if (White3 > sustain)
            {
                sustain = White3;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder stringBuilder = new();
            if (Open.IsActive())
            {
                stringBuilder.Append($"Open: {Open.Ticks} | ");
            }
            if (Black1.IsActive())
            {
                stringBuilder.Append($"Black 1: {Black1.Ticks} | ");
            }
            if (Black2.IsActive())
            {
                stringBuilder.Append($"Black 2: {Black2.Ticks} | ");
            }
            if (Black3.IsActive())
            {
                stringBuilder.Append($"Black 3: {Black3.Ticks} | ");
            }
            if (White1.IsActive())
            {
                stringBuilder.Append($"White 1: {White1.Ticks} | ");
            }
            if (White2.IsActive())
            {
                stringBuilder.Append($"White 2: {White2.Ticks} | ");
            }
            if (White3.IsActive())
            {
                stringBuilder.Append($"White 3: {White3.Ticks}");
            }
            if (_state != GuitarState.Natural)
            {
                stringBuilder.Append(_state.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}
