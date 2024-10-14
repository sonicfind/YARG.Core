using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarInstrumentTrack<TProFret> : InstrumentTrack2<ProGuitarNote<TProFret>>, IDisposable
        where TProFret : unmanaged, IProFret
    {
        public YARGNativeSortedList<DualTime, PitchName> Roots                = YARGNativeSortedList<DualTime, PitchName>.Default;
        public YARGNativeSortedList<DualTime, TProFret>  HandPositions        = YARGNativeSortedList<DualTime, TProFret>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Arpeggios_Easy       = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Arpeggios_Medium     = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Arpeggios_Hard       = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Arpeggios_Expert     = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  Force_ChordNumbering = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  SlashChords          = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  HideChords           = YARGNativeSortedList<DualTime, DualTime>.Default;
        public YARGNativeSortedList<DualTime, DualTime>  AccidentalSwitches   = YARGNativeSortedList<DualTime, DualTime>.Default;

        public ref YARGNativeSortedList<DualTime, DualTime> GetArpeggios(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Beginner:
                case Difficulty.Easy:
                    return ref Arpeggios_Easy;
                case Difficulty.Medium:
                    return ref Arpeggios_Medium;
                case Difficulty.Hard:
                    return ref Arpeggios_Hard;
                case Difficulty.Expert:
                    return ref Arpeggios_Expert;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public ref YARGNativeSortedList<DualTime, DualTime> GetArpeggios(int index)
        {
            switch (index)
            {
                case 0: return ref Arpeggios_Easy;
                case 1: return ref Arpeggios_Medium;
                case 2: return ref Arpeggios_Hard;
                case 3: return ref Arpeggios_Expert;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public override bool IsEmpty()
        {
            return Roots.IsEmpty()
                && HandPositions.IsEmpty()
                && Arpeggios_Easy.IsEmpty()
                && Arpeggios_Medium.IsEmpty()
                && Arpeggios_Hard.IsEmpty()
                && Arpeggios_Expert.IsEmpty()
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
            Arpeggios_Easy.Clear();
            Arpeggios_Medium.Clear();
            Arpeggios_Hard.Clear();
            Arpeggios_Expert.Clear();
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
            Arpeggios_Easy.TrimExcess();
            Arpeggios_Medium.TrimExcess();
            Arpeggios_Hard.TrimExcess();
            Arpeggios_Expert.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
            base.TrimExcess();
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        protected override void _Dispose()
        {
            Roots.Dispose();
            HandPositions.Dispose();
            Arpeggios_Easy.Dispose();
            Arpeggios_Medium.Dispose();
            Arpeggios_Hard.Dispose();
            Arpeggios_Expert.Dispose();
            Force_ChordNumbering.Dispose();
            SlashChords.Dispose();
            HideChords.Dispose();
            AccidentalSwitches.Dispose();
            base._Dispose();
        }
    }
}
