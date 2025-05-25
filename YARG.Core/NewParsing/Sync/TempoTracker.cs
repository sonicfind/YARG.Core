using System.Diagnostics;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Handles tracking the position in seconds of a particular tick. It uses a moving pointer to dodge long binary searches.
    /// </summary>
    public struct TempoTracker
    {
        private readonly long                               _resolution;
        private readonly YargNativeSortedList<long, Tempo2> _tempoMarkers;
        private          int                                _index;

        public readonly long Resolution => _resolution;

        /// <summary>
        /// Initializes the tracker to the starting position of the given sync track
        /// </summary>
        /// <param name="sync">The sync track to traverse</param>
        /// <param name="resolution">The tickrate of the chart</param>
        public TempoTracker(SyncTrack2 sync, long resolution)
        {
            Debug.Assert(sync.TempoMarkers.Count > 0, "At least one marker must exist in the tempo list");
            _resolution = resolution;
            _tempoMarkers = sync.TempoMarkers;
            _index = 0;
        }

        /// <summary>
        /// Converts the provided ticks to seconds
        /// </summary>
        /// <remarks>This modifies internal state to perform performance-optimized searches</remarks>
        /// <param name="ticks">Position in ticks to convert</param>
        /// <returns>The position in seconds</returns>
        public double Convert(long ticks)
        {
            Debug.Assert(ticks >= _tempoMarkers[_index].Key,
                "Tick position to convert must reside at or later than the current position");

            while (_index + 1 < _tempoMarkers.Count && _tempoMarkers[_index + 1].Key <= ticks)
            {
                ++_index;
            }

            ref readonly var marker = ref _tempoMarkers[_index];
            double quartersOffset = (ticks - marker.Key) / (double) _resolution;
            long microsecondOffset = (long) (marker.Value.MicrosecondsPerQuarter * quartersOffset);
            long microsecondPosition = microsecondOffset + marker.Value.PositionInMicroseconds;
            return microsecondPosition / (double) Tempo2.MICROS_PER_SECOND;
        }

        /// <summary>
        /// Converts the provided ticks to seconds
        /// </summary>
        /// <remarks>The internal state of the tracker will not change from this call</remarks>
        /// <param name="ticks">Position in ticks to convert</param>
        /// <returns>The position in seconds</returns>
        public readonly double ReadonlyConvert(long ticks)
        {
            Debug.Assert(ticks >= _tempoMarkers[_index].Key,
                "Tick position to convert must reside at or later than the current position");

            var index = _index;
            while (index + 1 < _tempoMarkers.Count && _tempoMarkers[index + 1].Key <= ticks)
            {
                ++index;
            }

            ref readonly var marker = ref _tempoMarkers[index];
            double quartersOffset = (ticks - marker.Key) / (double) _resolution;
            long microsecondOffset = (long) (marker.Value.MicrosecondsPerQuarter * quartersOffset);
            long microsecondPosition = microsecondOffset + marker.Value.PositionInMicroseconds;
            return microsecondPosition / (double) Tempo2.MICROS_PER_SECOND;
        }
    }
}
