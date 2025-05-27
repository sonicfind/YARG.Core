using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading.Guitar
{
    public enum GuitarNoteStyle : byte
    {
        Strum,
        Hopo,
        Tap
    }

    public struct GuitarNoteGroup // 16 bytes
    {
        /// <summary>
        /// The lanes that encompass the group
        /// </summary>
        public GuitarLaneMask LaneMask;

        /// <summary>
        /// The number of lanes that encompass the group (used to differ engine behavior with chords)
        /// </summary>
        public byte LaneCount;

        /// <summary>
        /// The hit "style" for the group
        /// </summary>
        public GuitarNoteStyle Style;

        /// <summary>
        /// Number of <see cref="GuitarSustain"/> instances attributed to this group (should never change)
        /// </summary>
        public byte SustainCount;

        /// <summary>
        /// The starting index for this group's <see cref="GuitarSustain"/> instances
        /// </summary>
        /// <remarks>
        /// A mid-song difficulty swap may require updating this value
        /// </remarks>
        public int SustainIndex;

        /// <summary>
        /// The index to the overdrive phrase that contains this group, or -1 if no such phrase exists
        /// </summary>
        public int OverdriveIndex;

        /// <summary>
        /// The index to the solo phrase that contains this group, or -1 if no such phrase exists
        /// </summary>
        public int SoloIndex;
    }
}
