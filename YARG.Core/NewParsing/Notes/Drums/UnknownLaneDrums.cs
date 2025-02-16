using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct UnknownLaneDrums : IInstrumentNote
    {
        public DualTime Kick;
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
        public KickState KickState;
        public bool IsFlammed;
        // Placed at the bottom to allow best blittability with FourLane
        // as that will be the most common conversion
        public DualTime Green;
        public DrumDynamics Dynamics_Green;

        public readonly int GetNumActiveLanes()
        {
            int numActive = 0;
            bool state = Kick.IsActive();
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
            var sustain = Kick;
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
            if (Kick.IsActive())
            {
                if (KickState != KickState.PlusOnly)
                {
                    builder.Append($"Bass: {Kick.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleBass: {Kick.Ticks} | ");
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
