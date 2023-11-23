using YARG.Core.Parsing.Keys;

namespace YARG.Core.Parsing
{
    public static partial class DotChartLoader
    {
        public static bool Set(ref KeyNote note, int lane, in DualTime length)
        {
            if (lane >= 5)
                return false;

            note[lane] = new TruncatableSustain(length);
            return true;
        }
    }
}
