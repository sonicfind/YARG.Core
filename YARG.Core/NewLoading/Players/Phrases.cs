using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public struct OverdrivePhrase
    {
        public readonly DualTime Position;
        public readonly int NumNotesInPhease;
        public int NumNotesHit;

        public OverdrivePhrase(in DualTime position, int numNotes)
        {
            Position = position;
            NumNotesInPhease = numNotes;
            NumNotesHit = 0;
        }
    }

    public struct SoloPhrase
    {
        public readonly DualTime Start;
        public readonly DualTime End;
        public readonly int NumNotesInPhease;
        public int NumNotesHit;

        public SoloPhrase(in DualTime start, in DualTime end, int numNotes)
        {
            Start = start;
            End = end;
            NumNotesInPhease = numNotes;
            NumNotesHit = 0;
        }
    }
}
