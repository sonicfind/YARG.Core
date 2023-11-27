using System;
using YARG.Core.Chart;

namespace YARG.Core.Parsing
{
    public class SyncTrack_FW : IDisposable
    {
        public uint Tickrate;
        public readonly NativeFlatDictionary<long, Tempo_FW> TempoMarkers = new();
        public readonly NativeFlatDictionary<long, TimeSig_FW> TimeSigs = new();
        public readonly NativeFlatDictionary<DualTime, BeatlineType> BeatMap = new();

        internal const int MICROS_PER_SECOND = 1000000;
        public double ConvertPositionToSeconds(long ticks, int startIndex)
        {
            return ConvertPositionToSeconds(ticks, ref startIndex);
        }

        public double ConvertPositionToSeconds(long ticks, ref int startIndex)
        {
            var span = TempoMarkers.Span;
            int length = span.Length;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || ticks < span[i + 1].position)
                {
                    ref var marker = ref span[i];
                    startIndex = i;
                    return ((marker.obj.Micros * (ticks - marker.position) / (double) Tickrate) + marker.obj.Anchor) / MICROS_PER_SECOND;
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
