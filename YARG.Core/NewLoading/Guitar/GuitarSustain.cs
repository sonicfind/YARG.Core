using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public struct GuitarSustain // 24 bytes (8-byte alignment)
    {
        /// <summary>
        /// The lanes that the sustain encompasses
        /// </summary>
        public GuitarLaneMask LaneMask;

        /// <summary>
        /// The number of lanes that encompass the sustain (used to differ engine behavior with chords)
        /// </summary>
        public byte LaneCount;

        /// <summary>
        /// Denotes whether this instance allows players to fret unused non-anchor-able lanes without dropping
        /// </summary>
        public bool HasFretLeniency;
    }
}
