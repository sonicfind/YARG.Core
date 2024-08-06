using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public struct VocalPart2
    {
        public YARGNativeSortedList<DualTime, VocalNote2> Notes;
        public YARGManagedSortedList<DualTime, NonNullString> Lyrics;

        public readonly void Clear()
        {
            Notes.Clear();
            Lyrics.Clear();
        }

        public readonly bool IsEmpty()
        {
            return Notes.IsEmpty() && Lyrics.IsEmpty();
        }
    }

    public class VocalTrack2 : Track
    {
        private readonly VocalPart2[] _parts;
        public readonly YARGNativeSortedList<DualTime, VocalPercussion2> Percussion = new();

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

        public override bool IsEmpty()
        {
            for (int i = 0; i < _parts.Length; ++i)
            {
                if (!_parts[i].IsEmpty())
                {
                    return false;
                }
            }
            return Percussion.IsEmpty() && base.IsEmpty();
        }

        public override void Clear()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Clear();
            }
            Percussion.Clear();
            base.Clear();
        }

        public override void TrimExcess()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                var notes = _parts[i].Notes;
                if ((notes.Count < 100 || 2000 <= notes.Count) && notes.Count < notes.Capacity)
                {
                    notes.TrimExcess();
                }
            }

            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
            {
                Percussion.TrimExcess();
            }
        }

        public override unsafe DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            for (int i = 0; i < _parts.Length; i++)
            {
                var notes = _parts[i].Notes;
                if (notes.IsEmpty())
                    continue;

                var vocal = notes.ElementAtIndex(notes.Count - 1);
                var end = vocal->Key + vocal->Value.Duration;
                if (end > endTime)
                {
                    endTime = end;
                }
            }

            if (!Percussion.IsEmpty())
            {
                var perc = Percussion.ElementAtIndex(Percussion.Count - 1);
                if (perc->Key > endTime)
                {
                    endTime = perc->Key;
                }
            }
            return endTime;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _parts.Length; i++)
                    {
                        _parts[i].Notes.Dispose();
                        _parts[i].Lyrics.Clear();
                    }
                    Percussion.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
