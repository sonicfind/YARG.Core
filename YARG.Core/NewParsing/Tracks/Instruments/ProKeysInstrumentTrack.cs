using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Containers;

namespace YARG.Core.NewParsing
{
    public enum ProKey_Ranges
    {
        C1_E2,
        D1_F2,
        E1_G2,
        F1_A2,
        G1_B2,
        A1_C3,
    };

    public struct ProKeysInstrumentTrack : ITrack
    {
        public static readonly ProKeysInstrumentTrack Default = new()
        {
            Difficulties = DifficultyTrackCollection<ProKeyNote>.Default,
            Ranges = DifficultyExtensions<ProKey_Ranges>.Default,
            Glissandos = YARGNativeSortedList<DualTime, DualTime>.Default,
            Events = YARGManagedSortedList<DualTime, HashSet<string>>.Default,
        };

        public DifficultyTrackCollection<ProKeyNote>    Difficulties;
        public DifficultyExtensions<ProKey_Ranges>      Ranges;
        public YARGNativeSortedList<DualTime, DualTime> Glissandos;
        public YARGManagedSortedList<DualTime, HashSet<string>> Events;
        
        public readonly long NativeMemoryUsage =>
            Difficulties.NativeMemoryUsage
            + Ranges.NativeMemoryUsage
            + Glissandos.MemoryUsage;

        /// <summary>
        /// Returns whether all active difficulties and track-scope phrases (and events) are empty
        /// </summary>
        /// <returns>Whether the instrument contains no data</returns>
        public readonly bool IsEmpty()
        {
            return Difficulties.IsEmpty()
                && Ranges.IsEmpty()
                && Glissandos.IsEmpty()
                && Events.IsEmpty();
        }

        public void TrimExcess()
        {
            Difficulties.TrimExcess();
            Ranges.TrimExcess();
            Glissandos.TrimExcess();
        }

        public void Clear()
        {
            Difficulties.Clear();
            Ranges.Clear();
            Glissandos.Clear();
            Events.Clear();
        }

        /// <summary>
        /// Checks all difficulties to determine the end point of the track
        /// </summary>
        /// <returns>The end point of the track</returns>
        public readonly void UpdateLastNoteTime(ref DualTime lastNoteTime)
        {
            Difficulties.UpdateLastNoteTime(ref lastNoteTime);
        }

        public void Dispose(bool dispose)
        {
            Difficulties.Dispose(dispose);
            Ranges.Dispose();
            Glissandos.Dispose();
            if (dispose)
            {
                Events.Dispose();
            }
        }
    }
}
