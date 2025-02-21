﻿using System;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public class FiveFretPlayer : YargPlayer
    {
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