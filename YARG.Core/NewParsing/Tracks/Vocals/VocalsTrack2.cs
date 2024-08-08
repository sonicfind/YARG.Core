using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public class VocalTrack2 : ITrack
    {
        private readonly VocalPart2[] _parts;
        public readonly YARGNativeSortedList<DualTime, bool> Percussion = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> VocalPhrases_1 = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> VocalPhrases_2 = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> HarmonyLines = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> RangeShifts = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Overdrives = new();
        public readonly YARGNativeSortedSet<DualTime> LyricShifts = new();
        public readonly YARGManagedSortedList<DualTime, HashSet<string>> Events = new();

        public VocalPart2 this[int index] => _parts[index];

        public VocalTrack2(int numParts)
        {
            _parts = new VocalPart2[numParts];
            for (int i = 0; i < numParts; ++i)
            {
                _parts[i].Notes = new YARGNativeSortedList<DualTime, VocalNote2>();
                _parts[i].Lyrics = new YARGManagedSortedList<DualTime, NonNullString>();
            }
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < _parts.Length; ++i)
            {
                if (!_parts[i].IsEmpty())
                {
                    return false;
                }
            }
            return Percussion.IsEmpty()
                && VocalPhrases_1.IsEmpty()
                && VocalPhrases_2.IsEmpty()
                && HarmonyLines.IsEmpty()
                && RangeShifts.IsEmpty()
                && Overdrives.IsEmpty()
                && LyricShifts.IsEmpty()
                && Events.IsEmpty();
        }

        public void Clear()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Clear();
            }
            Percussion.Clear();
            VocalPhrases_1.Clear();
            VocalPhrases_2.Clear();
            HarmonyLines.Clear();
            RangeShifts.Clear();
            Overdrives.Clear();
            LyricShifts.Clear();
            Events.Clear();
        }

        public void TrimExcess()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].TrimExcess();
            }

            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
            {
                Percussion.TrimExcess();
            }
            VocalPhrases_1.TrimExcess();
            VocalPhrases_2.TrimExcess();
            HarmonyLines.TrimExcess();
            RangeShifts.TrimExcess();
            Overdrives.TrimExcess();
            LyricShifts.TrimExcess();
        }

        public unsafe DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            for (int i = 0; i < _parts.Length; i++)
            {
                var notes = _parts[i].Notes;
                if (notes.IsEmpty())
                    continue;

                ref var vocal = ref notes.Data[notes.Count - 1];
                var end = vocal.Key + vocal.Value.Duration;
                if (end > endTime)
                {
                    endTime = end;
                }
            }

            if (!Percussion.IsEmpty())
            {
                ref var perc = ref Percussion.Data[Percussion.Count - 1];
                if (perc.Key > endTime)
                {
                    endTime = perc.Key;
                }
            }
            return endTime;
        }

        public void Dispose()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Notes.Dispose();
                _parts[i].Lyrics.Clear();
            }
            Percussion.Dispose();
            VocalPhrases_1.Dispose();
            VocalPhrases_2.Dispose();
            HarmonyLines.Dispose();
            RangeShifts.Dispose();
            Overdrives.Dispose();
            LyricShifts.Dispose();
        }
    }
}
