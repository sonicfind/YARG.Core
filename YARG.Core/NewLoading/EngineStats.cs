using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public enum UpdateResult
    {
        OK,
        Drop,
        Overhit,
        MultiplierDrop,
    }

    public struct EngineStats
    {
        public static readonly EngineStats Zero = new EngineStats
        {
            NoteIndex   = 0,
            SoloIndex   = 0,
            CurrentTime = DualTime.Zero,
            Score       = 0,
            Health      = 0,
            Overdrive   = 0,
        };

        public const long POINTS_PER_BEAT = 25;
        public const long NOTES_PER_MULTIPLIER = 10;

        public long     NoteIndex;
        public long     SoloIndex;
        public DualTime CurrentTime;
        public long     Score;
        public long     Combo;
        public long     Multiplier;
        public long     Health;
        public long     Overdrive;
    }
}