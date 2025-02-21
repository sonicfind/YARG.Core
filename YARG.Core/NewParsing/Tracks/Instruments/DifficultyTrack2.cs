using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class DifficultyTrack2<TNote> : ITrack
        where TNote : unmanaged, IInstrumentNote
    {
        public YargNativeSortedList<DualTime, TNote> Notes { get; }
        public YargNativeSortedList<DualTime, DualTime> Overdrives { get; }
        public YargNativeSortedList<DualTime, DualTime> Soloes { get; }
        public YargNativeSortedList<DualTime, DualTime> Trills { get; }
        public YargNativeSortedList<DualTime, DualTime> Tremolos { get; }
        public YargNativeSortedList<DualTime, DualTime> BREs { get; }
        public YargNativeSortedList<DualTime, DualTime> Faceoff_Player1 { get; }
        public YargNativeSortedList<DualTime, DualTime> Faceoff_Player2 { get; }
        public YargManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public DifficultyTrack2()
        {
            Notes = new();
            Overdrives = new();
            Soloes = new();
            Trills = new();
            Tremolos = new();
            BREs = new();
            Faceoff_Player1 = new();
            Faceoff_Player2 = new();
            Events = new();
        }

        public DifficultyTrack2(DifficultyTrack2<TNote> source)
        {
            Notes           = new(source.Notes);
            Overdrives      = new(source.Overdrives);
            Soloes          = new(source.Soloes);
            Trills          = new(source.Trills);
            Tremolos        = new(source.Tremolos);
            BREs            = new(source.BREs);
            Faceoff_Player1 = new(source.Faceoff_Player1);
            Faceoff_Player2 = new(source.Faceoff_Player2);
            Events          = new(source.Events);
        }

        public void CopyFrom(DifficultyTrack2<TNote> source)
        {
            Notes.CopyFrom(source.Notes);
            Overdrives.CopyFrom(source.Overdrives);
            Soloes.CopyFrom(source.Soloes);
            Trills.CopyFrom(source.Trills);
            Tremolos.CopyFrom(source.Tremolos);
            BREs.CopyFrom(source.BREs);
            Faceoff_Player1.CopyFrom(source.Faceoff_Player1);
            Faceoff_Player2.CopyFrom(source.Faceoff_Player2);
            Events.CopyFrom(source.Events);
        }

        /// <summary>
        /// Returns if no notes, phrases, or events are present
        /// </summary>
        /// <returns>Whether the track is empty</returns>
        public bool IsEmpty()
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
            // Trimming managed lists just generates a new array for GC to handle.
            // The exact opposite of what we want.
        }

        public void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (!Notes.IsEmpty())
            {
                ref readonly var note = ref Notes[Notes.Count - 1];
                var tmp = note.Key + note.Value.GetLongestSustain();
                if (tmp > lastNoteTime)
                {
                    lastNoteTime = tmp;
                }
            }
        }

        public void Dispose()
        {
            Notes.Dispose();
            Overdrives.Dispose();
            Soloes.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            Faceoff_Player1.Dispose();
            Faceoff_Player2.Dispose();
            Events.Dispose();
        }
    }
}
