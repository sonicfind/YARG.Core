using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public class SyncTrack2 : IDisposable
    {
        public uint Tickrate;
        public readonly YARGNativeSortedList<long, Tempo2> TempoMarkers = new();
        public readonly YARGNativeSortedList<long, TimeSig2> TimeSigs = new();
        public readonly YARGNativeSortedList<DualTime, BeatlineType> BeatMap = new();

        public double ConvertToSeconds(long ticks, int startIndex = 0)
        {
            return ConvertToSeconds(ticks, ref startIndex);
        }

        public double ConvertToSeconds(long ticks, ref int startIndex)
        {
            unsafe
            {
                var curr = TempoMarkers.Data + startIndex;
                var end = TempoMarkers.End;
                while (curr < end)
                {
                    if (curr + 1 == end || ticks < curr[1].Key)
                    {
                        startIndex = (int)(curr - TempoMarkers.Data);

                        double quarters = (ticks - curr->Key) / (double) Tickrate;
                        long micros = (long)(curr->Value.MicrosPerQuarter * quarters) + curr->Value.Anchor;
                        return micros / (double) Tempo2.MICROS_PER_SECOND;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
        }

        public long ConvertToTicks(double seconds, int startIndex = 0)
        {
            return ConvertToTicks(seconds, ref startIndex);
        }

        public long ConvertToTicks(double seconds, ref int startIndex)
        {
            long micros = (long) (seconds * Tempo2.MICROS_PER_SECOND);
            unsafe
            {
                var curr = TempoMarkers.Data + startIndex;
                var end = TempoMarkers.End;
                while (curr < end)
                {
                    if (curr + 1 == end || micros < curr[1].Key)
                    {
                        startIndex = (int) (curr - TempoMarkers.Data);

                        double quarters = (micros - curr->Value.Anchor) / (double) curr->Value.MicrosPerQuarter;
                        return (long)(quarters * Tickrate) + curr->Key;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
        }

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
            BeatMap.Dispose();
        }

        public static void Finalize(SyncTrack2 sync)
        {
            if (sync.TempoMarkers.IsEmpty() || sync.TempoMarkers.ElementAtIndex(0).Key != 0)
            {
                sync.TempoMarkers.Insert_Forced(0, 0, Tempo2.DEFAULT);
            }

            if (sync.TimeSigs.IsEmpty() || sync.TimeSigs.ElementAtIndex(0).Key != 0)
            {
                sync.TimeSigs.Insert_Forced(0, 0, TimeSig2.DEFAULT);
            }

            unsafe
            {
                var end = sync.TempoMarkers.End;
                // We can skip the first Anchor, even if not explicitly set (as it'd still be 0)
                for (var marker = sync.TempoMarkers.Data + 1; marker < end; ++marker)
                {
                    if (marker->Value.Anchor == 0)
                    {
                        var prev = marker - 1;
                        marker->Value.Anchor = (long) (((marker->Key - prev->Key) / (float) sync.Tickrate) * prev->Value.MicrosPerQuarter) + prev->Value.Anchor;
                    }
                }
            }
        }
    }
}
