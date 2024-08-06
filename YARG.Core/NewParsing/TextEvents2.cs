using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class TextEvents2
    {
        public readonly YARGManagedSortedList<DualTime, NonNullString> Sections = new();
        public readonly YARGManagedSortedList<DualTime, List<string>> Globals = new();

        public bool IsEmpty()
        {
            return Sections.IsEmpty() && Globals.IsEmpty();
        }

        public void Clear()
        {
            Sections.Clear();
            Globals.Clear();
        }
    }
}
