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

    public interface IGuitarConfig<TConfig> : IInstrumentNote
        where TConfig : unmanaged, IGuitarConfig<TConfig>
    {
        public static readonly unsafe uint MAX_LANES = (uint)(sizeof(TConfig) / sizeof(DualTime));
    }

    public struct GuitarNote<TConfig> : IInstrumentNote
        where TConfig : unmanaged, IGuitarConfig<TConfig>
    {
        public TConfig     Lanes;
        public GuitarState State;

        public readonly int GetNumActiveLanes()
        {
            return Lanes.GetNumActiveLanes();
        }

        public readonly DualTime GetLongestSustain()
        {
            return Lanes.GetLongestSustain();
        }

        public readonly override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Lanes.ToString());
            if (State != GuitarState.Natural)
            {
                builder.Append(", ");
                builder.Append(State.ToString());
            }
            return builder.ToString();
        }
    }
}
