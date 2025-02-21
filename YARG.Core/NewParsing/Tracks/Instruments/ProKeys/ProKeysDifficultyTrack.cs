using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public enum ProKey_Ranges
    {
        C1_E2,
        D1_F2,
        E1_G2,
        F1_A2,
        G1_B2,
        A1_C3,
    };

    public class ProKeysDifficultyTrack
    {
        public YargNativeSortedList<DualTime, ProKeyNote> Notes { get; }
        public YargNativeSortedList<DualTime, DualTime> Overdrives { get; }
        public YargNativeSortedList<DualTime, DualTime> Soloes { get; }
        public YargNativeSortedList<DualTime, DualTime> Trills { get; }
        public YargNativeSortedList<DualTime, DualTime> Tremolos { get; }
        public YargNativeSortedList<DualTime, DualTime> BREs { get; }
        public YargNativeSortedList<DualTime, DualTime> Faceoff_Player1 { get; }
        public YargNativeSortedList<DualTime, DualTime> Faceoff_Player2 { get; }
        public YargNativeSortedList<DualTime, ProKey_Ranges> Ranges { get; }
        public YargManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public ProKeysDifficultyTrack()
        {
            Notes = new();
            Overdrives = new();
            Soloes = new();
            Trills = new();
            Tremolos = new();
            BREs = new();
            Faceoff_Player1 = new();
            Faceoff_Player2 = new();
            Ranges = new();
            Events = new();
        }

        public ProKeysDifficultyTrack(ProKeysDifficultyTrack source)
        {
            Notes = new(source.Notes);
            Overdrives = new(source.Overdrives);
            Soloes = new(source.Soloes);
            Trills = new(source.Trills);
            Tremolos = new(source.Tremolos);
            BREs = new(source.BREs);
            Faceoff_Player1 = new(source.Faceoff_Player1);
            Faceoff_Player2 = new(source.Faceoff_Player2);
            Ranges = new(source.Ranges);
            Events = new(source.Events);
        }

        public void CopyFrom(ProKeysDifficultyTrack source)
        {
            Notes.CopyFrom(source.Notes);
            Overdrives.CopyFrom(source.Overdrives);
            Soloes.CopyFrom(source.Soloes);
            Trills.CopyFrom(source.Trills);
            Tremolos.CopyFrom(source.Tremolos);
            BREs.CopyFrom(source.BREs);
            Faceoff_Player1.CopyFrom(source.Faceoff_Player1);
            Faceoff_Player2.CopyFrom(source.Faceoff_Player2);
            Ranges.CopyFrom(source.Ranges);
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
                && Ranges.IsEmpty()
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
            Ranges.Clear();
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
            Ranges.TrimExcess();
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
            Ranges.Dispose();
            Events.Dispose();
        }
    }
}
