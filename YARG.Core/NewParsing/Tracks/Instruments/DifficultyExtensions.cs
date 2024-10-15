using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public struct DifficultyExtensions<T> : ITrack
        where T : unmanaged
    {
        public static readonly DifficultyExtensions<T> Default = new()
        {
            Easy   = YARGNativeSortedList<DualTime, T>.Default,
            Medium = YARGNativeSortedList<DualTime, T>.Default,
            Hard   = YARGNativeSortedList<DualTime, T>.Default,
            Expert = YARGNativeSortedList<DualTime, T>.Default,
        };

        public YARGNativeSortedList<DualTime, T> Easy;
        public YARGNativeSortedList<DualTime, T> Medium;
        public YARGNativeSortedList<DualTime, T> Hard;
        public YARGNativeSortedList<DualTime, T> Expert;

#pragma warning disable CS9084
        public unsafe ref YARGNativeSortedList<DualTime, T> this[Difficulty diff]
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

        public unsafe ref YARGNativeSortedList<DualTime, T> this[int index]
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
        /// Clears all difficulties, phrases, and events
        /// </summary>
        public void Clear()
        {
            Easy.Clear();
            Medium.Clear();
            Hard.Clear();
            Expert.Clear();
        }

        /// <summary>
        /// Trims excess unmanaged buffer data from all difficulties and the track's phrases.<br></br>
        /// This will also delete any completely empty difficulties.
        /// </summary>
        public void TrimExcess()
        {
            Easy.TrimExcess();
            Medium.TrimExcess();
            Hard.TrimExcess();
            Expert.TrimExcess();
        }

        /// <summary>
        /// Disposes all unmanaged buffer data from every active difficulty and all phrase containers
        /// </summary>
        public void Dispose()
        {
            Easy.Dispose();
            Medium.Dispose();
            Hard.Dispose();
            Expert.Dispose();
        }
    }
}
