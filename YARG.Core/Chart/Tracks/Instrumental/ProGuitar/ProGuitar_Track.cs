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
        public readonly TimedFlatDictionary<PitchName> roots = new();
        public readonly TimedFlatDictionary<HandPosition<TProFretConfig>> handPositions = new();
        public readonly TimedFlatDictionary<List<ChordPhrase>> chordPhrases = new();

        public override bool IsOccupied()
        {
            return !roots.IsEmpty() || !handPositions.IsEmpty() || !chordPhrases.IsEmpty() || base.IsOccupied();
        }

        public override void Clear()
        {
            base.Clear();
            roots.Clear();
            handPositions.Clear();
            chordPhrases.Clear();
        }
    }
}
