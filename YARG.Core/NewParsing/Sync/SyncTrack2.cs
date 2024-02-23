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

        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
            BeatMap.Dispose();
        }
    }
}
