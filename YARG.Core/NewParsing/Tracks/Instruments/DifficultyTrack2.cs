using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class DifficultyTrack2<TNote> : Track
        where TNote : unmanaged, IInstrumentNote
    {
        public readonly YARGNativeSortedList<DualTime, TNote> Notes = new();

        public DifficultyTrack2() { }
        public DifficultyTrack2(int capcacity)
        {
            Notes.Capacity = capcacity;
        }

        public override bool IsOccupied() { return !Notes.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            Notes.Clear();
        }

        public override void TrimExcess() => Notes.TrimExcess();

        public override unsafe DualTime GetLastNoteTime()
        {
            if (Notes.IsEmpty())
            {
                return default;
            }

            var note = Notes.ElementAtIndex(Notes.Count - 1);
            return note->Key + note->Value.GetLongestSustain();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Notes.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
