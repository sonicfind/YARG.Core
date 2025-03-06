using System;
using System.Linq;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.Logging;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct BeatTracker
    {
        private readonly long                                         _resolution;
        private readonly YargNativeSortedList<DualTime, BeatlineType> _beats;
        private          long                                         _index;

        public readonly long Resolution => _resolution;
        public readonly long Index      => _index;
        public readonly long NumBeats  => _beats.Count;
        public readonly ref readonly (DualTime Position, BeatlineType Beat) this[long index] => ref _beats[index];

        /// <summary>
        /// Initializes the tracker to the starting position of the given sync track
        /// </summary>
        /// <param name="beats">The beat map to traverse</param>
        /// <param name="resolution">The tickRate of the chart</param>
        public BeatTracker(YargNativeSortedList<DualTime, BeatlineType> beats, long resolution)
        {
            YargLogger.Assert(!beats.IsEmpty(), "At least one beat must exist in the map");
            _resolution = resolution;
            _beats = beats;
            _index = 0;
        }

        public readonly bool IsAtEnd()
        {
            return _index >= _beats.Count;
        }

        public void IncrementIndex()
        {
            YargLogger.Assert(_index < _beats.Count, "At least one beat must exist in the map");
            _index++;
        }

        /// <summary>
        /// Updates the internal index to the beat that overlaps over the provided position
        /// </summary>
        /// <param name="time">The position to search for</param>
        /// <returns>The beat index currently pointed at</returns>
        public long Update(DualTime time)
        {
            _index = _beats.Find(time);
            if (_index < 0)
            {
                _index = ~_index - 1;
            }
            return _index;
        }

        /// <summary>
        /// Converts the provided seconds to ticks
        /// </summary>
        /// <remarks>This modifies internal state to perform performance-optimized searches</remarks>
        /// <param name="seconds">Position in seconds to convert</param>
        /// <returns>The position in ticks</returns>
        public long Convert(double seconds)
        {
            YargLogger.Assert(seconds >= _beats[_index].Key.Seconds,
                "Seconds position to convert must reside at or later than the current position");

            while (_index + 1 < _beats.Count && _beats[_index + 1].Key.Seconds <= seconds)
            {
                ++_index;
            }

            if (_index + 1 == _beats.Count)
            {
                return _beats[_index].Key.Ticks;
            }

            ref readonly var current = ref _beats[_index];
            ref readonly var next = ref _beats[_index + 1];
            var beatDistance = next.Key - current.Key;
            var secondsFromCurrent = seconds - current.Key.Seconds;
            return (long)Math.Floor(beatDistance.Ticks * (secondsFromCurrent / beatDistance.Seconds));
        }

        /// <summary>
        /// Converts the provided seconds to ticks
        /// </summary>
        /// <remarks>The internal state of the tracker will not change from this call</remarks>
        /// <param name="seconds">Position in seconds to convert</param>
        /// <returns>The position in ticks</returns>
        public readonly long ReadonlyConvert(double seconds)
        {
            YargLogger.Assert(seconds >= _beats[_index].Key.Seconds,
                "Seconds position to convert must reside at or later than the current position");

            var index = _index;
            while (index + 1 < _beats.Count && _beats[index + 1].Key.Seconds <= seconds)
            {
                ++index;
            }

            if (index + 1 == _beats.Count)
            {
                return _beats[index].Key.Ticks;
            }

            ref readonly var current = ref _beats[index];
            ref readonly var next = ref _beats[index + 1];
            var beatDistance = next.Key - current.Key;
            var secondsFromCurrent = seconds - current.Key.Seconds;
            return (long)Math.Floor(beatDistance.Ticks * (secondsFromCurrent / beatDistance.Seconds));
        }
    }
}