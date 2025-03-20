namespace YARG.Core.NewLoading
{
    public struct GuitarDoubleNote
    {
        /// <summary>
        /// The lane to add to a mask
        /// </summary>
        public readonly int LaneAddition;

        /// <summary>
        /// The lane to search for within a set of sustains
        /// </summary>
        public readonly int LaneQuery;

        /// <summary>
        /// The mask for the lane to add
        /// </summary>
        public readonly GuitarLaneMask MaskAddition;

        /// <summary>
        /// The mask for the lane to add
        /// </summary>
        public readonly GuitarLaneMask MaskQuery;

        public GuitarDoubleNote(int laneAddition, int laneQuery)
        {
            LaneQuery = laneQuery;
            LaneAddition = laneAddition;
            MaskQuery = (GuitarLaneMask) (1 << laneQuery);
            MaskAddition = (GuitarLaneMask) (1 << laneAddition);
        }
    }
}