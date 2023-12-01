using System;
using YARG.Core.Chart;

namespace YARG.Core.Parsing
{
    public class SyncTrack_FW : IDisposable
    {
        public readonly NativeFlatDictionary<long, Tempo_FW> TempoMarkers = new();
        public readonly NativeFlatDictionary<long, TimeSig_FW> TimeSigs = new();
        public readonly NativeFlatDictionary<DualTime, BeatlineType> BeatMap = new();

        public uint Tickrate;
        public DualTime EndTime;


        internal const int MICROS_PER_SECOND = 1000000;
        public double ConvertPositionToSeconds(long ticks, int startIndex)
        {
            return ConvertPositionToSeconds(ticks, ref startIndex);
        }

        public double ConvertPositionToSeconds(long ticks, ref int startIndex)
        {
            unsafe
            {
                var data = TempoMarkers.Data;
                int length = TempoMarkers.Count;
                for (int i = startIndex; i < length; i++)
                {
                    if (i + 1 == length || ticks < data[i + 1].position)
                    {
                        ref var marker = ref data[i];
                        startIndex = i;
                        return ((marker.obj.Micros * (ticks - marker.position) / (double) Tickrate) + marker.obj.Anchor) / MICROS_PER_SECOND;
                    }
                }
            }
            throw new Exception("dafuq");
        }

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
            BeatMap.Dispose();
        }
    }
}
