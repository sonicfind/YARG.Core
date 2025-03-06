using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct GuitarNoteGroup
    {
        public readonly DualTime       Position;
        public readonly long           OverdriveIndex;
        public readonly long           SoloIndex;
        public          GuitarLaneMask LaneMask;
        public          int            LaneCount;
        public          int            SustainCount;
        public          GuitarState    GuitarState;

        public GuitarNoteGroup(in DualTime endTime, long overdriveIndex, long soloIndex)
        {
            Position = endTime;
            OverdriveIndex = overdriveIndex;
            SoloIndex = soloIndex;
            LaneMask = GuitarLaneMask.None;
            LaneCount = 0;
            GuitarState = GuitarState.Natural;
            SustainCount = 0;
        }
    }
}