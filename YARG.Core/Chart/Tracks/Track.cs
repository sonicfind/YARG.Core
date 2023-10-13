using System;
using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public abstract class Track : IDisposable
    {
        public TimedFlatDictionary<List<SpecialPhrase_FW>> SpecialPhrases = new();
        public TimedFlatDictionary<List<string>> Events = new();
        public virtual bool IsOccupied() { return !SpecialPhrases.IsEmpty() || !Events.IsEmpty(); }
        public virtual void Clear()
        {
            SpecialPhrases.Clear();
            Events.Clear();
        }

        public virtual void Dispose() { }

        public abstract void TrimExcess();
        public abstract long GetLastNoteTime();
    }
}
