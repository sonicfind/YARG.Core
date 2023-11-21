using System;
using System.Collections.Generic;
using YARG.Core.Parsing.Pitch;
using YARG.Core.Parsing.ProGuitar;

namespace YARG.Core.Parsing
{
    public enum ChordPhrase
    {
        Force_Numbering,
        Slash,
        Hide,
        Accidental_Switch
    };

    public class ProGuitarTrack<TProFretConfig> : InstrumentTrack_Base<ProGuitarDifficulty<TProFretConfig>>, IDisposable
        where TProFretConfig : IProFretConfig, new()
    {
        public readonly TimedNativeFlatDictionary<PitchName> Roots = new();
        public readonly TimedNativeFlatDictionary<HandPosition<TProFretConfig>> HandPositions = new();
        public readonly TimedManagedFlatDictionary<List<ChordPhrase>> ChordPhrases = new();

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

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Roots.Dispose();
                    HandPositions.Dispose();
                    ChordPhrases.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
