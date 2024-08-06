using System.Collections;
using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public static class InstrumentTrack2
    {
        public const int NUM_DIFFICULTIES = 4;
    }

    public class InstrumentTrack2<TDifficultyTrack> : Track, IEnumerable<TDifficultyTrack?>
        where TDifficultyTrack : Track
    {
        protected readonly TDifficultyTrack?[] difficulties = new TDifficultyTrack[InstrumentTrack2.NUM_DIFFICULTIES];
        public override bool IsEmpty()
        {
            foreach (var diff in difficulties)
            {
                if (diff != null && !diff.IsEmpty())
                {
                    return false;
                }
            }
            return base.IsEmpty();
        }

        public override void Clear()
        {
            base.Clear();
            foreach (var diff in difficulties)
            {
                diff?.Clear();
            }
        }

        public override void TrimExcess()
        {
            foreach (var diff in difficulties)
            {
                diff?.TrimExcess();
            }
        }

        public ref TDifficultyTrack? this[int index] { get { return ref difficulties[index]; } }
        public ref TDifficultyTrack? this[Difficulty diff] { get { return ref difficulties[(int) diff - 1]; } }

        public override DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            foreach (var diff in difficulties)
            {
                if (diff != null)
                {
                    var end = diff.GetLastNoteTime();
                    if (end > endTime)
                    {
                        endTime = end;
                    }
                }
            }
            return endTime;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var diff in difficulties)
                    {
                        diff?.Dispose();
                    }
                }
                base.Dispose(disposing);
            }
        }

        IEnumerator<TDifficultyTrack> IEnumerable<TDifficultyTrack?>.GetEnumerator()
        {
            return ((IEnumerable<TDifficultyTrack>) this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<TDifficultyTrack?>, IEnumerator
        {
            private readonly InstrumentTrack2<TDifficultyTrack> _track;
            private int diffIndex;

            internal Enumerator(InstrumentTrack2<TDifficultyTrack> track)
            {
                _track = track;
                diffIndex = -1;
            }

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++diffIndex;
                return diffIndex < InstrumentTrack2.NUM_DIFFICULTIES;
            }

            public readonly TDifficultyTrack? Current => _track.difficulties[diffIndex];

            readonly object? IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                diffIndex = -1;
            }
        }
    }
}
