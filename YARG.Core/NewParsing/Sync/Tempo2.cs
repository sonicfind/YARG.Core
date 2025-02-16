namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Contains the microseconds per quarter and microseconds position for a specific tick position
    /// </summary>
    public struct Tempo2
    {
        public const int MICROS_PER_SECOND =  1000000;
        /// <summary>
        /// A factor used to convert to and from BPM and MicrosPerQuarter.<br></br>
        /// There is quite literally no unit of measurement for it.
        /// </summary>
        public const int BPM_FACTOR = 60000000;
        public const int DEFAULT_BPM = 120;
        public const int MICROS_AT_120BPM = BPM_FACTOR / DEFAULT_BPM;

        public static readonly Tempo2 DEFAULT = new()
        {
            MicrosPerQuarter = MICROS_AT_120BPM
        };

        public int MicrosPerQuarter;
        public long Anchor;

        /// <summary>
        /// Handles the conversions to and from float BPMs from and to microseconds per quarter (respectively)
        /// </summary>
        public float BPM
        {
            readonly get { return MicrosPerQuarter != 0 ? (float) BPM_FACTOR / MicrosPerQuarter : float.MaxValue; }
            set { MicrosPerQuarter = value != 0 ? (int) (BPM_FACTOR / value) : int.MaxValue; }
        }
    }
}
