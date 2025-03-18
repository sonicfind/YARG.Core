using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct DrumNoteGroup
    {
        public readonly DualTime         Position;
        public readonly long             OverdriveIndex;
        public readonly long             SoloIndex;
        public          DrumLaneMask     LaneMask;
        public          long             LaneCount;
        public          DrumDynamicsMask DynamicsMask;

        public DrumNoteGroup(in DualTime position, in long overdriveIndex, in long soloIndex)
        {
            Position = position;
            OverdriveIndex = overdriveIndex;
            SoloIndex = soloIndex;
            LaneMask = DrumLaneMask.None;
            LaneCount = 0;
            DynamicsMask = DrumDynamicsMask.None;
        }
    }
}