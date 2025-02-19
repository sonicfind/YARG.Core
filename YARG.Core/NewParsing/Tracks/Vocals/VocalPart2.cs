using System;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Holds the notes and lyrics for a specific vocal line
    /// </summary>
    public class VocalPart2 : IDisposable
    {
        public YARGNativeSortedList<DualTime, VocalNote2> Notes { get; } = new();
        public YARGManagedSortedList<DualTime, NonNullString> Lyrics { get; } = new();

        public bool IsEmpty()
        {
            return Notes.IsEmpty() && Lyrics.IsEmpty();
        }

        public void TrimExcess()
        {
            if ((Notes.Count < 100 || 2000 <= Notes.Count) && Notes.Count < Notes.Capacity)
            {
                Notes.TrimExcess();
            }
        }

        public void Clear()
        {
            Notes.Clear();
            Lyrics.Clear();
        }

        public void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (Notes.IsEmpty())
            {
                return;
            }

            ref readonly var vocal = ref Notes[Notes.Count - 1];
            var end = vocal.Key + vocal.Value.Duration;
            if (end > lastNoteTime)
            {
                lastNoteTime = end;
            }
        }

        public void Dispose()
        {
            Notes.Dispose();
            Lyrics.Dispose();
        }
    }
}
