using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class SyncTrack2 : IDisposable
    {
        private long _tickrate;
        public readonly YARGNativeSortedList<long, Tempo2> TempoMarkers = new();
        public readonly YARGNativeSortedList<long, TimeSig2> TimeSigs = new();

        public long Tickrate => _tickrate;

        /// <summary>
        /// Initializes the synctrack with the given tickrate, a default timesig of 4/4, and a default bpm of 120
        /// </summary>
        public SyncTrack2(long tickrate)
        {
            _tickrate = tickrate;
            TempoMarkers.Append(0, Tempo2.DEFAULT);
            TimeSigs.Append(0, TimeSig2.DEFAULT);
        }

        /// <summary>
        /// Resets the synctrack to its initial state with 120bpm at 4/4
        /// </summary>
        public void Reset()
        {
            TempoMarkers.Clear();
            TimeSigs.Clear();
            TempoMarkers.Append(0, Tempo2.DEFAULT);
            TimeSigs.Append(0, TimeSig2.DEFAULT);
        }

        /// <summary>
        /// Disposes all unmanaged data held by the sync track
        /// </summary>
        public void Dispose()
        {
            TempoMarkers.Dispose();
            TimeSigs.Dispose();
        }
    }
}
