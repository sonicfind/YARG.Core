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
        public YARGNativeSortedList<DualTime, ProKey_Ranges> Ranges_Easy   = YARGNativeSortedList<DualTime, ProKey_Ranges>.Default;
        public YARGNativeSortedList<DualTime, ProKey_Ranges> Ranges_Medium = YARGNativeSortedList<DualTime, ProKey_Ranges>.Default;
        public YARGNativeSortedList<DualTime, ProKey_Ranges> Ranges_Hard   = YARGNativeSortedList<DualTime, ProKey_Ranges>.Default;
        public YARGNativeSortedList<DualTime, ProKey_Ranges> Ranges_Expert = YARGNativeSortedList<DualTime, ProKey_Ranges>.Default;
        public YARGNativeSortedList<DualTime, DualTime>      Glissandos    = YARGNativeSortedList<DualTime, DualTime>.Default;

        public ref YARGNativeSortedList<DualTime, ProKey_Ranges> GetRanges(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Beginner:
                case Difficulty.Easy:
                    return ref Ranges_Easy;
                case Difficulty.Medium:
                    return ref Ranges_Medium;
                case Difficulty.Hard:
                    return ref Ranges_Hard;
                case Difficulty.Expert:
                    return ref Ranges_Expert;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public ref YARGNativeSortedList<DualTime, ProKey_Ranges> GetRanges(int index)
        {
            switch (index)
            {
                case 0: return ref Ranges_Easy;
                case 1: return ref Ranges_Medium;
                case 2: return ref Ranges_Hard;
                case 3: return ref Ranges_Expert;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public override bool IsEmpty()
        {
            return Ranges_Easy.IsEmpty()
                && Ranges_Medium.IsEmpty()
                && Ranges_Hard.IsEmpty()
                && Ranges_Expert.IsEmpty()
                && Glissandos.IsEmpty()
                && base.IsEmpty();
        }

        public override void TrimExcess()
        {
            Ranges_Easy.TrimExcess();
            Ranges_Medium.TrimExcess();
            Ranges_Hard.TrimExcess();
            Ranges_Expert.TrimExcess();
            Glissandos.TrimExcess();
            base.TrimExcess();
        }

        public override void Clear()
        {
            Ranges_Easy.Clear();
            Ranges_Medium.Clear();
            Ranges_Hard.Clear();
            Ranges_Expert.Clear();
            Glissandos.Clear();
            base.Clear();
        }

        public override void Dispose(bool dispose)
        {
            Ranges_Easy.Dispose();
            Ranges_Medium.Dispose();
            Ranges_Hard.Dispose();
            Ranges_Expert.Dispose();
            Glissandos.Dispose();
            base.Dispose(dispose);
        }
    }
}
