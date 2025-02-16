using System;
using System.Collections;
using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public static class InstrumentTrack2
    {
        public const int NUM_DIFFICULTIES = 4;
        public static int DifficultyToIndex(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Beginner or
                Difficulty.Easy => 0,
                Difficulty.Medium => 1,
                Difficulty.Hard => 2,
                Difficulty.Expert or
                Difficulty.ExpertPlus => 3,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    public class InstrumentTrack2<TNote> : ITrack, IEnumerable<DifficultyTrack2<TNote>>
        where TNote : unmanaged, IInstrumentNote
    {
        private readonly DifficultyTrack2<TNote>[] _difficulties = new DifficultyTrack2<TNote>[InstrumentTrack2.NUM_DIFFICULTIES];
        public YARGManagedSortedList<DualTime, HashSet<string>> Events { get; }

        public DifficultyTrack2<TNote> this[int index] => _difficulties[index];
        public DifficultyTrack2<TNote> this[Difficulty difficulty] => _difficulties[InstrumentTrack2.DifficultyToIndex(difficulty)];

        public DifficultyTrack2<TNote> Easy   => _difficulties[0];
        public DifficultyTrack2<TNote> Medium => _difficulties[1];
        public DifficultyTrack2<TNote> Hard   => _difficulties[2];
        public DifficultyTrack2<TNote> Expert => _difficulties[3];

        public InstrumentTrack2()
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new();
            }
            Events = new();
        }

        public InstrumentTrack2(InstrumentTrack2<TNote> source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i] = new(source[i]);
            }
            Events = new(source.Events);
        }

        public void CopyFrom(InstrumentTrack2<TNote> source)
        {
            for (int i = 0; i < _difficulties.Length; i++)
            {
                _difficulties[i].CopyFrom(source[i]);
            }
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
            return Events.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties, and events
        /// </summary>
        public void Clear()
        {
            foreach (var diff in _difficulties)
            {
                diff.Clear();
            }
            Events.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties.<br></br>
        /// </summary>
        public void TrimExcess()
        {
            foreach (var diff in _difficulties)
            {
                diff.TrimExcess();
            }
            // Trimming managed lists just generates a new array for GC to handle.
            // The exact opposite of what we want.
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
        /// Disposes all unmanaged buffer data from every difficulty
        /// </summary>
        public void Dispose()
        {
            foreach(var diff in _difficulties)
            {
                diff.Dispose();
            }
            Events.Dispose();
        }

        public IEnumerator<DifficultyTrack2<TNote>> GetEnumerator()
        {
            return ((IEnumerable<DifficultyTrack2<TNote>>) _difficulties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _difficulties.GetEnumerator();
        }
    }
}
