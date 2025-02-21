using System;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class FiveFretPlayer : YargPlayer
    {
        /// <summary>
        /// Total number of possible lanes (including open note)
        /// </summary>
        public const int NUM_LANES = 6;
        public const int OPEN_NOTE = 0;
        public const int GREEN_NOTE = 1;
        public const int RED_NOTE = 2;
        public const int YELLOW_NOTE = 3;
        public const int BLUE_NOTE = 4;
        public const int ORANGE_NOTE = 5;

        public struct GuitarNoteGroup
        {
            public GuitarState State;
            public long        NoteIndex;
            public long        NoteCount;
        }

        public static FiveFretPlayer Create(YARGChart chart, InstrumentTrack2<GuitarNote<FiveFret>> instrument, in DualTime endTime, in InstrumentSelection selection)
        {
            return new FiveFretPlayer();
        }
    }
}