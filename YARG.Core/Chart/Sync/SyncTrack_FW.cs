using System;
using YARG.Core.Chart.FlatDictionary;

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
            var span = tempoMarkers.Span;
            int length = span.Length;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || ticks < span[i + 1].position)
                {
                    ref var marker = ref span[i];
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
            var span = tempoMarkers.Span;
            int length = span.Length;
            float micros = seconds * MICROS_PER_SECOND;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || micros < span[i + 1].obj.Anchor)
                {
                    ref var marker = ref span[i];
                    startIndex = i;
                    return (long) ((micros - marker.obj.Anchor) * _tickrate / marker.obj.Micros) + marker.position;
                }
            }
            throw new Exception("dafuq");
        }
    }
}
