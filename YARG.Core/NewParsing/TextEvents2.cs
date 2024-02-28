using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class TextEvents2 : IDisposable
    {
        public readonly YARGManagedSortedList<DualTime, SongSection2> Sections = new();
        public readonly YARGManagedSortedList<DualTime, List<string>> Globals = new();

        public bool IsOccupied()
        {
            return !Sections.IsEmpty() || !Globals.IsEmpty();
        }

        public void Clear()
        {
            Sections.Clear();
            Globals.Clear();
        }

        public void Dispose()
        {
            Sections.Dispose();
            Globals.Dispose();
        }
    }
}
