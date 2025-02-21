using System.Collections;
using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class ProKeysInstrumentTrack : ITrack, IEnumerable<ProKeysDifficultyTrack>
    {
        private readonly ProKeysDifficultyTrack[] _difficulties = new ProKeysDifficultyTrack[InstrumentTrack2.NUM_DIFFICULTIES];
        public YargNativeSortedList<DualTime, DualTime> Glissandos { get; }
        public YargManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public ProKeysDifficultyTrack this[int index] => _difficulties[index];

        public ProKeysDifficultyTrack this[Difficulty difficulty] => _difficulties[InstrumentTrack2.DifficultyToIndex(difficulty)];

        public ProKeysDifficultyTrack Easy =>   _difficulties[0];
        public ProKeysDifficultyTrack Medium => _difficulties[1];
        public ProKeysDifficultyTrack Hard =>   _difficulties[2];
        public ProKeysDifficultyTrack Expert => _difficulties[3];

        public ProKeysInstrumentTrack()
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new();
            }
            Glissandos = new();
            Events = new();
        }

        public ProKeysInstrumentTrack(ProKeysInstrumentTrack source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new(source[i]);
            }
            Glissandos = new(source.Glissandos);
            Events = new(source.Events);
        }

        public void CopyFrom(ProKeysInstrumentTrack source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i].CopyFrom(source[i]);
            }
            Glissandos.CopyFrom(source.Glissandos);
            Events.CopyFrom(source.Events);
        }

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases (and events) are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public bool IsEmpty()
        {
            foreach (var diff in _difficulties)
            {
                if (diff != null && !diff.IsEmpty())
                {
                    return false;
                }
            }
            return Glissandos.IsEmpty() && Events.IsEmpty();
        }

        public void TrimExcess()
        {
            foreach (var diff in _difficulties)
            {
                diff.TrimExcess();
            }
            Glissandos.TrimExcess();
        }

        public void Clear()
        {
            foreach (var diff in _difficulties)
            {
                diff.Clear();
            }
            Glissandos.Clear();
            Events.Clear();
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

        public void Dispose()
        {
            foreach (var diff in _difficulties)
            {
                diff.Dispose();
            }
            Glissandos.Dispose();
            Events.Dispose();
        }

        public IEnumerator<ProKeysDifficultyTrack> GetEnumerator()
        {
            return ((IEnumerable<ProKeysDifficultyTrack>) _difficulties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _difficulties.GetEnumerator();
        }
    }
}
