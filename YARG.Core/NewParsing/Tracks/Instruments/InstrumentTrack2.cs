using System;
using System.Collections;
using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public static class InstrumentTrack2
    {
        public const int NUM_DIFFICULTIES = 4;
    }

    public struct InstrumentTrack2<TNote> : ITrack
        where TNote : unmanaged, IInstrumentNote
    {
        public static readonly InstrumentTrack2<TNote> Default = new()
        {
            Difficulties = DifficultyTrackCollection<TNote>.Default,
            Events = YARGManagedSortedList<DualTime, HashSet<string>>.Default,
        };

        public DifficultyTrackCollection<TNote> Difficulties;
        public YARGManagedSortedList<DualTime, HashSet<string>> Events;

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public readonly bool IsEmpty()
        {
            return Difficulties.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public void Clear()
        {
            Difficulties.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties.<br></br>
        /// </summary>
        public void TrimExcess()
        {
            Difficulties.TrimExcess();
            // Trimming managed lists just generates a new array for GC to handle.
            // The exact opposite of what we want.
        }

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            Difficulties.UpdateLastNoteTime(ref lastNoteTime);
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every difficulty
        /// </summary>
        public void Dispose(bool dispose)
        {
            Difficulties.Dispose(dispose);
            if (dispose)
            {
                Events.Dispose();
            }
        }
    }
}
