using System.Collections.Generic;

namespace YARG.Core.Chart
{
    public enum ChordPhrase
    {
        Force_Numbering,
        Slash,
        Hide,
        Accidental_Switch
    };

    public class ProGuitarTrack<FretType> : InstrumentTrack_Base<ProGuitarDifficulty<FretType>>
        where FretType : unmanaged, IFretted
    {
        public readonly TimedFlatDictionary<PitchName> roots = new();
        public readonly TimedFlatDictionary<FretType> handPositions = new();
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
