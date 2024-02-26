using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    internal static class MidiLoader_Constants
    {
        public const int NOTE_SNAP_THRESHOLD = 16;
        public const int NOTES_PER_DIFFICULTY = 12;
        public const int DEFAULT_MIN = 60;
        public const int DEFAULT_MAX = 100;

        public const int BRE_MIN = 120;
        public const int BRE_MAX = 124;

        public const int SOLO = 103;
        public const int OVERDRIVE = 116;
        public const int TREMOLO = 126;
        public const int TRILL = 127;

        public static readonly int[] DIFFVALUES = new int[InstrumentTrack2.NUM_DIFFICULTIES * NOTES_PER_DIFFICULTY]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
        };

        internal static ref TValue TraverseBackwardsUntil<TKey, TValue>(this YARGManagedSortedList<TKey, TValue> list, TKey key)
            where TKey : IEquatable<TKey>, IComparable<TKey>
            where TValue : new()
        {
            int index = list.Count - 1;
            while (index > 0 && key.CompareTo(list.ElementAtIndex(index).Key) < 0)
            {
                --index;
            }
            return ref list.ElementAtIndex(index).Value;
        }

        internal static ref TValue TraverseBackwardsUntil<TKey, TValue>(this YARGNativeSortedList<TKey, TValue> list, TKey key)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            unsafe
            {
                var current = list.End - 1;
                while (current > list.Data && key.CompareTo(current->Key) < 0)
                    --current;
                return ref current->Value;
            }
        }
    }
}
