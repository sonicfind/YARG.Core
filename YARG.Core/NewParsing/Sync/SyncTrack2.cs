using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Chart;

namespace YARG.Core.NewParsing
{
    public class SyncTrack2 : IDisposable
    {
        private long _tickrate;
        public readonly YARGNativeSortedList<long, Tempo2> TempoMarkers = new();
        public readonly YARGNativeSortedList<long, TimeSig2> TimeSigs = new();

        public long Tickrate => _tickrate;

        public SyncTrack2(long tickrate)
        {
            _tickrate = tickrate;
            TempoMarkers.Append(0, Tempo2.DEFAULT);
            TimeSigs.Append(0, TimeSig2.DEFAULT);
        }

        public void TrimExcessData()
        {
            TempoMarkers.TrimExcess();
            TimeSigs.TrimExcess();
        }

        public void Reset()
        {
            TempoMarkers.Clear();
            TimeSigs.Clear();
            TempoMarkers.Append(0, Tempo2.DEFAULT);
            TimeSigs.Append(0, TimeSig2.DEFAULT);
        }

        public double ConvertToSeconds(long ticks, int startIndex = 0)
        {
            return ConvertToSeconds(ticks, ref startIndex);
        }

        public double ConvertToSeconds(long ticks, ref int startIndex)
        {
            unsafe
            {
                var end = TempoMarkers.End;
                for (var curr = TempoMarkers.Data + startIndex;  curr < end; ++curr)
                {
                    if (curr + 1 == end || ticks < curr[1].Key)
                    {
                        startIndex = (int)(curr - TempoMarkers.Data);

                        double quarters = (ticks - curr->Key) / (double) _tickrate;
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
                var end = TempoMarkers.End;
                for (var curr = TempoMarkers.Data + startIndex; curr < end; ++curr)
                {
                    if (curr + 1 == end || micros < curr[1].Key)
                    {
                        startIndex = (int) (curr - TempoMarkers.Data);

                        double quarters = (micros - curr->Value.Anchor) / (double) curr->Value.MicrosPerQuarter;
                        return (long)(quarters * _tickrate) + curr->Key;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
        }

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
        }
    }
}
