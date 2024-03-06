using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiProKeysLoader : MidiInstrumentLoader<ProKeysDifficultyTrack>
    {
        public static ProKeysDifficultyTrack Load(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var loader = new MidiProKeysLoader();
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                loader.Position.Ticks = midiTrack.Position;
                loader.Position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    if (loader.Note.velocity > 0)
                    {
                        loader.ParseNote_ON();
                    }
                    else
                        loader.ParseNote_Off();
                }
                else if (midiTrack.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    loader.ParseNote_Off();
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    loader.Track.Events.GetLastOrAppend(loader.Position)
                                       .Add(Encoding.UTF8.GetString(midiTrack.ExtractTextOrSysEx()));
                }
            }

            loader.Track.TrimExcess();
            return loader.Track;
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

        private void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (NOTE_MIN <= Note.value && Note.value <= NOTE_MAX)
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

        private void ParseNote_Off()
        {
            if (NOTE_MIN <= Note.value && Note.value <= NOTE_MAX)
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

            Track.Notes.TryAppend(Position);
            lanes[Note.value - NOTE_MIN] = Position;
        }

        private void ParseLaneColor_Off()
        {
            ref var colorPosition = ref lanes[Note.value - NOTE_MIN];
            if (colorPosition.Ticks != -1)
            {
                Track.Notes.Traverse_Backwards_Until(colorPosition).Add(Note.value, DualTime.Truncate(Position - colorPosition));
                colorPosition.Ticks = -1;
            }
        }

        private void AddRangeShift()
        {
            switch (Note.value)
            {
                case 0: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.C1_E2; break;
                case 2: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.D1_F2; break;
                case 4: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.E1_G2; break;
                case 5: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.F1_A2; break;
                case 7: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.G1_B2; break;
                case 9: Track.Ranges.GetLastOrAppend(Position) = ProKey_Ranges.A1_C3; break;
            };
        }
    }
}
