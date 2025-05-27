using System;
using System.Diagnostics;
using System.Linq;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.Logging;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class BeatTracker
    {
        private readonly YargNativeSortedList<DualTime, BeatlineType> _beats;

        public int Index { get; private set; }
        public int Count => _beats.Count;

        public (double Position, BeatlineType Beat) this[int index]
        {
            get
            {
                ref readonly var beat = ref _beats[index];
                return (beat.Key.Seconds, beat.Value);
            }
        }

        /// <summary>
        /// Initializes the tracker to the starting position of the given sync track
        /// </summary>
        /// <param name="beats">The beat map to traverse</param>
        public BeatTracker(YargNativeSortedList<DualTime, BeatlineType> beats)
        {
            YargLogger.Assert(!beats.IsEmpty(), "At least one beat must exist in the map");
            _beats = beats;
            Index = 0;
        }

        public bool IsComplete()
        {
            return Index + 1 >= _beats.Count;
        }

        public long Update(double time)
        {
            Debug.Assert(time >= _beats[Index].Key.Seconds);
            while (Index + 1 < _beats.Count && _beats[Index + 1].Key.Seconds <= time)
            {
                ++Index;
            }
            return Index;
        }

        public long Jump(double time)
        {
            int index = _beats.Find(time);
            if (index < 0)
            {
                // minus one as the indices bit flip to after the beat that actually contains that point in time
                index = ~index - 1;
            }
            Index = index;
            return index;
        }
    }
}
