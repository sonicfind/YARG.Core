namespace YARG.Core.Chart
{
    public class SyncTrack_FW
    {
        private uint _tickrate;
        public readonly TimedFlatDictionary<Tempo_FW> tempoMarkers = new();
        public readonly TimedFlatDictionary<TimeSig_FW> timeSigs = new();
        public readonly FlatDictionary<DualPosition, BeatlineType> beatMap = new();

        public SyncTrack_FW() { }

        public uint Tickrate
        {
            get { return _tickrate; }
            set { _tickrate = value; }
        }

    }
}
