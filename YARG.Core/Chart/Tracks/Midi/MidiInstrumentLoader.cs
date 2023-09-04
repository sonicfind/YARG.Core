using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public abstract class MidiInstrumentLoader<TTrack> : MidiLoader<TTrack>
        where TTrack : Track, new()
    {
        protected const int DEFAULT_MIN = 60;
        protected const int DEFAULT_MAX = 100;
        protected const int NUM_DIFFICULTIES = 4;

        private long lastOn = 0;
        private readonly long[] notes_BRE = { -1, -1, -1, -1, -1 };
        private bool doBRE = false;

        protected MidiInstrumentLoader(Midi_PhraseList phrases) : base(new TTrack(), phrases) { }

        protected override void ParseNote_ON(YARGMidiTrack midiTrack)
        {
            NormalizeNoteOnPosition();
            if (ProcessSpecialNote(midiTrack))
                return;

            if (IsNote())
                ParseLaneColor(midiTrack);
            else if (!AddPhrase(ref track.specialPhrases, note))
            {
                if (120 <= note.value && note.value <= 124)
                    ParseBRE(note.value);
                else
                    ToggleExtraValues(midiTrack);
            }
        }

        protected override void ParseNote_Off(YARGMidiTrack midiTrack)
        {
            if (ProcessSpecialNote_Off(midiTrack))
                return;

            if (IsNote())
                ParseLaneColor_Off(midiTrack);
            else if (!AddPhrase_Off(ref track.specialPhrases, note))
            {
                if (120 <= note.value && note.value <= 124)
                    ParseBRE_Off();
                else
                    ToggleExtraValues_Off(midiTrack);
            }
        }

        protected abstract void ParseLaneColor(YARGMidiTrack midiTrack);

        protected abstract void ParseLaneColor_Off(YARGMidiTrack midiTrack);

        protected void NormalizeNoteOnPosition()
        {
            if (position < lastOn + 16)
                position = lastOn;
            else
                lastOn = position;
        }

        protected virtual bool IsNote() { return 60 <= note.value && note.value <= 100; }

        protected virtual bool ProcessSpecialNote(YARGMidiTrack midiTrack) { return false; }

        protected virtual bool ProcessSpecialNote_Off(YARGMidiTrack midiTrack) { return false; }

        protected virtual void ToggleExtraValues(YARGMidiTrack midiTrack) { }

        protected virtual void ToggleExtraValues_Off(YARGMidiTrack midiTrack) { }

        private bool AddPhrase(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, MidiNote note)
        {
            return this.phrases.AddPhrase(ref phrases, position, note);
        }

        private bool AddPhrase_Off(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, MidiNote note)
        {
            return this.phrases.AddPhrase_Off(ref phrases, position, note);
        }

        private void ParseBRE(int midiValue)
        {
            notes_BRE[midiValue - 120] = position;
            doBRE = notes_BRE[0] == notes_BRE[1] && notes_BRE[1] == notes_BRE[2] && notes_BRE[2] == notes_BRE[3];
        }

        private void ParseBRE_Off()
        {
            if (doBRE)
            {
                ref var phrasesList = ref track.specialPhrases[notes_BRE[0]];
                phrasesList.Add(new(SpecialPhraseType.BRE, position - notes_BRE[0]));

                for (int i = 0; i < 5; i++)
                    notes_BRE[0] = -1;
                doBRE = false;
            }
        }
    }

    public abstract class MidiInstrumentLoader_Common<TNote, TDiffTracker> : MidiInstrumentLoader<InstrumentTrack_FW<TNote>>
        where TNote : INote, new()
        where TDiffTracker : new()
    {
        protected const int NOTES_PER_DIFFICULTY = 12;
        protected static readonly int[] SOLO = { 103 };
        protected static readonly int[] TREMOLO = { 126 };
        protected static readonly int[] TRILL = { 127 };
        protected static readonly int[] DIFFVALUES = new int[NUM_DIFFICULTIES * NOTES_PER_DIFFICULTY] {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
        };

        protected readonly TDiffTracker[] difficulties = new TDiffTracker[NUM_DIFFICULTIES];

        protected MidiInstrumentLoader_Common(HashSet<Difficulty>? difficulties) : base(new(new (int[], Midi_Phrase)[] {
            (SOLO,            new(SpecialPhraseType.Solo)),
            (IMidLoader.overdrivePhrase, new(SpecialPhraseType.StarPower)),
            (TREMOLO,         new(SpecialPhraseType.Tremolo)),
            (TRILL,           new(SpecialPhraseType.Trill))
        }))
        {
            for (int i = 0; i < NUM_DIFFICULTIES; i++)
            {
                if (difficulties == null || difficulties.Contains((Difficulty) i))
                {
                    track[i] = new();
                    this.difficulties[i] = new();
                }
            }
        }
    }
}
