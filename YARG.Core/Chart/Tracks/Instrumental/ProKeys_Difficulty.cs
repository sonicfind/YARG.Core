using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.ProKeys;

namespace YARG.Core.Chart
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

    public class ProKeysDifficulty : DifficultyTrack_FW<ProKeyNote>
    {
        public readonly TimedFlatDictionary<ProKey_Ranges> ranges = new();

        public override bool IsOccupied() { return !ranges.IsEmpty() || base.IsOccupied(); }
        public override void Clear()
        {
            base.Clear();
            ranges.Clear();
        }
    }
}
