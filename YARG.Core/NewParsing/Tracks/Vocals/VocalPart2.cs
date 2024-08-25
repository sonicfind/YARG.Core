using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    /// <summary>
    /// Holds the notes and lyrics for a specific vocal line
    /// </summary>
    public struct VocalPart2
    {
        public YARGNativeSortedList<DualTime, VocalNote2> Notes;
        public YARGManagedSortedList<DualTime, NonNullString> Lyrics;

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

        public readonly bool IsEmpty()
        {
            return Notes.IsEmpty() && Lyrics.IsEmpty();
        }
    }
}
