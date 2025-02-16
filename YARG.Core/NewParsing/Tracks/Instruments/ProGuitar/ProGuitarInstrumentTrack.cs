using System.Collections;
using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProGuitarInstrumentTrack<TProFret> : ITrack, IEnumerable<ProGuitarDifficultyTrack<TProFret>>
        where TProFret : unmanaged, IProFret
    {
        private readonly ProGuitarDifficultyTrack<TProFret>[] _difficulties = new ProGuitarDifficultyTrack<TProFret>[InstrumentTrack2.NUM_DIFFICULTIES];
        public YARGNativeSortedList<DualTime, PitchName> Roots { get; }
        public YARGNativeSortedList<DualTime, TProFret>  HandPositions { get; }
        public YARGNativeSortedList<DualTime, DualTime>  Force_ChordNumbering { get; }
        public YARGNativeSortedList<DualTime, DualTime>  SlashChords { get; }
        public YARGNativeSortedList<DualTime, DualTime>  HideChords { get; }
        public YARGNativeSortedList<DualTime, DualTime>  AccidentalSwitches { get; }
        public YARGManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public ProGuitarDifficultyTrack<TProFret> this[int index] => _difficulties[index];

        public ProGuitarDifficultyTrack<TProFret> this[Difficulty difficulty] => _difficulties[InstrumentTrack2.DifficultyToIndex(difficulty)];

        public ProGuitarDifficultyTrack<TProFret> Easy =>   _difficulties[0];
        public ProGuitarDifficultyTrack<TProFret> Medium => _difficulties[1];
        public ProGuitarDifficultyTrack<TProFret> Hard =>   _difficulties[2];
        public ProGuitarDifficultyTrack<TProFret> Expert => _difficulties[3];

        public ProGuitarInstrumentTrack()
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new();
            }
            Roots = new();
            HandPositions = new();
            Force_ChordNumbering = new();
            SlashChords = new();
            HideChords = new();
            AccidentalSwitches = new();
            Events = new();
        }

        public ProGuitarInstrumentTrack(ProGuitarInstrumentTrack<TProFret> source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new(source[i]);
            }
            Roots = new(source.Roots);
            HandPositions = new(source.HandPositions);
            Force_ChordNumbering = new(source.Force_ChordNumbering);
            SlashChords = new(source.SlashChords);
            HideChords = new(source.HideChords);
            AccidentalSwitches = new(source.AccidentalSwitches);
            Events = new(source.Events);
        }

        public void CopyFrom(ProGuitarInstrumentTrack<TProFret> source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i].CopyFrom(source[i]);
            }
            Roots.CopyFrom(source.Roots);
            HandPositions.CopyFrom(source.HandPositions);
            Force_ChordNumbering.CopyFrom(source.Force_ChordNumbering);
            SlashChords.CopyFrom(source.SlashChords);
            HideChords.CopyFrom(source.HideChords);
            AccidentalSwitches.CopyFrom(source.AccidentalSwitches);
            Events.CopyFrom(source.Events);
        }

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public bool IsEmpty()
        {
            foreach (var diff in _difficulties)
            {
                if (!diff.IsEmpty())
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
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public void Clear()
        {
            foreach (var diff in _difficulties)
            {
                diff.Clear();
            }
            Roots.Clear();
            HandPositions.Clear();
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
            foreach (var diff in _difficulties)
            {
                diff.TrimExcess();
            }
            Roots.TrimExcess();
            HandPositions.TrimExcess();
            Force_ChordNumbering.TrimExcess();
            SlashChords.TrimExcess();
            HideChords.TrimExcess();
            AccidentalSwitches.TrimExcess();
        }

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
        public void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            foreach (var diff in _difficulties)
            {
                diff.UpdateLastNoteTime(ref lastNoteTime);
            }
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        public void Dispose()
        {
            foreach (var diff in _difficulties)
            {
                diff.Dispose();
            }
            Roots.Dispose();
            HandPositions.Dispose();
            Force_ChordNumbering.Dispose();
            SlashChords.Dispose();
            HideChords.Dispose();
            AccidentalSwitches.Dispose();
            Events.Dispose();
        }

        public IEnumerator<ProGuitarDifficultyTrack<TProFret>> GetEnumerator()
        {
            return ((IEnumerable<ProGuitarDifficultyTrack<TProFret>>) _difficulties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _difficulties.GetEnumerator();
        }
    }
}
