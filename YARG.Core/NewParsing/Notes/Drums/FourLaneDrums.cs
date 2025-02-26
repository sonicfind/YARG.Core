using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FourLaneDrums : IInstrumentNote
    {
        public static readonly unsafe int NUM_LANES = sizeof(LaneArray) / sizeof(DualTime);
        public struct LaneArray
        {
            public DualTime Kick;
            public DualTime Snare;
            public DualTime Yellow;
            public DualTime Blue;
            public DualTime Green;
        }

        public static readonly unsafe int NUM_DYNAMICS = sizeof(DynamicsArray) / sizeof(DrumDynamics);
        public struct DynamicsArray
        {
            public DrumDynamics Snare;
            public DrumDynamics Yellow;
            public DrumDynamics Blue;
            public DrumDynamics Green;
        }

        // Can't use the division due to potential padding
        public const int NUM_CYMBALS = 3;
        public struct CymbalArray
        {
            public bool Yellow;
            public bool Blue;
            public bool Green;
        }

        public LaneArray     Lanes;
        public DynamicsArray Dynamics;
        public CymbalArray   Cymbals;
        public KickState     KickState;
        public bool          IsFlammed;

        public readonly int GetNumActiveLanes()
        {
            int numActive = Lanes.Kick.IsActive() ? 1 : 0;
            numActive += Lanes.Snare  .IsActive() ? 1 : 0;
            numActive += Lanes.Yellow .IsActive() ? 1 : 0;
            numActive += Lanes.Blue   .IsActive() ? 1 : 0;
            numActive += Lanes.Green  .IsActive() ? 1 : 0;
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = Lanes.Kick;
            if (Lanes.Snare > sustain)
            {
                sustain = Lanes.Snare;
            }
            if (Lanes.Yellow > sustain)
            {
                sustain = Lanes.Yellow;
            }
            if (Lanes.Blue > sustain)
            {
                sustain = Lanes.Blue;
            }
            if (Lanes.Green > sustain)
            {
                sustain = Lanes.Green;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            StringBuilder builder = new();
            if (Lanes.Kick.IsActive())
            {
                if (KickState != KickState.PlusOnly)
                {
                    builder.Append($"Bass: {Lanes.Kick.Ticks} | ");
                }
                else
                {
                    builder.Append($"DoubleKick: {Lanes.Kick.Ticks} | ");
                }
            }
            if (Lanes.Snare.IsActive())
            {
                builder.Append($"Snare: {Lanes.Snare.Ticks}");
                if (Dynamics.Snare != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Snare})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Yellow.IsActive())
            {
                builder.Append($"Yellow: {Lanes.Yellow.Ticks}");
                if (Cymbals.Yellow)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics.Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Yellow})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Blue.IsActive())
            {
                builder.Append($"Blue: {Lanes.Blue.Ticks}");
                if (Cymbals.Blue)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics.Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Blue})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Green.IsActive())
            {
                builder.Append($"Green: {Lanes.Green.Ticks}");
                if (Cymbals.Green)
                {
                    builder.Append("(Cymbal)");
                }
                if (Dynamics.Green != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Green})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }
}
