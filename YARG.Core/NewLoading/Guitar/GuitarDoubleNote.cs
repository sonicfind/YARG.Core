namespace YARG.Core.NewLoading.Guitar
{
    public struct GuitarDoubleNote
    {
        /// <summary>
        /// The lane to add to a mask
        /// </summary>
        public readonly byte LaneAddition;

        /// <summary>
        /// The lane to search for within a set of sustains
        /// </summary>
        public readonly byte LaneQuery;

        public GuitarDoubleNote(byte laneAddition, byte laneQuery)
        {
            LaneQuery = laneQuery;
            LaneAddition = laneAddition;
        }
    }
}
