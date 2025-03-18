namespace YARG.Core.NewLoading
{
    public struct GuitarDoubleNote
    {
        /// <summary>
        /// The lane to add to a mask
        /// </summary>
        public readonly GuitarLaneMask LaneAddition;

        /// <summary>
        /// The lane to search for within a set of sustains
        /// </summary>
        public readonly GuitarLaneMask SustainQuery;
    }
}