using System.Runtime.CompilerServices;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct UnknownLaneDrums : IInstrumentNote
    {
        public FourLaneDrums FourLane;
        public DualTime      FifthLane;
        public DrumDynamics  FifthDynamics;

        public readonly int GetNumActiveLanes()
        {
            int numActive = FourLane.GetNumActiveLanes();
            numActive += FifthLane.IsActive() ? 1 : 0;
            return numActive;
        }

        public readonly DualTime GetLongestSustain()
        {
            var sustain = FourLane.GetLongestSustain();
            if (FifthLane > sustain)
            {
                sustain = FifthLane;
            }
            return sustain;
        }

        public readonly override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(FourLane.ToString());
            if (FifthLane.IsActive())
            {
                builder.Append($"Green: {FifthLane.Ticks}");
                if (FifthDynamics != DrumDynamics.None)
                {
                    builder.Append($"({FifthDynamics})");
                }
                builder.Append(" | ");
            }
            return builder.ToString();
        }
    }
}
