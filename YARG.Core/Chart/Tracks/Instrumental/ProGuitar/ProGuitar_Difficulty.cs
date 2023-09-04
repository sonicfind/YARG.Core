using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public class ProGuitarDifficulty<TProFretConfig> : DifficultyTrack_FW<ProGuitarNote<TProFretConfig>>
        where TProFretConfig : IProFretConfig, new()
    {
        private TimedNativeFlatDictionary<Arpeggio<TProFretConfig>> _arpeggios = new();
        public TimedNativeFlatDictionary<Arpeggio<TProFretConfig>> Arpeggios => _arpeggios;

        public override bool IsOccupied() { return !_arpeggios.IsEmpty() || base.IsOccupied(); }

        public override void Clear()
        {
            base.Clear();
            _arpeggios.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            _notes.Dispose();
            _arpeggios.Dispose();
        }
    }
}
