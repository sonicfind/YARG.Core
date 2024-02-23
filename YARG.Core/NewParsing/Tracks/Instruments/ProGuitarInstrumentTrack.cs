using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public enum ChordPhrase
    {
        Force_Numbering,
        Slash,
        Hide,
        Accidental_Switch
    };

    public class ProGuitarInstrumentTrack<TProFretConfig> : InstrumentTrack2<ProGuitarDifficultyTrack<TProFretConfig>>, IDisposable
        where TProFretConfig : unmanaged, IProFretConfig<TProFretConfig>
    {
        public readonly YARGNativeSortedList<DualTime, PitchName> Roots = new();
        public readonly YARGNativeSortedList<DualTime, HandPosition<TProFretConfig>> HandPositions = new();
        public readonly YARGManagedSortedList<DualTime, List<ChordPhrase>> ChordPhrases = new();

        public override bool IsOccupied()
        {
            return !Roots.IsEmpty() || !HandPositions.IsEmpty() || !ChordPhrases.IsEmpty() || base.IsOccupied();
        }

        public override void Clear()
        {
            Roots.Clear();
            HandPositions.Clear();
            ChordPhrases.Clear();
            base.Clear();
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
