using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarInstrumentTrack<TProFret> : InstrumentTrack2<ProGuitarNote<TProFret>>, IDisposable
        where TProFret : unmanaged, IProFret
    {
        public DifficultyExtensions<DualTime> Arpeggios = DifficultyExtensions<DualTime>.Default;
        public YARGNativeSortedList<DualTime, PitchName> Roots                = YARGNativeSortedList<DualTime, PitchName>.Default;
        public YARGNativeSortedList<DualTime, TProFret>  HandPositions        = YARGNativeSortedList<DualTime, TProFret>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Force_ChordNumbering = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  SlashChords          = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  HideChords           = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  AccidentalSwitches   = YARGNativeSortedList<DualTime, DualTime>.Default;

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public override bool IsEmpty()
        {
            return Roots.IsEmpty()
                && HandPositions.IsEmpty()
                && Arpeggios.IsEmpty()
                && Force_ChordNumbering.IsEmpty()
                && SlashChords.IsEmpty()
                && HideChords.IsEmpty()
                && AccidentalSwitches.IsEmpty()
                && base.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public override void Clear()
        {
            Roots.Clear();
            HandPositions.Clear();
            Arpeggios.Clear();
            Force_ChordNumbering.Clear();
            SlashChords.Clear();
            HideChords.Clear();
            AccidentalSwitches.Clear();
            base.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties and the track's phrases.<br></br>
        /// This will also delete any completely empty difficulties.
        /// </summary>
        public override void TrimExcess()
        {
            Roots.TrimExcess();
            HandPositions.TrimExcess();
            Arpeggios.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
            base.TrimExcess();
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        public override void Dispose(bool dispose)
        {
            Roots.Dispose();
            HandPositions.Dispose();
            Arpeggios.Dispose();
            Force_ChordNumbering.Dispose();
            SlashChords.Dispose();
            HideChords.Dispose();
            AccidentalSwitches.Dispose();
            base.Dispose(dispose);
        }
    }
}
