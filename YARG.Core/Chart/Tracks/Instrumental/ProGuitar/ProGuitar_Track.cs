using System.Collections.Generic;
using YARG.Core.Chart.FlatDictionary;
using YARG.Core.Chart.Pitch;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public enum ChordPhrase
    {
        Force_Numbering,
        Slash,
        Hide,
        Accidental_Switch
    };

    public class ProGuitarTrack<TProFretConfig> : InstrumentTrack_Base<ProGuitarDifficulty<TProFretConfig>>
        where TProFretConfig : IProFretConfig, new()
    {
        public readonly TimedFlatDictionary<PitchName> Roots = new();
        public readonly TimedFlatDictionary<HandPosition<TProFretConfig>> HandPositions = new();
        public readonly TimedFlatDictionary<List<ChordPhrase>> ChordPhrases = new();

        public override bool IsOccupied()
        {
            return !Roots.IsEmpty() || !HandPositions.IsEmpty() || !ChordPhrases.IsEmpty() || base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            Roots.Clear();
            HandPositions.Clear();
            ChordPhrases.Clear();
        }
    }
}
