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
        private TimedNativeFlatDictionary<ProKey_Ranges> _ranges = new();
        public TimedNativeFlatDictionary<ProKey_Ranges> Ranges => _ranges;

        public override bool IsOccupied() { return !_ranges.IsEmpty() || base.IsOccupied(); }
        public override void Clear()
        {
            base.Clear();
            _ranges.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _ranges.Dispose();
        }
    }
}
