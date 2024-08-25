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

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
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

        /// <summary>
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public override void Clear()
        {
            foreach (var diff in Difficulties)
            {
                diff?.Clear();
            }
            base.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties and the track's phrases.<br></br>
        /// This will also delete any completely empty difficulties.
        /// </summary>
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

        /// <summary>
        /// Returns a reference to the track that best matches the provided <see cref="Difficulty"></see>
        /// </summary>
        /// <remarks>
        /// <see cref="Difficulty.Beginner"></see> maps to <see cref="Difficulty.Easy"></see><br></br>
        /// <see cref="Difficulty.ExpertPlus"></see> maps to <see cref="Difficulty.Expert"></see>
        /// </remarks>
        /// <param name="diff">The difficulty to grab</param>
        /// <returns>A direct ref to the appropriate track</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Some unhandled difficulty was provided</exception>
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

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
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

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase constainers
        /// </summary>
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
