using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct SustainTracker
    {
        public readonly long     SustainIndex;
        public          DualTime BasePosition;

        public SustainTracker(long index, DualTime basePosition)
        {
            SustainIndex = index;
            BasePosition = basePosition;
        }
    }
}