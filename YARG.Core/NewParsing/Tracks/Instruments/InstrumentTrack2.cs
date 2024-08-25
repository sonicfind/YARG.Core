using System.Collections;
using System.Collections.Generic;

namespace YARG.Core.NewParsing
{
    public static class InstrumentTrack2
    {
        public const int NUM_DIFFICULTIES = 4;
    }

    public class InstrumentTrack2<TDifficultyTrack> : PhraseTrack
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

        public ref TDifficultyTrack? this[Difficulty diff]
        {
            get
            {
                switch (diff)
                {
                    case Difficulty.Beginner:
                    case Difficulty.Easy:
                        return ref Difficulties[0];
                    case Difficulty.Medium:
                        return ref Difficulties[1];
                    case Difficulty.Hard:
                        return ref Difficulties[2];
                    case Difficulty.Expert:
                    case Difficulty.ExpertPlus:
                        return ref Difficulties[3];
                    default:
                        throw new System.ArgumentOutOfRangeException();
                }
            }
        }

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
    }
}
