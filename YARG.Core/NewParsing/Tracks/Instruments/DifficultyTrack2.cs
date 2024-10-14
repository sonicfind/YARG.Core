using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public struct DifficultyTrack2<TNote> : ITrack
        where TNote : unmanaged, IInstrumentNote
    {
        public static readonly DifficultyTrack2<TNote> Default = new()
        {
            Notes = YARGNativeSortedList<DualTime, TNote>.Default,
            Overdrives = YARGNativeSortedList<DualTime, DualTime>.Default,
            Soloes = YARGNativeSortedList<DualTime, DualTime>.Default,
            Trills = YARGNativeSortedList<DualTime, DualTime>.Default,
            Tremolos = YARGNativeSortedList<DualTime, DualTime>.Default,
            BREs = YARGNativeSortedList<DualTime, DualTime>.Default,
            Faceoff_Player1 = YARGNativeSortedList<DualTime, DualTime>.Default,
            Faceoff_Player2 = YARGNativeSortedList<DualTime, DualTime>.Default,
            Events = YARGManagedSortedList<DualTime, HashSet<string>>.Default
        };

        public YARGNativeSortedList<DualTime, TNote> Notes;
        public YARGNativeSortedList<DualTime, DualTime> Overdrives;
        public YARGNativeSortedList<DualTime, DualTime> Soloes;
        public YARGNativeSortedList<DualTime, DualTime> Trills;
        public YARGNativeSortedList<DualTime, DualTime> Tremolos;
        public YARGNativeSortedList<DualTime, DualTime> BREs;
        public YARGNativeSortedList<DualTime, DualTime> Faceoff_Player1;
        public YARGNativeSortedList<DualTime, DualTime> Faceoff_Player2;
        public YARGManagedSortedList<DualTime, HashSet<string>> Events;

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public readonly bool IsEmpty()
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
        public void Clear()
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
        public void TrimExcess()
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

        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (Notes.IsEmpty())
            {
                return;
            }

            unsafe
            {
                ref readonly var note = ref Notes.Data[Notes.Count - 1];
                var tmp = note.Key + note.Value.GetLongestSustain();
                if (tmp > lastNoteTime)
                {
                    lastNoteTime = tmp;
                }
            }
        }

        
        public void Dispose(bool dispose)
        {
            Notes.Dispose();
            Overdrives.Dispose();
            Soloes.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            Faceoff_Player1.Dispose();
            Faceoff_Player2.Dispose();
            if (dispose)
            {
                Events.Dispose();
            }
        }

        /// <summary>
        /// Diposes all the data used for notes, phrases, and events
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
