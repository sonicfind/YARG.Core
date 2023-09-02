using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class ProGuitarDifficulty<FretType> : DifficultyTrack_FW<Guitar_Pro<FretType>>
        where FretType : unmanaged, IFretted
    {
        public readonly TimedFlatDictionary<Arpeggio<FretType>> arpeggios = new();

        public override bool IsOccupied() { return !arpeggios.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            arpeggios.Clear();
        }
    }
}
