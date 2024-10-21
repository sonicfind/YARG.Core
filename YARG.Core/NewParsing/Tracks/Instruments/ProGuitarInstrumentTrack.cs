using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public struct ProGuitarInstrumentTrack<TProFret> : ITrack
        where TProFret : unmanaged, IProFret
    {
        public static readonly ProGuitarInstrumentTrack<TProFret> Default = new()
        {
            Difficulties = DifficultyTrackCollection<ProGuitarNote<TProFret>>.Default,
            Arpeggios = DifficultyExtensions<DualTime>.Default,
            Roots = YARGNativeSortedList<DualTime, PitchName>.Default,
            HandPositions = YARGNativeSortedList<DualTime, TProFret>.Default,
            Force_ChordNumbering = YARGNativeSortedList<DualTime, DualTime>.Default,
            SlashChords = YARGNativeSortedList<DualTime, DualTime>.Default,
            HideChords = YARGNativeSortedList<DualTime, DualTime>.Default,
            AccidentalSwitches = YARGNativeSortedList<DualTime, DualTime>.Default,
            Events = YARGManagedSortedList<DualTime, HashSet<string>>.Default,
        };

        public DifficultyTrackCollection<ProGuitarNote<TProFret>> Difficulties;
        public DifficultyExtensions<DualTime>                     Arpeggios;
        public YARGNativeSortedList<DualTime, PitchName>          Roots;
        public YARGNativeSortedList<DualTime, TProFret>           HandPositions;
        public YARGNativeSortedList<DualTime, DualTime>           Force_ChordNumbering;
        public YARGNativeSortedList<DualTime, DualTime>           SlashChords;
        public YARGNativeSortedList<DualTime, DualTime>           HideChords;
        public YARGNativeSortedList<DualTime, DualTime>           AccidentalSwitches;
        public YARGManagedSortedList<DualTime, HashSet<string>>   Events;

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public readonly bool IsEmpty()
        {
            return Difficulties.IsEmpty()
                && Roots.IsEmpty()
                && HandPositions.IsEmpty()
                && Arpeggios.IsEmpty()
                && Force_ChordNumbering.IsEmpty()
                && SlashChords.IsEmpty()
                && HideChords.IsEmpty()
                && AccidentalSwitches.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public void Clear()
        {
            Difficulties.Clear();
            Roots.Clear();
            HandPositions.Clear();
            Arpeggios.Clear();
            Force_ChordNumbering.Clear();
            SlashChords.Clear();
            HideChords.Clear();
            AccidentalSwitches.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties and the track's phrases.<br></br>
        /// This will also delete any completely empty difficulties.
        /// </summary>
        public void TrimExcess()
        {
            Difficulties.TrimExcess();
            Roots.TrimExcess();
            HandPositions.TrimExcess();
            Arpeggios.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
        }

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            Difficulties.UpdateLastNoteTime(ref lastNoteTime);
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        public void Dispose(bool dispose)
        {
            Difficulties.Dispose(dispose);
            Roots.Dispose();
            HandPositions.Dispose();
            Arpeggios.Dispose();
            Force_ChordNumbering.Dispose();
            SlashChords.Dispose();
            HideChords.Dispose();
            AccidentalSwitches.Dispose();
            if (dispose)
            {
                Events.Dispose();
            }
        }
    }
}
