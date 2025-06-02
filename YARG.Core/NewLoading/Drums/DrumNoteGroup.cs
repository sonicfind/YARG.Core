using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct DrumNoteGroup // 12 bytes
    {
        /// <summary>
        /// The active lanes without dynamics
        /// </summary>
        public DrumLaneMask NormalMask;

        /// <summary>
        /// The active lanes with accents
        /// </summary>
        public DrumLaneMask AccentMask;

        /// <summary>
        /// The active lanes with ghosts
        /// </summary>
        public DrumLaneMask GhostMask;

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
