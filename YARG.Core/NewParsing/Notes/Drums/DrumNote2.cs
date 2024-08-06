using System;
using System.Runtime.CompilerServices;
using System.Text;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public enum DrumDynamics
    {
        None,
        Accent,
        Ghost
    }

    public struct FourLaneDrums : IInstrumentNote
    {
        public bool IsDoubleBass;
        public bool IsFlammed;
        public DualTime Bass;
        public DualTime Snare;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Green;
        public DrumDynamics Dynamics_Snare;
        public DrumDynamics Dynamics_Yellow;
        public DrumDynamics Dynamics_Blue;
        public DrumDynamics Dynamics_Green;
        public bool Cymbal_Yellow;
        public bool Cymbal_Blue;
        public bool Cymbal_Green;

        public readonly int NUMLANES => 5;

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Bass.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Snare.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Bass;
            if (Snare > sustain)
            {
                sustain = Snare;
            }
            if (Yellow > sustain)
            {
                sustain = Yellow;
            }
            if (Blue > sustain)
            {
                sustain = Blue;
            }
            if (Green > sustain)
            {
                sustain = Green;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Bass.IsActive())
            {
                if (!IsDoubleBass)
                {
                    builder.Append($"Bass: {Bass.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Bass.Ticks} | ");
                }
            }
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Ticks}");
                if (Dynamics_Snare != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Snare})");
                }
                builder.Append(" | ");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Ticks}");
                if (Cymbal_Yellow)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Yellow})");
                }
                builder.Append(" | ");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Ticks}");
                if (Cymbal_Blue)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Blue})");
                }
                builder.Append(" | ");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Ticks}");
                if (Cymbal_Green)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Green != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Green})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }

    public struct FiveLaneDrums : IInstrumentNote
    {
        public bool IsDoubleBass;
        public bool IsFlammed;
        public DualTime Bass;
        public DualTime Snare;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public DualTime Green;
        public DrumDynamics Dynamics_Snare;
        public DrumDynamics Dynamics_Yellow;
        public DrumDynamics Dynamics_Blue;
        public DrumDynamics Dynamics_Orange;
        public DrumDynamics Dynamics_Green;

        public readonly int NUMLANES => 6;

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Bass.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Snare.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Orange.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Bass;
            if (Snare > sustain)
            {
                sustain = Snare;
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
            if (Green > sustain)
            {
                sustain = Green;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Bass.IsActive())
            {
                if (!IsDoubleBass)
                {
                    builder.Append($"Bass: {Bass.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Bass.Ticks} | ");
                }
            }
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Ticks}");
                if (Dynamics_Snare != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Snare})");
                }
                builder.Append(" | ");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Ticks}");
                if (Dynamics_Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Yellow})");
                }
                builder.Append(" | ");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Ticks}");
                if (Dynamics_Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Blue})");
                }
                builder.Append(" | ");
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange.Ticks}");
                if (Dynamics_Orange != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Orange})");
                }
                builder.Append(" | ");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Ticks}");
                if (Dynamics_Green != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Green})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }

    public struct UnknownLaneDrums : IInstrumentNote
    {
        public bool IsDoubleBass;
        public bool IsFlammed;
        public DualTime Bass;
        public DualTime Snare;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public DrumDynamics Dynamics_Snare;
        public DrumDynamics Dynamics_Yellow;
        public DrumDynamics Dynamics_Blue;
        public DrumDynamics Dynamics_Orange;
        public bool Cymbal_Yellow;
        public bool Cymbal_Blue;
        public bool Cymbal_Orange;
        // Placed at the bottom to allowed best blittability with FourLane
        // as that will be the most common conversion
        public DualTime Green;
        public DrumDynamics Dynamics_Green;

        public readonly int NUMLANES => 6;

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Bass.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Snare.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Yellow.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Blue.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Orange.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            state = Green.IsActive();
            numActive += Unsafe.As<bool, byte>(ref state);
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Bass;
            if (Snare > sustain)
            {
                sustain = Snare;
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
            if (Green > sustain)
            {
                sustain = Green;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Bass.IsActive())
            {
                if (!IsDoubleBass)
                {
                    builder.Append($"Bass: {Bass.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Bass.Ticks} | ");
                }
            }
            if (Snare.IsActive())
            {
                builder.Append($"Snare: {Snare.Ticks}");
                if (Dynamics_Snare != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Snare})");
                }
                builder.Append(" | ");
            }
            if (Yellow.IsActive())
            {
                builder.Append($"Yellow: {Yellow.Ticks}");
                if (Cymbal_Yellow)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Yellow})");
                }
                builder.Append(" | ");
            }
            if (Blue.IsActive())
            {
                builder.Append($"Blue: {Blue.Ticks}");
                if (Cymbal_Blue)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Blue})");
                }
                builder.Append(" | ");
            }
            if (Orange.IsActive())
            {
                builder.Append($"Orange: {Orange.Ticks}");
                if (Cymbal_Orange)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics_Orange != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Orange})");
                }
                builder.Append(" | ");
            }
            if (Green.IsActive())
            {
                builder.Append($"Green: {Green.Ticks}");
                if (Dynamics_Green != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics_Green})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }
}
