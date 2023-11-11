using System;
using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public abstract class Track : IDisposable
    {
        public TimedFlatDictionary<List<SpecialPhrase_FW>> SpecialPhrases = new();
        public TimedFlatDictionary<List<string>> Events = new();
        protected bool disposedValue;

        public virtual bool IsOccupied() { return !SpecialPhrases.IsEmpty() || !Events.IsEmpty(); }
        public virtual void Clear()
        {
            SpecialPhrases.Clear();
            Events.Clear();
        }

        public abstract void TrimExcess();
        public abstract long GetLastNoteTime();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SpecialPhrases.Clear();
                    Events.Clear();
                }

                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Track()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
