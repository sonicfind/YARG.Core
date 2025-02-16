using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarDifficultyTrack<TProFret>
        where TProFret : unmanaged, IProFret
    {
        public YARGNativeSortedList<DualTime, ProGuitarNote<TProFret>> Notes { get; }
        public YARGNativeSortedList<DualTime, DualTime> Overdrives { get; }
        public YARGNativeSortedList<DualTime, DualTime> Soloes { get; }
        public YARGNativeSortedList<DualTime, DualTime> Trills { get; }
        public YARGNativeSortedList<DualTime, DualTime> Tremolos { get; }
        public YARGNativeSortedList<DualTime, DualTime> BREs { get; }
        public YARGNativeSortedList<DualTime, DualTime> Faceoff_Player1 { get; }
        public YARGNativeSortedList<DualTime, DualTime> Faceoff_Player2 { get; }
        public YARGNativeSortedList<DualTime, DualTime> Arpeggios { get; }
        public YARGManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public ProGuitarDifficultyTrack()
        {
            Notes = new();
            Overdrives = new();
            Soloes = new();
            Trills = new();
            Tremolos = new();
            BREs = new();
            Faceoff_Player1 = new();
            Faceoff_Player2 = new();
            Arpeggios = new();
            Events = new();
        }

        public ProGuitarDifficultyTrack(ProGuitarDifficultyTrack<TProFret> source)
        {
            Notes = new(source.Notes);
            Overdrives = new(source.Overdrives);
            Soloes = new(source.Soloes);
            Trills = new(source.Trills);
            Tremolos = new(source.Tremolos);
            BREs = new(source.BREs);
            Faceoff_Player1 = new(source.Faceoff_Player1);
            Faceoff_Player2 = new(source.Faceoff_Player2);
            Arpeggios = new(source.Arpeggios);
            Events = new(source.Events);
        }

        public void CopyFrom(ProGuitarDifficultyTrack<TProFret> source)
        {
            Notes.CopyFrom(source.Notes);
            Overdrives.CopyFrom(source.Overdrives);
            Soloes.CopyFrom(source.Soloes);
            Trills.CopyFrom(source.Trills);
            Tremolos.CopyFrom(source.Tremolos);
            BREs.CopyFrom(source.BREs);
            Faceoff_Player1.CopyFrom(source.Faceoff_Player1);
            Faceoff_Player2.CopyFrom(source.Faceoff_Player2);
            Arpeggios.CopyFrom(source.Arpeggios);
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
                && Arpeggios.IsEmpty()
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
            Arpeggios.Clear();
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
            Arpeggios.TrimExcess();
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
            Arpeggios.Dispose();
            Events.Dispose();
        }
    }
}
