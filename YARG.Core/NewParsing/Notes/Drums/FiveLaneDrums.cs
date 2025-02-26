using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FiveLaneDrums : IInstrumentNote
    {
        public static readonly unsafe int NUM_LANES = sizeof(LaneArray) / sizeof(DualTime);
        public struct LaneArray
        {
            public DualTime Kick;
            public DualTime Snare;
            public DualTime Yellow;
            public DualTime Blue;
            public DualTime Orange;
            public DualTime Green;
        }

        public static readonly unsafe int NUM_DYNAMICS = sizeof(DynamicsArray) / sizeof(DrumDynamics);
        public struct DynamicsArray
        {
            public DrumDynamics Snare;
            public DrumDynamics Yellow;
            public DrumDynamics Blue;
            public DrumDynamics Orange;
            public DrumDynamics Green;
        }

        public LaneArray     Lanes;
        public DynamicsArray Dynamics;
        public KickState     KickState;
        public bool          IsFlammed;

        public readonly int GetNumActiveLanes()
        {
            int numActive = Lanes.Kick.IsActive() ? 1 : 0;
            numActive += Lanes.Snare  .IsActive() ? 1 : 0;
            numActive += Lanes.Yellow .IsActive() ? 1 : 0;
            numActive += Lanes.Blue   .IsActive() ? 1 : 0;
            numActive += Lanes.Orange .IsActive() ? 1 : 0;
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
            if (Lanes.Orange > sustain)
            {
                sustain = Lanes.Orange;
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
                    builder.Append($"DoubleBass: {Lanes.Kick.Ticks} | ");
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
                if (Dynamics.Yellow != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Yellow})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Blue.IsActive())
            {
                builder.Append($"Blue: {Lanes.Blue.Ticks}");
                if (Dynamics.Blue != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Blue})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Orange.IsActive())
            {
                builder.Append($"Orange: {Lanes.Orange.Ticks}");
                if (Dynamics.Orange != DrumDynamics.None)
                {
                    builder.Append($"({Dynamics.Orange})");
                }
                builder.Append(" | ");
            }
            if (Lanes.Green.IsActive())
            {
                builder.Append($"Green: {Lanes.Green.Ticks}");
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
