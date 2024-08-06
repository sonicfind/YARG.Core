using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum SpecialPhraseType
    {
        FaceOff_Player1 = 0,
        FaceOff_Player2 = 1,
        StarPower = 2,
        Solo = 3,
        LyricLine = 4,
        RangeShift = 5,
        HarmonyLine = 6,
        StarPower_Diff = 8,
        BRE = 64,
        Tremolo = 65,
        Trill = 66,
        LyricShift = 67,
        Glissando = 68,
    }

    public abstract class Track : IDisposable
    {
        public YARGManagedSortedList<DualTime, Dictionary<SpecialPhraseType, (DualTime Duration, int Velocity)>> SpecialPhrases = new();
        public YARGManagedSortedList<DualTime, HashSet<string>> Events = new();
        protected bool disposedValue;

        public virtual bool IsEmpty() { return SpecialPhrases.IsEmpty() && Events.IsEmpty(); }
        public virtual void Clear()
        {
            SpecialPhrases.Clear();
            Events.Clear();
        }

        public abstract void TrimExcess();
        public abstract DualTime GetLastNoteTime();

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
