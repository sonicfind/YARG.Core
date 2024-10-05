using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Handles tracking the position in seconds of a particular tick. It uses a moving pointer to dodge long binary searches.
    /// </summary>
    public unsafe struct TempoTracker
    {
        private readonly YARGKeyValuePair<long, Tempo2>* _end;
        private YARGKeyValuePair<long, Tempo2>* _position;

        public readonly long Resolution;

        /// <summary>
        /// Initializes the tracker to the starting position of the given sync track
        /// </summary>
        /// <param name="sync">The sync track to traverse</param>
        /// <param name="resolution">The tickrate of the chart</param>
        public TempoTracker(SyncTrack2 sync, long resolution)
        {
            Debug.Assert(sync.TempoMarkers.Count > 0, "There must exist at least one marker in the tempo list");
            Resolution = resolution;
            _position = sync.TempoMarkers.Data;
            _end = _position + sync.TempoMarkers.Count;
        }

        /// <summary>
        /// Converts the provided ticks to seconds
        /// </summary>
        /// <remarks>This modifies internal state to perform performance-optimized searches</remarks>
        /// <param name="ticks">Position in ticks to convert</param>
        /// <returns>The position in seconds</returns>
        public unsafe double Traverse(long ticks)
        {
            Debug.Assert(ticks >= _position->Key, "Tick position to convert placed before the current key");
            while (_position + 1 < _end && _position[1].Key <= ticks)
            {
                ++_position;
            }

            double quarters = (ticks - _position->Key) / (double) Resolution;
            long micros = (long) (_position->Value.MicrosPerQuarter * quarters) + _position->Value.Anchor;
            return micros / (double) Tempo2.MICROS_PER_SECOND;
        }

        /// <summary>
        /// Converts the provided ticks to seconds
        /// </summary>
        /// <remarks>The internal state of the tracker will not change from this call</remarks>
        /// <param name="ticks">Position in ticks to convert</param>
        /// <returns>The position in seconds</returns>
        public readonly unsafe double UnmovingConvert(long ticks)
        {
            Debug.Assert(ticks >= _position->Key, "Tick position to convert placed before the current key");
            var tmp = _position;
            while (tmp + 1 < _end && tmp[1].Key <= ticks)
            {
                ++tmp;
            }

            double quarters = (ticks - tmp->Key) / (double) Resolution;
            long micros = (long) (tmp->Value.MicrosPerQuarter * quarters) + tmp->Value.Anchor;
            return micros / (double) Tempo2.MICROS_PER_SECOND;
        }
    }
}
