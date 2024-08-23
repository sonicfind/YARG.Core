using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public abstract class PhraseTrack : ITrack
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Overdrives;
        public readonly YARGNativeSortedList<DualTime, DualTime> Soloes;
        public readonly YARGNativeSortedList<DualTime, DualTime> Trills;
        public readonly YARGNativeSortedList<DualTime, DualTime> Tremolos;
        public readonly YARGNativeSortedList<DualTime, DualTime> BREs;
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player1;
        public readonly YARGNativeSortedList<DualTime, DualTime> Faceoff_Player2;
        public readonly YARGManagedSortedList<DualTime, HashSet<string>> Events;

        protected PhraseTrack()
        {
            Overdrives = new();
            Soloes = new();
            Trills = new();
            Tremolos = new();
            BREs = new();
            Faceoff_Player1 = new();
            Faceoff_Player2 = new();
            Events = new();
        }

        protected PhraseTrack(PhraseTrack source)
        {
            Overdrives      = new(source.Overdrives);
            Soloes          = new(source.Soloes);
            Trills          = new(source.Trills);
            Tremolos        = new(source.Tremolos);
            BREs            = new(source.BREs);
            Faceoff_Player1 = new(source.Faceoff_Player1);
            Faceoff_Player2 = new(source.Faceoff_Player2);
            Events          = new(source.Events);
        }

        public virtual bool IsEmpty()
        {
            return Overdrives.IsEmpty()
                && Soloes.IsEmpty()
                && Trills.IsEmpty()
                && Tremolos.IsEmpty()
                && BREs.IsEmpty()
                && Faceoff_Player1.IsEmpty()
                && Faceoff_Player2.IsEmpty()
                && Events.IsEmpty();
        }

        public virtual void Clear()
        {
            Overdrives.Clear();
            Soloes.Clear();
            Trills.Clear();
            Tremolos.Clear();
            BREs.Clear();
            Faceoff_Player1.Clear();
            Faceoff_Player2.Clear();
            Events.Clear();
        }

        public virtual void TrimExcess()
        {
            Overdrives.TrimExcess();
            Soloes.TrimExcess();
            Trills.TrimExcess();
            Tremolos.TrimExcess();
            BREs.TrimExcess();
            Faceoff_Player1.TrimExcess();
            Faceoff_Player2.TrimExcess();
            // Ignore Events, as GC doesn't guarantee quick disposal of the old array
        }

        public virtual void Dispose()
        {
            Overdrives.Dispose();
            Soloes.Dispose();
            Trills.Dispose();
            Tremolos.Dispose();
            BREs.Dispose();
            Faceoff_Player1.Dispose();
            Faceoff_Player2.Dispose();
        }

        public abstract DualTime GetLastNoteTime();
    }
}
