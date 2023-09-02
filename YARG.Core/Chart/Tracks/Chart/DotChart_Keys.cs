namespace YARG.Core.Chart
{
    public static partial class DotChartLoader
    {
        public static bool Set(Keys note, int lane, long length)
        {
            if (lane >= 5)
                return false;

            note[lane] = length;
            return true;
        }
    }
}
