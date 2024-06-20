using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class ProGuitarDifficultyTrack<TProFretConfig> : DifficultyTrack2<ProGuitarNote<TProFretConfig>>
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Arpeggios = new();

        public override bool IsOccupied() { return !Arpeggios.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            Arpeggios.Clear();
            base.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Arpeggios.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
