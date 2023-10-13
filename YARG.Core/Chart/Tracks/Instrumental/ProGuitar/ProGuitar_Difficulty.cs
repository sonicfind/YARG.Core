using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public class ProGuitarDifficulty<TProFretConfig> : DifficultyTrack_FW<ProGuitarNote<TProFretConfig>>
        where TProFretConfig : IProFretConfig, new()
    {
        public readonly TimedNativeFlatDictionary<Arpeggio<TProFretConfig>> Arpeggios = new();

        public override bool IsOccupied() { return !Arpeggios.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            Arpeggios.Clear();
        }

        public override void Dispose()
        {
            base.Dispose();
            Arpeggios.Dispose();
        }
    }
}
