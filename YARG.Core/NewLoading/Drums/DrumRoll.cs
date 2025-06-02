using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct DrumRoll
    {
        public readonly DualTime     StartTime;
        public readonly DualTime     EndTime;
        public readonly DrumLaneMask LaneMask;

        public DrumRoll(DualTime startTime, DualTime endTime, DrumLaneMask laneMask)
        {
            StartTime = startTime;
            EndTime = endTime;
            LaneMask = laneMask;
        }
    }
}
