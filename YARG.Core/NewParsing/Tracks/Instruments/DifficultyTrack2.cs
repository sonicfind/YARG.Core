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
        public readonly YARGNativeSortedList<DualTime, DualTime> Overdrives = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Soloes = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Trills = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Tremolos = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> BREs = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player1 = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player2 = new();
        public readonly YARGManagedSortedList<DualTime, HashSet<string>> Events = new();

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public virtual bool IsEmpty()
        {
            return Notes.IsEmpty()
                && Overdrives.IsEmpty()
                && Soloes.IsEmpty()
                && Trills.IsEmpty()
                && Tremolos.IsEmpty()
                && BREs.IsEmpty()
                && Faceoff_Player1.IsEmpty()
                && Faceoff_Player2.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all notes, phrases, and events
        /// </summary>
        public virtual void Clear()
        {
            Notes.Clear();
            Overdrives.Clear();
            Soloes.Clear();
            Trills.Clear();
            Tremolos.Clear();
            BREs.Clear();
            Faceoff_Player1.Clear();
            Faceoff_Player2.Clear();
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
            Overdrives.TrimExcess();
            Soloes.TrimExcess();
            Trills.TrimExcess();
            Tremolos.TrimExcess();
            BREs.TrimExcess();
            Faceoff_Player1.TrimExcess();
            Faceoff_Player2.TrimExcess();
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
            Overdrives.Dispose();
            Soloes.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            Faceoff_Player1.Dispose();
            Faceoff_Player2.Dispose();
        }
    }
}
