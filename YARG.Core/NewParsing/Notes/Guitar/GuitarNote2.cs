using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum GuitarState
    {
        Natural,
        Forced,
        Hopo,
        Strum,
        Tap
    }

    public struct FiveFretGuitar : IInstrumentNote
    {
        public DualTime Open;
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public GuitarState State;

        public readonly int NUMLANES => 6;

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Open.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Red.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Orange.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Open;
            if (Green > sustain)
            {
                sustain = Green;
            }
            if (Red > sustain)
            {
                sustain = Red;
            }
            if (Yellow > sustain)
            {
                sustain = Yellow;
            }
            if (Blue > sustain)
            {
                sustain = Blue;
            }
            if (Orange > sustain)
            {
                sustain = Orange;
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
            if (Green.IsActive())
            {
                stringBuilder.Append($"Green: {Green.Ticks} | ");
            }
            if (Red.IsActive())
            {
                stringBuilder.Append($"Red: {Red.Ticks} | ");
            }
            if (Yellow.IsActive())
            {
                stringBuilder.Append($"Yellow: {Yellow.Ticks} | ");
            }
            if (Blue.IsActive())
            {
                stringBuilder.Append($"Blue: {Blue.Ticks} | ");
            }
            if (Orange.IsActive())
            {
                stringBuilder.Append($"Orange: {Orange.Ticks}");
            }
            if (State != GuitarState.Natural)
            {
                stringBuilder.Append(State.ToString());
            }
            return stringBuilder.ToString();
        }
    }

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

        public readonly int NUMLANES => 7;

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
