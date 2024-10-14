using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Holds the notes and lyrics for a specific vocal line
    /// </summary>
    public struct VocalPart2 : IDisposable
    {
        public static readonly VocalPart2 Default = new()
        {
            Notes = YARGNativeSortedList<DualTime, VocalNote2>.Default,
            Lyrics = YARGManagedSortedList<DualTime, NonNullString>.Default,
        };

        public YARGNativeSortedList<DualTime, VocalNote2> Notes;
        public YARGManagedSortedList<DualTime, NonNullString> Lyrics;

        public readonly bool IsEmpty()
        {
            return Notes.IsEmpty() && Lyrics.IsEmpty();
        }

        public readonly void TrimExcess()
        {
            if ((Notes.Count < 100 || 2000 <= Notes.Count) && Notes.Count < Notes.Capacity)
            {
                Notes.TrimExcess();
            }
        }

        public readonly void Clear()
        {
            Notes.Clear();
            Lyrics.Clear();
        }

        public readonly void Dispose()
        {
            Notes.Dispose();
            Lyrics.Clear();
        }

        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            if (Notes.IsEmpty())
            {
                return;
            }

            unsafe
            {
                ref readonly var vocal = ref Notes.Data[Notes.Count - 1];
                var end = vocal.Key + vocal.Value.Duration;
                if (end > lastNoteTime)
                {
                    lastNoteTime = end;
                }
            }
        }
    }
}
