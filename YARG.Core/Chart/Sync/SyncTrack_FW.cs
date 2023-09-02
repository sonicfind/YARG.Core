using System;

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

        internal const int MICROS_PER_SECOND = 1000000;
        public float ConvertToSeconds(long ticks, int startIndex = 0)
        {
            return ConvertToSeconds(ticks, ref startIndex);
        }

        public float ConvertToSeconds(long ticks, ref int startIndex)
        {
            (var data, int count) = tempoMarkers.Data;
            for (int i = startIndex; i < count; i++)
            {
                ref var marker = ref data[i];
                if (i + 1 == count || ticks < data[i + 1].position)
                {
                    startIndex = i;
                    return ((marker.obj.Micros * (ticks - marker.position) / (float) _tickrate) + marker.obj.Anchor) / MICROS_PER_SECOND;
                }
            }
            throw new Exception("dafuq");
        }

        public long ConvertToTicks(float seconds, int startIndex = 0)
        {
            return ConvertToTicks(seconds, ref startIndex);
        }

        public long ConvertToTicks(float seconds, ref int startIndex)
        {
            (var data, int count) = tempoMarkers.Data;
            float micros = seconds * MICROS_PER_SECOND;
            for (int i = startIndex; i < count; i++)
            {
                ref var marker = ref data[i];
                if (i + 1 == count || micros < data[i + 1].obj.Anchor)
                {
                    startIndex = i;
                    return (long) ((micros - marker.obj.Anchor) * _tickrate / marker.obj.Micros) + marker.position;
                }
            }
            throw new Exception("dafuq");
        }
    }
}
