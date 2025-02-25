using System.Text;

namespace YARG.Core.NewParsing
{
    public struct SixFret : IGuitarConfig<SixFret>
    {
        public DualTime Open;
        public DualTime Black1;
        public DualTime Black2;
        public DualTime Black3;
        public DualTime White1;
        public DualTime White2;
        public DualTime White3;

        public readonly int GetNumActiveLanes()
        {
            var numActive = Open.IsActive() ? 1 : 0;
            numActive += Black1  .IsActive() ? 1 : 0;
            numActive += Black2  .IsActive() ? 1 : 0;
            numActive += Black3  .IsActive() ? 1 : 0;
            numActive += White1  .IsActive() ? 1 : 0;
            numActive += White2  .IsActive() ? 1 : 0;
            numActive += White3  .IsActive() ? 1 : 0;
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
            var builder = new StringBuilder();
            if (Open.IsActive())
            {
                builder.Append($"Open: {Open.Ticks}");
            }
            if (Black1.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Black1: {Black1.Ticks}");
            }
            if (Black2.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Black2: {Black2.Ticks}");
            }
            if (Black3.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"Black3: {Black3.Ticks}");
            }
            if (White1.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"White1: {White1.Ticks}");
            }
            if (White2.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"White2: {White2.Ticks}");
            }
            if (White3.IsActive())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
                builder.Append($"White3: {White3.Ticks}");
            }
            return builder.ToString();
        }
    }
}
