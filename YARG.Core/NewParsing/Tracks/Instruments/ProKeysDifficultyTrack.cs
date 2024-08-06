using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum ProKey_Ranges
    {
        C1_E2,
        D1_F2,
        E1_G2,
        F1_A2,
        G1_B2,
        A1_C3,
    };

    public class ProKeysDifficultyTrack : DifficultyTrack2<ProKeyNote>
    {
        public readonly YARGNativeSortedList<DualTime, ProKey_Ranges> Ranges = new();

        public override bool IsEmpty() { return Ranges.IsEmpty() && base.IsEmpty(); }
        public override void Clear()
        {
            Ranges.Clear();
            base.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Ranges.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
