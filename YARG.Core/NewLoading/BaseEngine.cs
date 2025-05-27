using System;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public abstract class BaseEngine : IDisposable
    {
        public const long POINTS_PER_BEAT      = 25;
        public const long NOTES_PER_MULTIPLIER = 10;

        /// <summary>
        /// The length of time (in seconds) to use when applying transformation on a track
        /// </summary>
        public const double TRANSFORMATION_SPACING   = 1.5;

        public int    OverdriveIndex { get; protected set; } = 0;
        public int    SoloIndex      { get; protected set; } = 0;
        public double CurrentTime    { get; protected set; } = 0;
        public long   Score          { get; protected set; } = 0;
        public long   Combo          { get; protected set; } = 0;
        public long   Multiplier     { get; protected set; } = 1;
        public long   Health         { get; protected set; } = (long) int.MaxValue + 1;

        public abstract OverdriveInfo UpdateTime(double time);
        public abstract HittablePhrase GetCurrentOverdrive();
        public abstract void Dispose();
    }
}
