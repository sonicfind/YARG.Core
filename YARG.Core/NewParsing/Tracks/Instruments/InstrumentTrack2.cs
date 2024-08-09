using System.Collections;
using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public static class InstrumentTrack2
    {
        public const int NUM_DIFFICULTIES = 4;
    }

    public class InstrumentTrack2<TDifficultyTrack> : PhraseTrack, IEnumerable<TDifficultyTrack?>
        where TDifficultyTrack : class, ITrack, new()
    {
        public readonly TDifficultyTrack?[] Difficulties = new TDifficultyTrack[InstrumentTrack2.NUM_DIFFICULTIES];

        public InstrumentTrack2() {}

        /// <summary>
        /// Move constructor that siphons all phrases and special events from the source,
        /// leaving it in a default state.
        /// </summary>
        /// <remarks>Does not effect <see cref="Difficulties"/>. Those remains unchanged and <see langword="null"/></remarks>
        /// <param name="source"></param>
        public InstrumentTrack2(PhraseTrack source)
            : base(source) {}

        public override bool IsEmpty()
        {
            foreach (var diff in Difficulties)
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
            foreach (var diff in Difficulties)
            {
                diff?.Clear();
            }
            base.Clear();
        }

        public override void TrimExcess()
        {
            for (var i = 0; i < Difficulties.Length; i++)
            {
                var diff = Difficulties[i];
                if (diff != null)
                {
                    if (diff.IsEmpty())
                    {
                        diff.Dispose();
                        Difficulties[i] = null;
                    }
                    else
                    {
                        diff.TrimExcess();
                    }
                }
            }
            base.TrimExcess();
        }

        public ref TDifficultyTrack? this[Difficulty diff] { get { return ref Difficulties[(int) diff - 1]; } }

        public override DualTime GetLastNoteTime()
        {
            DualTime endTime = default;
            foreach (var diff in Difficulties)
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

        public override void Dispose()
        {
            foreach (var diff in Difficulties)
            {
                diff?.Dispose();
            }
            base.Dispose();
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

            public readonly TDifficultyTrack? Current => _track.Difficulties[diffIndex];

            readonly object? IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                diffIndex = -1;
            }
        }
    }
}
