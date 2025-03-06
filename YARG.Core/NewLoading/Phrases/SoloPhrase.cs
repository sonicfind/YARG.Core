using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct SoloPhrase
    {
        public readonly DualTime StartTime;
        public readonly DualTime EndTime;
        public long     TotalNotes;
        public long     HitCount;

        public SoloPhrase(in DualTime startTime, in DualTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            TotalNotes = 0;
            HitCount = 0;
        }
    }
}