using System;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class DifficultyTrack_FW<T> : Track, IDisposable
        where T : unmanaged, INote
    {
        private bool disposedValue;
        protected TimedNativeFlatDictionary<T> _notes = new();
        public TimedNativeFlatDictionary<T> Notes => _notes;
        

        public DifficultyTrack_FW() { }
        public DifficultyTrack_FW(int capcacity)
        {
            _notes.Capacity = capcacity;
        }

        public override bool IsOccupied() { return !_notes.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            _notes.Clear();
        }

        public override void TrimExcess() => _notes.TrimExcess();

        public override long GetLastNoteTime()
        {
            if (_notes.IsEmpty()) return 0;
            var note = _notes.At_index(_notes.Count - 1);
            return note.position + note.obj.GetLongestSustain();
        }

        protected virtual void Dispose(bool disposing)
        {
            _notes.Dispose();
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
                disposedValue = true;
            }
        }
    }
}
