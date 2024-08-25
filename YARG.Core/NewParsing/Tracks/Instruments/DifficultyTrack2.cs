using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class DifficultyTrack2<TNote> : PhraseTrack
        where TNote : unmanaged, IInstrumentNote
    {
        public readonly YARGNativeSortedList<DualTime, TNote> Notes = new();

        public DifficultyTrack2() {}

        /// <summary>
        /// Move constructor that siphons all phrases and special events from the source,
        /// leaving it in a default state.
        /// </summary>
        /// <remarks>Does not effect <see cref="Notes"/>. That remains unchanged</remarks>
        /// <param name="source">The source track to move phrases from</param>
        public DifficultyTrack2(PhraseTrack source)
            : base(source) {}

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public override bool IsEmpty()
        {
            return Notes.IsEmpty() && base.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, phrases, and events
        /// </summary>
        public override void Clear()
        {
            Notes.Clear();
            base.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from notes and phrases
        /// </summary>
        public override void TrimExcess()
        {
            if ((Notes.Count < 500 || 10000 <= Notes.Count) && Notes.Count < Notes.Capacity)
            {
                Notes.TrimExcess();
            }
            base.TrimExcess();
        }

        public override unsafe DualTime GetLastNoteTime()
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
        public override void Dispose()
        {
            Notes.Dispose();
            base.Dispose();
        }
    }
}
