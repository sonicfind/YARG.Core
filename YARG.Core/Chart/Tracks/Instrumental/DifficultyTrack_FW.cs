using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class DifficultyTrack_FW<T> : Track
        where T : INote, new()
    {
        public readonly TimedFlatDictionary<T> notes = new();

        public DifficultyTrack_FW() { }
        public DifficultyTrack_FW(int capcacity)
        {
            notes.Capacity = capcacity;
        }

        public override bool IsOccupied() { return !notes.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            notes.Clear();
        }

        public override void TrimExcess() => notes.TrimExcess();

        public override long GetLastNoteTime()
        {
            if (notes.IsEmpty()) return 0;
            var note = notes.At_index(notes.Count - 1);
            return note.position + note.obj.GetLongestSustain();
        }
    }
}
