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
        public readonly YARGNativeSortedList<DualTime, DualTime> Glissandos = new();

        public new bool IsEmpty()
        {
            return Ranges.IsEmpty()
                && Glissandos.IsEmpty()
                && base.IsEmpty();
        }

        public new void TrimExcess()
        {
            Ranges.TrimExcess();
            Glissandos.TrimExcess();
            base.TrimExcess();
        }

        public new void Clear()
        {
            Ranges.Clear();
            Glissandos.Clear();
            base.Clear();
        }

        public new void Dispose()
        {
            Ranges.Dispose();
            Glissandos.Dispose();
            base.Dispose();
        }
    }
}
