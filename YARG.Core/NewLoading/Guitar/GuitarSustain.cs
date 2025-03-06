using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum SustainState
    {
        Idle,
        Held,
        Dropped,
    }

    public struct GuitarSustain
    {
        public readonly DualTime       EndTime;
        public readonly long           OverdriveIndex;
        public readonly bool           HasFretLeniency;
        public          GuitarLaneMask LaneMask;
        public          int            LaneCount;
        public          SustainState   State;

        public GuitarSustain(in DualTime endTime, long overdriveIndex, bool hasFretLeniency)
        {
            EndTime = endTime;
            OverdriveIndex = overdriveIndex;
            HasFretLeniency = hasFretLeniency;
            LaneMask = 0;
            LaneCount = 0;
            State = SustainState.Idle;
        }
    }
}