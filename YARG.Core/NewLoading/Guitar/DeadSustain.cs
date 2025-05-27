namespace YARG.Core.NewLoading.Guitar
{
    /// <summary>
    /// A sustain structure purely handling the visuals of dropped or missed sustains.
    /// </summary>
    /// <remarks>
    /// Because the structure is solely for visual communication,
    /// we don't need the tick units for the start and end times.
    /// </remarks>
    public struct DeadSustain
    {
        /// <summary>
        /// The starting point where the player missed or dropped the sustain
        /// </summary>
        public readonly double StartTime;

        /// <summary>
        /// The lanes encompassing the note
        /// </summary>
        public readonly GuitarLaneMask LaneMask;

        /// <summary>
        /// End point of the original sustain
        /// </summary>
        /// <remarks>
        /// We update this value if a player switches difficulties mid-song (to allow for cleanly swapping out notes)
        /// </remarks>
        public double EndTime;

        public DeadSustain(double startTime, double endTime, GuitarLaneMask mask)
        {
            StartTime = startTime;
            LaneMask = mask;
            EndTime = endTime;
        }
    }
}
