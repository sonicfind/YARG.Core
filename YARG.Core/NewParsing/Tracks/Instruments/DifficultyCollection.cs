using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public struct DifficultyTrackCollection<TNote>
        where TNote : unmanaged, IInstrumentNote
    {
        public static readonly DifficultyTrackCollection<TNote> Default = new()
        {
            Easy = DifficultyTrack2<TNote>.Default,
            Medium = DifficultyTrack2<TNote>.Default,
            Hard = DifficultyTrack2<TNote>.Default,
            Expert = DifficultyTrack2<TNote>.Default,
        };

        public DifficultyTrack2<TNote> Easy;
        public DifficultyTrack2<TNote> Medium;
        public DifficultyTrack2<TNote> Hard;
        public DifficultyTrack2<TNote> Expert;

#pragma warning disable CS9084
        /// <summary>
        /// Returns a reference to the track that best matches the provided <see cref="Difficulty"></see>
        /// </summary>
        /// <remarks>
        /// <see cref="Difficulty.Beginner"></see> maps to <see cref="Difficulty.Easy"></see><br></br>
        /// <see cref="Difficulty.ExpertPlus"></see> maps to <see cref="Difficulty.Expert"></see>
        /// </remarks>
        /// <param name="diff">The difficulty to grab</param>
        /// <returns>A direct ref to the appropriate track</returns>
        /// <exception cref="ArgumentOutOfRangeException">Some unhandled difficulty was provided</exception>
        public unsafe ref DifficultyTrack2<TNote> this[Difficulty diff]
        {
            get
            {
                switch (diff)
                {
                    case Difficulty.Beginner:
                    case Difficulty.Easy:
                        return ref Easy;
                    case Difficulty.Medium:
                        return ref Medium;
                    case Difficulty.Hard:
                        return ref Hard;
                    case Difficulty.Expert:
                    case Difficulty.ExpertPlus:
                        return ref Expert;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Returns a reference to the track that aligns with the given index (ascending from Easy to Expert)
        /// </summary>
        /// <param name="index">The ascending index</param>
        /// <returns>A direct ref to the appropriate track</returns>
        /// <exception cref="IndexOutOfRangeException">Some invalid index was provided</exception>
        public unsafe ref DifficultyTrack2<TNote> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return ref Easy;
                    case 1:
                        return ref Medium;
                    case 2:
                        return ref Hard;
                    case 3:
                        return ref Expert;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public readonly long NativeMemoryUsage =>
            Easy.NativeMemoryUsage
            + Medium.NativeMemoryUsage
            + Hard.NativeMemoryUsage
            + Expert.NativeMemoryUsage;

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases and events are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public readonly bool IsEmpty()
        {
            return Easy.IsEmpty()
                && Medium.IsEmpty()
                && Hard.IsEmpty()
                && Expert.IsEmpty();
        }

        /// <summary>
        /// Clears all difficulties
        /// </summary>
        public void Clear()
        {
            Easy.Clear();
            Medium.Clear();
            Hard.Clear();
            Expert.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties.<br></br>
        /// </summary>
        public void TrimExcess()
        {
            Easy.TrimExcess();
            Medium.TrimExcess();
            Hard.TrimExcess();
            Expert.TrimExcess();
        }

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            Easy.UpdateLastNoteTime(ref lastNoteTime);
            Medium.UpdateLastNoteTime(ref lastNoteTime);
            Hard.UpdateLastNoteTime(ref lastNoteTime);
            Expert.UpdateLastNoteTime(ref lastNoteTime);
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every difficulty
        /// </summary>
        public void Dispose(bool dispose)
        {
            Easy.Dispose(dispose);
            Medium.Dispose(dispose);
            Hard.Dispose(dispose);
            Expert.Dispose(dispose);
        }
    }
}
