using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public abstract class VocalTrack2 : ITrack
    {
        public readonly YARGNativeSortedList<DualTime, bool> Percussion = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> VocalPhrases_1 = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> VocalPhrases_2 = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> HarmonyLines = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> RangeShifts = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Overdrives = new();
        public readonly YARGNativeSortedSet<DualTime> LyricShifts = new();
        public readonly YARGManagedSortedList<DualTime, HashSet<string>> Events = new();

        public abstract VocalPart2 this[int index] { get; }
        public abstract int NumTracks { get; }

        protected VocalTrack2() { }

        public virtual bool IsEmpty()
        {
            return Percussion.IsEmpty()
                && VocalPhrases_1.IsEmpty()
                && VocalPhrases_2.IsEmpty()
                && HarmonyLines.IsEmpty()
                && RangeShifts.IsEmpty()
                && Overdrives.IsEmpty()
                && LyricShifts.IsEmpty()
                && Events.IsEmpty();
        }

        public virtual void Clear()
        {
            Percussion.Clear();
            VocalPhrases_1.Clear();
            VocalPhrases_2.Clear();
            HarmonyLines.Clear();
            RangeShifts.Clear();
            Overdrives.Clear();
            LyricShifts.Clear();
            Events.Clear();
        }

        public virtual void TrimExcess()
        {
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

        public virtual unsafe DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
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

        public virtual void Dispose()
        {
            Percussion.Dispose();
            VocalPhrases_1.Dispose();
            VocalPhrases_2.Dispose();
            HarmonyLines.Dispose();
            RangeShifts.Dispose();
            Overdrives.Dispose();
            LyricShifts.Dispose();
        }
    }

    public class LeadVocalsTrack : VocalTrack2
    {
        private readonly VocalPart2 _part;

        public override VocalPart2 this[int index]
        {
            get
            {
                if (index > 0)
                {
                    throw new System.IndexOutOfRangeException();
                }
                return _part;
            }
        }

        public override int NumTracks => 1;

        public LeadVocalsTrack()
        {
            _part.Notes = new YARGNativeSortedList<DualTime, VocalNote2>();
            _part.Lyrics = new YARGManagedSortedList<DualTime, NonNullString>();
        }

        public override bool IsEmpty()
        {
            return _part.IsEmpty() && base.IsEmpty();
        }

        public override void Clear()
        {
            _part.Clear();
            base.Clear();
        }

        public override void TrimExcess()
        {
            _part.TrimExcess();
        }

        public override unsafe DualTime GetLastNoteTime()
        {
            var endTime = base.GetLastNoteTime();
            if (!_part.Notes.IsEmpty())
            {
                ref var vocal = ref _part.Notes.Data[_part.Notes.Count - 1];
                var end = vocal.Key + vocal.Value.Duration;
                if (end > endTime)
                {
                    endTime = end;
                }
            }
            return endTime;
        }

        public override void Dispose()
        {
            _part.Notes.Dispose();
            _part.Lyrics.Clear();
            base.Dispose();
        }
    }

    public class HarmonyVocalsTrack : VocalTrack2
    {
        private readonly VocalPart2[] _parts = new VocalPart2[3];

        public override VocalPart2 this[int index] => _parts[index];

        public override int NumTracks => 3;

        public HarmonyVocalsTrack()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                _parts[i].Notes = new YARGNativeSortedList<DualTime, VocalNote2>();
                _parts[i].Lyrics = new YARGManagedSortedList<DualTime, NonNullString>();
            }
        }

        public override bool IsEmpty()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                if (!_parts[i].IsEmpty())
                {
                    return false;
                }
            }
            return base.IsEmpty();
        }

        public override void Clear()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                _parts[i].Clear();
            }
            base.Clear();
        }

        public override void TrimExcess()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                _parts[i].TrimExcess();
            }
        }

        public override unsafe DualTime GetLastNoteTime()
        {
            var endTime = base.GetLastNoteTime();
            for (int i = 0; i < NumTracks; ++i)
            {
                var notes = _parts[i].Notes;
                if (!notes.IsEmpty())
                {
                    ref var vocal = ref notes.Data[notes.Count - 1];
                    var end = vocal.Key + vocal.Value.Duration;
                    if (end > endTime)
                    {
                        endTime = end;
                    }
                }
            }
            
            return endTime;
        }

        public override void Dispose()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                _parts[i].Notes.Dispose();
                _parts[i].Lyrics.Clear();
            }
            base.Dispose();
        }
    }
}
