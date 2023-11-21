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

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Arpeggios.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
