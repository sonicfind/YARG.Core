using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Chart.Guitar;

namespace YARG.Core.Chart
{
    public class FiveFretMidiDifficulty
    {
        private static readonly int[] PHRASE = { 0 };

        public bool SliderNotes { get; set; }
        public bool HopoOn { get; set; }
        public bool HopoOff { get; set; }
        public readonly long[] notes = new long[6] { -1, -1, -1, -1, -1, -1 };
        public readonly Midi_PhraseList phrases;

        public FiveFretMidiDifficulty()
        {
            phrases = new(new (int[], Midi_Phrase)[] {
                (PHRASE, new(SpecialPhraseType.StarPower_Diff)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player1)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player2)),
            });
        }

        static FiveFretMidiDifficulty() { }
    }

    public class Midi_FiveFretLoader : MidiInstrumentLoader_Common<GuitarNote<FiveFret>, FiveFretMidiDifficulty>
    {
        private static readonly byte[][] ENHANCED_STRINGS = new byte[][] { Encoding.ASCII.GetBytes("[ENHANCED_OPENS]"), Encoding.ASCII.GetBytes("ENHANCED_OPENS") };
        private readonly int[] lanes = new int[] {
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        static Midi_FiveFretLoader() { }

        private Midi_FiveFretLoader(HashSet<Difficulty>? difficulties) : base(difficulties) { }

        public static InstrumentTrack_FW<GuitarNote<FiveFret>> Load(YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties)
        {
            Midi_FiveFretLoader loader = new(difficulties);
            return loader.Process(midiTrack);
        }

        protected override bool IsNote() { return 59 <= note.value && note.value <= 107; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 59;
            int diffIndex = DIFFVALUES[noteValue];
            int lane = lanes[noteValue];

            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null && (diffIndex != 3 || lane != 8))
                return;

            if (lane < 6)
            {
                midiDiff!.notes[lane] = position;
                if (!track[diffIndex].Notes.ValidateLastKey(position))
                {
                    if (track[diffIndex].Notes.Capacity == 0)
                        track[diffIndex].Notes.Capacity = 5000;

                    ref var guitar = ref track[diffIndex].Notes.Add(position);
                    if (midiDiff.SliderNotes)
                        guitar.IsTap = true;

                    if (midiDiff.HopoOn)
                        guitar.Forcing = ForceStatus.HOPO;
                    else if (midiDiff.HopoOff)
                        guitar.Forcing = ForceStatus.STRUM;
                }
            }
            else if (lane == 6)
            {
                midiDiff!.HopoOn = true;
                if (track[diffIndex].Notes.ValidateLastKey(position))
                    track[diffIndex].Notes.Last().Forcing = ForceStatus.HOPO;
            }
            // HopoOff marker
            else if (lane == 7)
            {
                midiDiff!.HopoOff = true;
                if (track[diffIndex].Notes.ValidateLastKey(position))
                    track[diffIndex].Notes.Last().Forcing = ForceStatus.STRUM;
            }
            else if (lane == 8)
            {
                if (diffIndex == 3)
                {
                    phrases.AddPhrase(ref track.specialPhrases, position, SpecialPhraseType.Solo, 100);
                    return;
                }

                for (int i = 0; i < 4; ++i)
                    lanes[12 * i + 8] = 12;

                for (int i = 0; i < track.specialPhrases.Count;)
                {
                    var vec = track.specialPhrases.At_index(i);
                    var phrases = vec.obj;
                    for (int p = 0; p < phrases.Count;)
                    {
                        if (phrases[p].Type == SpecialPhraseType.Solo)
                        {
                            track[3].specialPhrases[vec.position].Add(new(SpecialPhraseType.StarPower_Diff, phrases[p].Duration));
                            vec.obj.RemoveAt(p);
                        }
                        else
                            ++p;
                    }

                    if (vec.obj.Count == 0)
                        track.specialPhrases.RemoveAt(i);
                    else
                        ++i;
                }

                midiDiff!.phrases.AddPhrase(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.StarPower_Diff, 100);
            }
            else if (lane == 9)
                midiDiff!.SliderNotes = true;
            else if (lane == 10)
                midiDiff!.phrases.AddPhrase(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.FaceOff_Player1, 100);
            else if (lane == 11)
                midiDiff!.phrases.AddPhrase(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.FaceOff_Player2, 100);
            else if (lane == 12)
                midiDiff!.phrases.AddPhrase(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.StarPower_Diff, 100);
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 59;
            int lane = lanes[noteValue];
            int diffIndex = DIFFVALUES[noteValue];

            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null && (diffIndex != 3 || lane != 8))
                return;

            if (lane < 6)
            {
                long colorPosition = difficulties[diffIndex].notes[lane];
                if (colorPosition != -1)
                {
                    track[diffIndex].Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    midiDiff!.notes[lane] = -1;
                }
            }
            else if (lane == 6)
            {
                midiDiff!.HopoOn = false;
                if (track[diffIndex].Notes.ValidateLastKey(position))
                    track[diffIndex].Notes.Last().Forcing = ForceStatus.NATURAL;
            }
            else if (lane == 7)
            {
                midiDiff!.HopoOff = false;
                if (track[diffIndex].Notes.ValidateLastKey(position))
                    track[diffIndex].Notes.Last().Forcing = ForceStatus.NATURAL;
            }
            else if (lane == 8)
                phrases.AddPhrase_Off(ref track.specialPhrases, position, SpecialPhraseType.Solo);
            else if (lane == 9)
                midiDiff!.SliderNotes = false;
            else if (lane == 10)
                midiDiff!.phrases.AddPhrase_Off(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.FaceOff_Player1);
            else if (lane == 11)
                midiDiff!.phrases.AddPhrase_Off(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.FaceOff_Player2);
            else if (lane == 12)
                midiDiff!.phrases.AddPhrase_Off(ref track[diffIndex].specialPhrases, position, SpecialPhraseType.StarPower_Diff);
        }

        protected override void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (str.StartsWith(SYSEXTAG))
            {
                if (str[6] == 1)
                    NormalizeNoteOnPosition();

                if (str[4] == (char) 0xFF)
                {
                    switch (str[5])
                    {
                        case 1:
                            {
                                int status = str[6] == 0 ? 1 : 0;
                                for (int diff = 0; diff < 4; ++diff)
                                    lanes[12 * diff + 1] = status;
                                break;
                            }
                        case 4:
                            {
                                for (int diff = 0; diff < 4; ++diff)
                                {
                                    if (difficulties[diff] == null)
                                        continue;

                                    difficulties[diff].SliderNotes = str[6] == 1;
                                    if (track[diff].Notes.ValidateLastKey(position))
                                        track[diff].Notes.Last().IsTap = str[6] == 1;
                                }
                                break;
                            }
                    }
                }
                else
                {
                    byte diff = str[4];
                    if (difficulties[diff] == null)
                        return;

                    switch (str[5])
                    {
                        case 1:
                            lanes[12 * diff + 1] = str[6] == 0 ? 1 : 0;
                            break;
                        case 4:
                            {
                                bool enable = str[6] == 1;
                                difficulties[diff].SliderNotes = enable;
                                if (track[diff].Notes.ValidateLastKey(position))
                                    track[diff].Notes.Last().IsTap = enable;
                            }
                            break;
                    }
                }
            }
        }

        protected override void ParseText(ReadOnlySpan<byte> str)
        {
            if (lanes[0] == 13 && (str.SequenceEqual(ENHANCED_STRINGS[0]) || str.SequenceEqual(ENHANCED_STRINGS[1])))
            {
                for (int diff = 0; diff < 4; ++diff)
                    lanes[12 * diff] = 0;
            }
            else
                track.events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }
    }
}
