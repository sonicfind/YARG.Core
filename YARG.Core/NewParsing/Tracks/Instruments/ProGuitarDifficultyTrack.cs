using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class ProGuitarDifficultyTrack<TProFret> : DifficultyTrack2<ProGuitarNote<TProFret>>
        where TProFret : unmanaged, IProFret
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Arpeggios = new();

        public new bool IsEmpty()
        {
            return Arpeggios.IsEmpty() && base.IsEmpty();
        }

        public new void Clear()
        {
            Arpeggios.Clear();
            base.Clear();
        }

        public new void TrimExcess()
        {
            Arpeggios.TrimExcess();
            base.TrimExcess();
        }

        public new void Dispose()
        {
            Arpeggios.Dispose();
            base.Dispose();
        }
    }
}
