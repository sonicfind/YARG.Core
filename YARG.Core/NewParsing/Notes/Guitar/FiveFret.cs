using System.Text;

namespace YARG.Core.NewParsing
{
    public struct FiveFret : IGuitarConfig
    {
        public int MAX_LANES => 6;

        public DualTime Open;
        public DualTime Green;
        public DualTime Red;
        public DualTime Yellow;
        public DualTime Blue;
        public DualTime Orange;

        public readonly int GetNumActiveLanes()
        {
            var numActive = Open.IsActive() ? 1 : 0;
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
            var builder = new StringBuilder();
            if (Open.IsActive())
            {
                builder.Append($"Open: {Open.Ticks}");
            }
            if (Green.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Green: {Green.Ticks}");
            }
            if (Red.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Red: {Red.Ticks}");
            }
            if (Yellow.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Yellow: {Yellow.Ticks}");
            }
            if (Blue.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Blue: {Blue.Ticks}");
            }
            if (Orange.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Orange: {Orange.Ticks}");
            }
            return builder.ToString();
        }
    }
}
