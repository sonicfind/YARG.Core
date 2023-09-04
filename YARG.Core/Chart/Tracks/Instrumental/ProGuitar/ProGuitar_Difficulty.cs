using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public class ProGuitarDifficulty<TProFretConfig> : DifficultyTrack_FW<ProGuitarNote<TProFretConfig>>
        where TProFretConfig : IProFretConfig, new()
    {
        public readonly TimedFlatDictionary<Arpeggio<TProFretConfig>> arpeggios = new();

        public override bool IsOccupied() { return !arpeggios.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            arpeggios.Clear();
        }
    }
}
