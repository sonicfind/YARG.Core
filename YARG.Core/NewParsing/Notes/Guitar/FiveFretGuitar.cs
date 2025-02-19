using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FiveFretGuitar : IInstrumentNote
    {
        public DualTime Open;
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;
        public GuitarState State;

        public readonly int GetNumActiveLanes()
        {
            int numActive = Open.IsActive() ? 1 : 0;
            numActive += Green  .IsActive() ? 1 : 0;
            numActive += Red    .IsActive() ? 1 : 0;
            numActive += Yellow .IsActive() ? 1 : 0;
            numActive += Blue   .IsActive() ? 1 : 0;
            numActive += Orange .IsActive() ? 1 : 0;
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
}
