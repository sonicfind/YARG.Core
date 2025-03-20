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
        public readonly bool           HasFretLeniency;
        public          long           OverdriveIndex;
        public          GuitarLaneMask LaneMask;
        public          int            LaneCount;

        public GuitarSustain(in DualTime endTime, bool hasFretLeniency, long overdriveIndex)
        {
            EndTime = endTime;
            OverdriveIndex = overdriveIndex;
            HasFretLeniency = hasFretLeniency;
            LaneMask = 0;
            LaneCount = 0;
        }
    }
}