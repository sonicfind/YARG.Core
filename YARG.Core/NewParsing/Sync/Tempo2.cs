namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Contains the microseconds per quarter and microseconds position for a specific tick position
    /// </summary>
    public struct Tempo2
    {
        public const long MICROS_PER_SECOND =  1000000;
        /// <summary>
        /// A factor used to convert to and from BPM and MicrosPerQuarter.<br></br>
        /// There is quite literally no unit of measurement for it.
        /// </summary>
        public const long BPM_FACTOR = 60000000;
        public const long DEFAULT_BPM = 120;
        public const long MICROS_AT_120BPM = BPM_FACTOR / DEFAULT_BPM;

        public static readonly Tempo2 DEFAULT = new()
        {
            MicrosecondsPerQuarter = MICROS_AT_120BPM
        };

        public long MicrosecondsPerQuarter;
        public long PositionInMicroseconds;

        /// <summary>
        /// Handles the conversions to and from float BPMs from and to microseconds per quarter (respectively)
        /// </summary>
        public float BPM
        {
            readonly get { return MicrosecondsPerQuarter != 0 ? (float) BPM_FACTOR / MicrosecondsPerQuarter : float.MaxValue; }
            set { MicrosecondsPerQuarter = value != 0 ? (int) (BPM_FACTOR / value) : int.MaxValue; }
        }
    }
}
