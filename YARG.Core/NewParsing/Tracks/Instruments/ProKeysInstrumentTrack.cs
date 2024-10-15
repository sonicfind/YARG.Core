using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

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

    public class ProKeysInstrumentTrack : InstrumentTrack2<ProKeyNote>
    {
        public DifficultyExtensions<ProKey_Ranges> Ranges = DifficultyExtensions<ProKey_Ranges>.Default;
        public YARGNativeSortedList<DualTime, DualTime> Glissandos = YARGNativeSortedList<DualTime, DualTime>.Default;

        public override bool IsEmpty()
        {
            return Ranges.IsEmpty()
                && Glissandos.IsEmpty()
                && base.IsEmpty();
        }

        public override void TrimExcess()
        {
            Ranges.TrimExcess();
            Glissandos.TrimExcess();
            base.TrimExcess();
        }

        public override void Clear()
        {
            Ranges.Clear();
            Glissandos.Clear();
            base.Clear();
        }

        public override void Dispose(bool dispose)
        {
            Ranges.Dispose();
            Glissandos.Dispose();
            base.Dispose(dispose);
        }
    }
}
