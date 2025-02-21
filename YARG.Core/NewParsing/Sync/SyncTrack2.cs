using System;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class SyncTrack2 : IDisposable
    {
        public YargNativeSortedList<long, Tempo2> TempoMarkers { get; } = new();
        public YargNativeSortedList<long, TimeSig2> TimeSigs { get; } = new();

        /// <summary>
        /// Initializes the synctrack with the given tickrate, a default timesig of 4/4, and a default bpm of 120
        /// </summary>
        public SyncTrack2()
        {
            TempoMarkers.Add(0, Tempo2.DEFAULT);
            TimeSigs.Add(0, TimeSig2.DEFAULT);
        }

        /// <summary>
        /// Resets the synctrack to its initial state with 120bpm at 4/4
        /// </summary>
        public void Reset()
        {
            TempoMarkers.Clear();
            TimeSigs.Clear();
            TempoMarkers.Add(0, Tempo2.DEFAULT);
            TimeSigs.Add(0, TimeSig2.DEFAULT);
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
