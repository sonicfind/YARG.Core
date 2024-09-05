using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class DifficultyTrack2<TNote> : ITrack
        where TNote : unmanaged, IInstrumentNote
    {
        public readonly YARGNativeSortedList<DualTime, TNote> Notes = new();
        public readonly InstrumentPhrases Phrases = new();
        public readonly YARGManagedSortedList<DualTime, HashSet<string>> Events = new();

        public DifficultyTrack2() {}

        /// <summary>
        /// Move constructor that siphons all phrases and special events from the source,
        /// leaving it in a default state.
        /// </summary>
        /// <remarks>Does not effect <see cref="Notes"/>. That remains unchanged</remarks>
        /// <param name="source">The source track to move phrases from</param>
        public DifficultyTrack2(InstrumentPhrases phrases, YARGManagedSortedList<DualTime, HashSet<string>> events)
        {
            Phrases = new(phrases);
            Events = new(events);
        }

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public virtual bool IsEmpty()
        {
            return Notes.IsEmpty() && Phrases.IsEmpty() && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, phrases, and events
        /// </summary>
        public virtual void Clear()
        {
            Notes.Clear();
            Phrases.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from notes and phrases
        /// </summary>
        public virtual void TrimExcess()
        {
            if ((Notes.Count < 500 || 10000 <= Notes.Count) && Notes.Count < Notes.Capacity)
            {
                Notes.TrimExcess();
            }
            Phrases.TrimExcess();
        }

        public unsafe DualTime GetLastNoteTime()
        {
            if (Notes.IsEmpty())
            {
                return default;
            }

            ref var note = ref Notes.Data[Notes.Count - 1];
            return note.Key + note.Value.GetLongestSustain();
        }

        /// <summary>
        /// Diposes all the unmanaged data used for notes and phrases
        /// </summary>
        public virtual void Dispose()
        {
            Notes.Dispose();
            Phrases.Dispose();
        }
    }
}
