using YARG.Core.NewParsing;

namespace YARG.Core.NewLoading
{
    public readonly struct OverdrivePhrase
    {
        public readonly DualTime Position;
        public readonly int NumNotesInPhease;

        public OverdrivePhrase(in DualTime position, int numNotes)
        {
            Position = position;
            NumNotesInPhease = numNotes;
        }
    }

    public readonly struct SoloPhrase
    {
        public readonly DualTime Start;
        public readonly DualTime End;
        public readonly int NumNotesInPhease;

        public SoloPhrase(in DualTime start, in DualTime end, int numNotes)
        {
            Start = start;
            End = end;
            NumNotesInPhease = numNotes;
        }
    }
}
