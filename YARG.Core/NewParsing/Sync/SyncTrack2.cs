using System;
using System.Collections.Generic;
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

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
        }
    }
}
