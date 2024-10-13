using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class SyncTrack2 : IDisposable
    {
        public YARGNativeSortedList<long, Tempo2> TempoMarkers = YARGNativeSortedList<long, Tempo2>.Default;
        public YARGNativeSortedList<long, TimeSig2> TimeSigs = YARGNativeSortedList<long, TimeSig2>.Default;

        /// <summary>
        /// Initializes the synctrack with the given tickrate, a default timesig of 4/4, and a default bpm of 120
        /// </summary>
        public SyncTrack2()
        {
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
