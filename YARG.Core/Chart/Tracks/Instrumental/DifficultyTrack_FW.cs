using System;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class DifficultyTrack_FW<T> : Track
        where T : unmanaged, INote
    {
        public readonly TimedNativeFlatDictionary<T> Notes = new();

        public DifficultyTrack_FW() { }
        public DifficultyTrack_FW(int capcacity)
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

        public override long GetLastNoteTime()
        {
            if (Notes.IsEmpty()) return 0;
            var note = Notes.At_index(Notes.Count - 1);
            return note.position + note.obj.GetLongestSustain();
        }

        public override void Dispose()
        {
            Notes.Dispose();
        }
    }
}
