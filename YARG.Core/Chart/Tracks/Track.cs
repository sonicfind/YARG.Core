using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public abstract class Track
    {
        public TimedFlatDictionary<List<SpecialPhrase_FW>> specialPhrases = new();
        public TimedFlatDictionary<List<string>> events = new();
        public virtual bool IsOccupied() { return !specialPhrases.IsEmpty() || !events.IsEmpty(); }
        public virtual void Clear()
        {
            specialPhrases.Clear();
            events.Clear();
        }
        public abstract void TrimExcess();
        public abstract long GetLastNoteTime();
    }
}
