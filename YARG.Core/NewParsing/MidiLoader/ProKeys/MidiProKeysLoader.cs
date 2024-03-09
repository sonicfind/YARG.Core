using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiProKeysLoader : MidiInstrumentLoader<ProKeysDifficultyTrack>
    {
        public static ProKeysDifficultyTrack Load(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var loader = new MidiProKeysLoader();
            return loader.Process(midiTrack, sync);
        }

        private const int NOTE_MIN = 48;
        private const int NOTE_MAX = 72;

        private static readonly Midi_PhraseMapping SOLO = new(new[] { 115 }, SpecialPhraseType.Solo);

        private readonly DualTime[] lanes = new DualTime[NOTE_MAX - NOTE_MIN + 1]
        {
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
        };

        private readonly Midi_PhraseMapping[] PhraseMappings = new Midi_PhraseMapping[]
        {
            OverdrivePhrase, SOLO, MidiBasicInstrumentLoader.TREMOLO, MidiBasicInstrumentLoader.TRILL,
        };

        private MidiProKeysLoader() : base(1) { }

        protected override void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (NOTE_MIN <= _note.value && _note.value <= NOTE_MAX)
            {
                ParseLaneColor_ON();
            }
            else if (!AddPhrase_ON(PhraseMappings))
            {
                if (!ParseBRE_ON())
                {
                    AddRangeShift();
                }
            }
        }

        protected override void ParseNote_Off()
        {
            if (NOTE_MIN <= _note.value && _note.value <= NOTE_MAX)
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off(PhraseMappings))
            {
                ParseBRE_Off();
            }
        }

        private void ParseLaneColor_ON()
        {
            if (Track.Notes.Capacity == 0)
            {
                Track.Notes.Capacity = 5000;
            }

            Track.Notes.TryAppend(_position);
            lanes[_note.value - NOTE_MIN] = _position;
        }

        private void ParseLaneColor_Off()
        {
            ref var colorPosition = ref lanes[_note.value - NOTE_MIN];
            if (colorPosition.Ticks != -1)
            {
                Track.Notes.TraverseBackwardsUntil(colorPosition).Add(_note.value, DualTime.Truncate(_position - colorPosition));
                colorPosition.Ticks = -1;
            }
        }

        private void AddRangeShift()
        {
            switch (_note.value)
            {
                case 0: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.C1_E2; break;
                case 2: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.D1_F2; break;
                case 4: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.E1_G2; break;
                case 5: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.F1_A2; break;
                case 7: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.G1_B2; break;
                case 9: Track.Ranges.GetLastOrAppend(_position) = ProKey_Ranges.A1_C3; break;
            };
        }
    }
}
