using System.Collections.Generic;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public class VocalsTrack2 : ITrack
    {
        private readonly VocalPart2[] _parts;
        public YARGNativeSortedList<DualTime, bool> Percussion { get; } = new();
        public YARGNativeSortedList<DualTime, DualTime> VocalPhrases_1 { get; } = new();
        public YARGNativeSortedList<DualTime, DualTime> VocalPhrases_2 { get; } = new();
        public YARGNativeSortedList<DualTime, DualTime> HarmonyLines { get; } = new();
        public YARGNativeSortedList<DualTime, DualTime> RangeShifts { get; } = new();
        public YARGNativeSortedList<DualTime, DualTime> Overdrives { get; } = new();
        public YARGNativeList<DualTime> LyricShifts { get; } = new();
        public YARGManagedSortedList<DualTime, HashSet<string>> Events { get; } = new();

        /// <summary>
        /// Returns the vocal part (lyrics and notes) for the specified index
        /// </summary>
        /// <param name="index">The part index</param>
        /// <returns>The notes and lyrics for a specific vocal part</returns>
        public VocalPart2 this[int index] => _parts[index];
        public int TrackCount => _parts.Length;

        public VocalsTrack2(int count)
        {
            _parts = new VocalPart2[count];
            for (int i = 0; i < count; i++)
            {
                _parts[i] = new VocalPart2();
            }
        }

        /// <summary>
        /// Returns whether all parts, percussion, phrases, and events are empty
        /// </summary>
        /// <returns>If all containers are empty, return true</returns>
        public bool IsEmpty()
        {
            foreach (var part in _parts)
            {
                if (!part.IsEmpty())
                {
                    return false;
                }
            }
            return Percussion.IsEmpty()
                && VocalPhrases_1.IsEmpty()
                && VocalPhrases_2.IsEmpty()
                && HarmonyLines.IsEmpty()
                && RangeShifts.IsEmpty()
                && Overdrives.IsEmpty()
                && LyricShifts.IsEmpty()
                && Events.IsEmpty();
        }

        /// <summary>
        /// Clears all parts, percussion, phrase, and events
        /// </summary>
        public void Clear()
        {
            foreach (var part in _parts)
            {
                part.Clear();
            }
            Percussion.Clear();
            VocalPhrases_1.Clear();
            VocalPhrases_2.Clear();
            HarmonyLines.Clear();
            RangeShifts.Clear();
            Overdrives.Clear();
            LyricShifts.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Trims the buffers for parts, percussion, and phrases
        /// </summary>
        public void TrimExcess()
        {
            foreach(var part in _parts)
            {
                part.TrimExcess();
            }
            if ((Percussion.Count < 20 || 400 <= Percussion.Count) && Percussion.Count < Percussion.Capacity)
            {
                Percussion.TrimExcess();
            }
            VocalPhrases_1.TrimExcess();
            VocalPhrases_2.TrimExcess();
            HarmonyLines.TrimExcess();
            RangeShifts.TrimExcess();
            Overdrives.TrimExcess();
            LyricShifts.TrimExcess();
        }

        /// <summary>
        /// Returns the latest time when any part (or percussion) ends
        /// </summary>
        /// <returns>The latest end time of the track</returns>
        public void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            foreach (var part in _parts)
            {
                part.UpdateLastNoteTime(ref lastNoteTime);
            }

            if (!Percussion.IsEmpty())
            {
                unsafe
                {
                    ref readonly var perc = ref Percussion.Data[Percussion.Count - 1];
                    if (perc.Key > lastNoteTime)
                    {
                        lastNoteTime = perc.Key;
                    }
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged buffer data for each part, percussion, and phrases
        /// </summary>
        public void Dispose()
        {
            foreach (var part in _parts)
            {
                part.Dispose();
            }
            Percussion.Dispose();
            VocalPhrases_1.Dispose();
            VocalPhrases_2.Dispose();
            HarmonyLines.Dispose();
            RangeShifts.Dispose();
            Overdrives.Dispose();
            LyricShifts.Dispose();
            Events.Dispose();
        }
    }
}
