using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct DrumRoll
    {
        public readonly DualTime EndTime;
        public readonly int      NoteMask;

        public DrumRoll(in DualTime endTime, int noteMask)
        {
            EndTime = endTime;
            NoteMask = noteMask;
        }
    }
}