namespace YARG.Core.Chart
{
    public static partial class DotChartLoader
    {
        private const int FORCED_VALUE = 5;
        private const int TAPPED_VALUE = 6;
        private const int OPENNOTE = 7;
        private const int BASE_RANGE = 5;

        private const int OPEN_INDEX = 0;
        public static bool Set(FiveFret note, int lane, long length)
        {
            if (lane < BASE_RANGE)
                note[lane + 1] = length;
            else if (lane == FORCED_VALUE)
                note.Forcing = ForceStatus.FORCED_LEGACY;
            else if (lane == TAPPED_VALUE)
                note.IsTap = true;
            else if (lane == OPENNOTE)
                note[OPEN_INDEX] = length;
            else
                return false;
            return true;
        }

        private static readonly int[] SIXFRETLANES = new int[5] { 4, 5, 6, 1, 2 };
        private const int BLACK3_INDEX = 3;
        public static bool Set(SixFret note, int lane, long length)
        {
            if (lane < BASE_RANGE)
                note[SIXFRETLANES[lane]] = length;
            else if (lane == 8)
                note[BLACK3_INDEX] = length;
            else if (lane == FORCED_VALUE)
                note.Forcing = ForceStatus.FORCED_LEGACY;
            else if (lane == TAPPED_VALUE)
                note.IsTap = true;
            else if (lane == OPENNOTE)
                note[OPEN_INDEX] = length;
            else
                return false;
            return true;
        }
    }
}
