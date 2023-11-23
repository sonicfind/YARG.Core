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
        public double ConvertToSeconds(long ticks, int startIndex = 0)
        {
            return ConvertToSeconds(ticks, ref startIndex);
        }

        public double ConvertToSeconds(long ticks, ref int startIndex)
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

        public long ConvertToTicks(double seconds, int startIndex = 0)
        {
            return ConvertToTicks(seconds, ref startIndex);
        }

        public long ConvertToTicks(double seconds, ref int startIndex)
        {
            var span = TempoMarkers.Span;
            int length = span.Length;
            double micros = seconds * MICROS_PER_SECOND;
            for (int i = startIndex; i < length; i++)
            {
                if (i + 1 == length || micros < span[i + 1].obj.Anchor)
                {
                    ref var marker = ref span[i];
                    startIndex = i;
                    return (long) ((micros - marker.obj.Anchor) * Tickrate / marker.obj.Micros) + marker.position;
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
