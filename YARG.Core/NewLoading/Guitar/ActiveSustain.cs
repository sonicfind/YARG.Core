namespace YARG.Core.NewLoading.Guitar
{
    public struct ActiveSustain
    {
        /// <summary>
        /// The index of the sustain attached to this instance
        /// </summary>
        public readonly int SustainIndex;

        /// <summary>
        /// The index to the overdrive phrase that contains this group, or -1 if no such phrase exists
        /// </summary>
        /// <remarks>
        /// We will always override the index to -1 when a difficulty swap occurs mid-song
        /// </remarks>
        public int OverdriveIndex;

        public int BaseBeatIndex;

        /// <summary>
        /// The start point for hold-time based score calculations.
        /// </summary>
        /// <remarks>
        /// Whenever we perform a calculation, we need to always update the point to the current position (the only
        /// exception being whenever the sustain has ended).
        /// </remarks>
        public double BasePosition;

        public double? WhammyStart;

        public ActiveSustain(int baseBeatIndex, double basePosition, int sustainIndex, int overdriveIndex)
        {
            BaseBeatIndex = baseBeatIndex;
            BasePosition = basePosition;
            SustainIndex = sustainIndex;
            OverdriveIndex = overdriveIndex;
            WhammyStart = null;
        }
    }
}
