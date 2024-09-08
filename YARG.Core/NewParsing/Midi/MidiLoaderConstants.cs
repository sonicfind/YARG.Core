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
    }

    internal struct ChordSnapper
    {
        private DualTime _lastOn;
        /// <summary>
        /// Attempts to chord snap the given position if it lies less than <see cref="NOTE_SNAP_THRESHOLD"/> number of ticks
        /// from the last NoteOn position
        /// </summary>
        /// <param name="position">The position to compare against and possible snap</param>
        /// <returns>Whether the position passed in got snapped</returns>
        public bool Snap(ref DualTime position)
        {
            if (_lastOn.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
            {
                position = _lastOn;
                return true;
            }
            else
            {
                _lastOn = position;
                return false;
            }
        }
    }
}
