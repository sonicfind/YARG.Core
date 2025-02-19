﻿using System.Text;

namespace YARG.Core.NewParsing
{
    public struct SixFretGuitar : IInstrumentNote
    {
        public DualTime Open;
        public DualTime Black1;
        public DualTime Black2;
        public DualTime Black3;
        public DualTime White1;
        public DualTime White2;
        public DualTime White3;
        public GuitarState State;

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
            if (State != GuitarState.Natural)
            {
                stringBuilder.Append(State.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}
