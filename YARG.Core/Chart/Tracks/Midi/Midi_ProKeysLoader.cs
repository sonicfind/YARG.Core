using YARG.Core.IO;
﻿using YARG.Core.Chart.ProKeys;

namespace YARG.Core.Chart
{
    public class Midi_ProKeys_Loader : MidiInstrumentLoader<ProKeysDifficulty>
    {
        private const int NOTE_MIN = 48;
        private const int NOTE_MAX = 72;

        private static readonly int[] SOLO = { 115 };
        private static readonly int[] BRE = { 120 };
        private static readonly int[] TREMOLO = { 126 };
        private static readonly int[] TRILL = { 127 };

        private readonly long[] lanes = new long[NOTE_MAX - NOTE_MIN + 1]
        {
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
        };

        private Midi_ProKeys_Loader() : base(new(new (int[], Midi_Phrase)[] {
            new(SOLO,            new(SpecialPhraseType.Solo)),
            new(IMidLoader.overdrivePhrase, new(SpecialPhraseType.StarPower)),
            new(BRE,             new(SpecialPhraseType.BRE)),
            new(TRILL,           new(SpecialPhraseType.Tremolo)),
            new(TREMOLO,         new(SpecialPhraseType.Trill))
        })) { }

        public static ProKeysDifficulty Load(YARGMidiTrack track)
        {
            Midi_ProKeys_Loader loader = new();
            return loader.Process(track);
        }

        protected override bool IsNote() { return NOTE_MIN <= note.value && note.value <= NOTE_MAX; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            if (!track.notes.ValidateLastKey(position))
                track.notes.Add_NoReturn(position);
            lanes[note.value - NOTE_MIN] = position;
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            long colorPosition = lanes[note.value - NOTE_MIN];
            if (colorPosition != -1)
            {
                track.notes.Traverse_Backwards_Until(colorPosition)!.Add(note.value, position - colorPosition);
                lanes[note.value - NOTE_MIN] = -1;
            }
        }

        protected override void ToggleExtraValues(YARGMidiTrack midiTrack)
        {
            switch (note.value)
            {
                case 0: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.C1_E2; break;
                case 2: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.D1_F2; break;
                case 4: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.E1_G2; break;
                case 5: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.F1_A2; break;
                case 7: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.G1_B2; break;
                case 9: track.ranges.Get_Or_Add_Last(position) = ProKey_Ranges.A1_C3; break;
            };
        }
    }
}
