using System;
using System.Collections.Generic;
using System.Text;

namespace YARG.Core.NewParsing
{
    public class ProGuitarInstrumentTrack<TProFret> : InstrumentTrack2<ProGuitarDifficultyTrack<TProFret>>, IDisposable
        where TProFret : unmanaged, IProFret
    {
        public readonly YARGNativeSortedList<DualTime, PitchName> Roots = new();
        public readonly YARGNativeSortedList<DualTime, TProFret> HandPositions = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Force_ChordNumbering = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> SlashChords = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> HideChords = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> AccidentalSwitches = new();

        public new bool IsEmpty()
        {
            return Roots.IsEmpty()
                && HandPositions.IsEmpty()
                && Force_ChordNumbering.IsEmpty()
                && SlashChords.IsEmpty()
                && HideChords.IsEmpty()
                && AccidentalSwitches.IsEmpty()
                && base.IsEmpty();
        }

        public new void TrimExcess()
        {
            Roots.TrimExcess();
            HandPositions.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
            base.TrimExcess();
        }

        public new void Clear()
        {
            Roots.Clear();
            HandPositions.Clear();
            Force_ChordNumbering.Clear();
            SlashChords.Clear();
            HideChords.Clear();
            AccidentalSwitches.Clear();
            base.Clear();
        }

        public new void Dispose()
        {
            Roots.Dispose();
            HandPositions.Dispose();
            Force_ChordNumbering.Dispose();
            SlashChords.Dispose();
            HideChords.Dispose();
            AccidentalSwitches.Dispose();
            base.Dispose();
        }
    }
}
