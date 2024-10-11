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
        public readonly YARGNativeSortedList<DualTime, ProKey_Ranges>[] Ranges = new YARGNativeSortedList<DualTime, ProKey_Ranges>[InstrumentTrack2.NUM_DIFFICULTIES];
        public readonly YARGNativeSortedList<DualTime, DualTime> Glissandos = new();

        public ProKeysInstrumentTrack()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                Ranges[i] = new YARGNativeSortedList<DualTime, ProKey_Ranges>();
            }
        }

        public override bool IsEmpty()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                if (!Ranges[i].IsEmpty())
                {
                    return false;
                }
            }
            return Glissandos.IsEmpty() && base.IsEmpty();
        }

        public override void TrimExcess()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                Ranges[i].TrimExcess();
            }
            Glissandos.TrimExcess();
            base.TrimExcess();
        }

        public override void Clear()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                Ranges[i].Clear();
            }
            Glissandos.Clear();
            base.Clear();
        }

        public override void Dispose()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                Ranges[i].Dispose();
            }
            Glissandos.Dispose();
            base.Dispose();
        }
    }
}
