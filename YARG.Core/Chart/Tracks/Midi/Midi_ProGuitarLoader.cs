using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Pitch;
using YARG.Core.Chart.ProGuitar;

namespace YARG.Core.Chart
{
    public class Midi_ProGuitar_Loader<TProFretConfig> : MidiInstrumentLoader<ProGuitarTrack<TProFretConfig>>
        where TProFretConfig : IProFretConfig, new()
    {
        private const int NOTE_MIN = 24;
        private const int NOTE_MAX = 106;
        private const int VALUES_PER_DIFFICULTY = 24;
        private const int NUM_STRINGS = 6;
        private const int HOPO_VALUE = 6;
        private const int SLIDE_VALUE = 7;
        private const int ARPEGGIO_VALUE = 8;
        private const int EMPHASIS_VALUE = 9;
        private const int FRET_MIN = 100;

        private static readonly int[] DIFFVALUES = new int[NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]{
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
        };

        private static readonly int[] LANEVALUES = new int[NUM_DIFFICULTIES * VALUES_PER_DIFFICULTY]{
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
        };

        private static readonly int[] SOLO = { 115 };
        private static readonly int[] TREMOLO = { 126 };
        private static readonly int[] TRILL = { 127 };

        static Midi_ProGuitar_Loader() { }

        private class ProGuitar_MidiDiff
        {
            public bool Hopo { get; set; }
            public readonly long[] notes = new long[NUM_STRINGS] { -1, -1, -1, -1, -1, -1 };
            public long Arpeggio { get; set; }
            public ProSlide Slide { get; set; }
            public EmphasisType Emphasis { get; set; }
            public ProGuitar_MidiDiff() { }
        }

        private readonly ProGuitar_MidiDiff[] difficulties = new ProGuitar_MidiDiff[NUM_DIFFICULTIES];

        private Midi_ProGuitar_Loader(HashSet<Difficulty>? difficulties) : base(new(new (int[], Midi_Phrase)[] {
            new(SOLO, new(SpecialPhraseType.Solo)),
            new(IMidLoader.overdrivePhrase, new(SpecialPhraseType.StarPower)),
            new(TRILL, new(SpecialPhraseType.Tremolo)),
            new(TREMOLO, new(SpecialPhraseType.Trill))
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

        public static ProGuitarTrack<TProFretConfig> Load(YARGMidiTrack reader, HashSet<Difficulty>? difficulties)
        {
            Midi_ProGuitar_Loader<TProFretConfig> loader = new(difficulties);
            return loader.Process(reader);
        }

        protected override bool IsNote() { return NOTE_MIN <= note.value && note.value <= NOTE_MAX; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - NOTE_MIN;
            int diffIndex = DIFFVALUES[noteValue];
            
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            var diffTrack = track[diffIndex];
            if (lane < NUM_STRINGS)
            {
                if (midiTrack.Channel == 1)
                    diffTrack.Arpeggios.Get_Or_Add_Last(position)[lane] = note.velocity - FRET_MIN;
                else
                {
                    ProGuitarNote<TProFretConfig> guitar;
                    if (!track[diffIndex].Notes.ValidateLastKey(position))
                    {
                        guitar = track[diffIndex].Notes.Add(position);
                        guitar.HOPO = midiDiff.Hopo;
                        guitar.Slide = midiDiff.Slide;
                        guitar.Emphasis = midiDiff.Emphasis;
                    }
                    else
                        guitar = track[diffIndex].Notes.Last();

                    ref var proString = ref guitar[lane];
                    switch (midiTrack.Channel)
                    {
                        case 2: proString.mode = StringMode.Bend; break;
                        case 3: proString.mode = StringMode.Muted; break;
                        case 4: proString.mode = StringMode.Tapped; break;
                        case 5: proString.mode = StringMode.Harmonics; break;
                        case 6: proString.mode = StringMode.Pinch_Harmonics; break;
                    }

                    proString.Fret = note.velocity - FRET_MIN;
                    midiDiff.notes[lane] = position;
                }
            }
            else if (lane == HOPO_VALUE)
            {
                midiDiff.Hopo = true;
                if (true && diffTrack.Notes.ValidateLastKey(position))
                    diffTrack.Notes.Last().HOPO = true;
            }
            else if (lane == SLIDE_VALUE)
            {
                midiDiff.Slide = midiTrack.Channel == 11 ? ProSlide.Reversed : ProSlide.Normal;
                if (diffTrack.Notes.ValidateLastKey(position))
                    diffTrack.Notes.Last().Slide = midiDiff.Slide;
            }
            else if (lane == ARPEGGIO_VALUE)
            {
                diffTrack.Arpeggios.Get_Or_Add_Last(position);
                midiDiff.Arpeggio = position;
            }
            else if (lane == EMPHASIS_VALUE)
            {
                switch (midiTrack.Channel)
                {
                    case 13: midiDiff.Emphasis = EmphasisType.High; break;
                    case 14: midiDiff.Emphasis = EmphasisType.Middle; break;
                    case 15: midiDiff.Emphasis = EmphasisType.Low; break;
                    default: return;
                }

                if (diffTrack.Notes.ValidateLastKey(position))
                    diffTrack.Notes.Last().Emphasis = midiDiff.Emphasis;
            }
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - NOTE_MIN;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            if (lane < NUM_STRINGS)
            {
                if (midiTrack.Channel != 1)
                {
                    long colorPosition = midiDiff.notes[lane];
                    if (colorPosition != -1)
                    {
                        track[diffIndex].Notes.Traverse_Backwards_Until(colorPosition)[lane].Duration = position - colorPosition;
                        midiDiff.notes[lane] = -1;
                    }
                }
            }
            else if (lane == HOPO_VALUE)
                midiDiff.Hopo = false;
            else if (lane == SLIDE_VALUE)
                midiDiff.Slide = ProSlide.None;
            else if (lane == ARPEGGIO_VALUE)
            {
                long arpeggioPosition = midiDiff.Arpeggio;
                if (arpeggioPosition != -1)
                {
                    track[diffIndex].Arpeggios.Last().Length = position - arpeggioPosition;
                    midiDiff.Arpeggio = -1;
                }
            }
            else if (lane == EMPHASIS_VALUE)
                midiDiff.Emphasis = EmphasisType.None;
        }

        private const int ROOT_MIN = 4;
        private const int ROOT_MAX = 15;

        internal static readonly PitchName[] s_ROOTS = { PitchName.E, PitchName.F, PitchName.F_Sharp_Gb, PitchName.G, PitchName.G_Sharp_Ab, PitchName.A, PitchName.A_Sharp_Bb, PitchName.B, PitchName.C, PitchName.C_Sharp_Db, PitchName.D, PitchName.D_Sharp_Eb };
        protected override void ToggleExtraValues(YARGMidiTrack midiTrack)
        {
            if (ROOT_MIN <= note.value && note.value <= ROOT_MAX)
            {
                track.Roots.Add(position, s_ROOTS[note.value - ROOT_MIN]);
                return;
            }

            switch (note.value)
            {
                case 16: track.ChordPhrases.Get_Or_Add_Last(position).Add(ChordPhrase.Slash); break;
                case 17: track.ChordPhrases.Get_Or_Add_Last(position).Add(ChordPhrase.Hide); break;
                case 18: track.ChordPhrases.Get_Or_Add_Last(position).Add(ChordPhrase.Accidental_Switch); break;
                case 107: track.ChordPhrases.Get_Or_Add_Last(position).Add(ChordPhrase.Force_Numbering); break;
                case 108: track.HandPositions.Add(position) = note.velocity - FRET_MIN; break;
            }
        }
    }
}
