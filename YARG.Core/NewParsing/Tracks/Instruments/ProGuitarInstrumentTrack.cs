using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarInstrumentTrack<TProFret> : InstrumentTrack2<ProGuitarNote<TProFret>>, IDisposable
        where TProFret : unmanaged, IProFret
    {
        public readonly YARGNativeSortedList<DualTime, DualTime>[] Arpeggios = new YARGNativeSortedList<DualTime, DualTime>[InstrumentTrack2.NUM_DIFFICULTIES];
        public readonly YARGNativeSortedList<DualTime, PitchName> Roots = new();
        public readonly YARGNativeSortedList<DualTime, TProFret> HandPositions = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> Force_ChordNumbering = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> SlashChords = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> HideChords = new();
        public readonly YARGNativeSortedList<DualTime, DualTime> AccidentalSwitches = new();

        public ProGuitarInstrumentTrack()
        {
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; i++)
            {
                Arpeggios[i] = new YARGNativeSortedList<DualTime, DualTime>();
            }
        }

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public override bool IsEmpty()
        {
            foreach (var arp in Arpeggios)
            {
                if (!arp.IsEmpty())
                {
                    return false;
                }
            }
            return Roots.IsEmpty()
                && HandPositions.IsEmpty()
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
            foreach (var arp in Arpeggios)
            {
                arp.Clear();
            }
            Roots.Clear();
            HandPositions.Clear();
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
            foreach(var arp in Arpeggios)
            {
                arp.TrimExcess();
            }
            Roots.TrimExcess();
            HandPositions.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
            base.TrimExcess();
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        public override void Dispose()
        {
            foreach (var arp in Arpeggios)
            {
                arp.Dispose();
            }
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
