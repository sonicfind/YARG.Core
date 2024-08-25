using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class ProGuitarDifficultyTrack<TProFret> : DifficultyTrack2<ProGuitarNote<TProFret>>
        where TProFret : unmanaged, IProFret
    {
        public readonly YARGNativeSortedList<DualTime, DualTime> Arpeggios = new();

        public override bool IsEmpty()
        {
            return Arpeggios.IsEmpty() && base.IsEmpty();
        }

        public override void Clear()
        {
            Arpeggios.Clear();
            base.Clear();
        }

        public override void TrimExcess()
        {
            Arpeggios.TrimExcess();
            base.TrimExcess();
        }

        public override void Dispose()
        {
            Arpeggios.Dispose();
            base.Dispose();
        }
    }
}
