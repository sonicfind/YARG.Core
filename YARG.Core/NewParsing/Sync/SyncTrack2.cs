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

        /// <summary>
        /// Can you guess?
        /// </summary>
        public long Tickrate => _tickrate;

        /// <summary>
        /// Initializes the synctrack with the given tickrate, a default timesig of 4/4, a default bmp of 120
        /// </summary>
        /// <param name="tickrate">Uh... the tick rate</param>
        public SyncTrack2(uint tickrate)
        {
            _tickrate = tickrate;
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
