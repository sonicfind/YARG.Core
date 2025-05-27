using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct DrumRoll
    {
        public readonly DualTime     EndTime;
        public readonly DrumLaneMask LaneMask;

        public DrumRoll(in DualTime endTime, DrumLaneMask laneMask)
        {
            EndTime = endTime;
            LaneMask = laneMask;
        }
    }
}
