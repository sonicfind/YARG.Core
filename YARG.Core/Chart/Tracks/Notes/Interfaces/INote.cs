using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.Chart
{
    public interface INote
    {
        public bool HasActiveNotes();
        public long GetLongestSustain();
    }
}
