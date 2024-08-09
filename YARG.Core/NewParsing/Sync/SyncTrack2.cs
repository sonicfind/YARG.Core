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

        public unsafe double ConvertToSeconds(long ticks, YARGKeyValuePair<long, Tempo2>* curr, in YARGKeyValuePair<long, Tempo2>* end)
        {
            return ConvertToSeconds(ticks, ref curr, end);
        }

        public unsafe double ConvertToSeconds(long ticks, ref YARGKeyValuePair<long, Tempo2>* curr, in YARGKeyValuePair<long, Tempo2>* end)
        {
            while (curr < end)
            {
                if (curr + 1 == end || ticks < curr[1].Key)
                {
                    double quarters = (ticks - curr->Key) / (double) _tickrate;
                    long micros = (long) (curr->Value.MicrosPerQuarter * quarters) + curr->Value.Anchor;
                    return micros / (double) Tempo2.MICROS_PER_SECOND;
                }
                ++curr;
            }
            throw new ArgumentOutOfRangeException(nameof(curr));
        }

        public unsafe long ConvertToTicks(double seconds, YARGKeyValuePair<long, Tempo2>* curr, in YARGKeyValuePair<long, Tempo2>* end)
        {
            return ConvertToTicks(seconds, ref curr, end);
        }

        public unsafe long ConvertToTicks(double seconds, ref YARGKeyValuePair<long, Tempo2>* curr, in YARGKeyValuePair<long, Tempo2>* end)
        {
            long micros = (long) (seconds * Tempo2.MICROS_PER_SECOND);
            while (curr < end)
            {
                if (curr + 1 == end || micros < curr[1].Key)
                {
                    double quarters = (micros - curr->Value.Anchor) / (double) curr->Value.MicrosPerQuarter;
                    return (long) (quarters * _tickrate) + curr->Key;
                }
                ++curr;
            }
            throw new ArgumentOutOfRangeException(nameof(curr));
        }

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
        }
    }
}
