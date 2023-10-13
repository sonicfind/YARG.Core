using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Chart.Guitar;

namespace YARG.Core.Chart
{
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

            ref var diff = ref track[diffIndex]!;
            if (lane < 6)
            {
                midiDiff!.notes[lane] = position;
                if (!diff.Notes.ValidateLastKey(position))
                {
                    if (diff.Notes.Capacity == 0)
                        diff.Notes.Capacity = 5000;

                    ref var guitar = ref diff.Notes.Add(position);
                    if (midiDiff.SliderNotes)
                        guitar.State = GuitarState.TAP;
                    else if (midiDiff.HopoOn)
                        guitar.State = GuitarState.HOPO;
                    else if (midiDiff.HopoOff)
                        guitar.State = GuitarState.STRUM;
                }
            }
            else if (lane == 6)
            {
                midiDiff!.HopoOn = true;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State == GuitarState.NATURAL)
                        guitar.State = GuitarState.HOPO;
                }
            }
            // HopoOff marker
            else if (lane == 7)
            {
                midiDiff!.HopoOff = true;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State == GuitarState.NATURAL)
                        guitar.State = GuitarState.STRUM;
                }
            }
            else if (lane == 8)
            {
                if (diffIndex == 3)
                {
                    phrases.AddPhrase(ref track.SpecialPhrases, position, SpecialPhraseType.Solo, 100);
                    return;
                }

                for (int i = 0; i < 4; ++i)
                    lanes[12 * i + 8] = 12;

                for (int i = 0; i < track.SpecialPhrases.Count;)
                {
                    var vec = track.SpecialPhrases.At_index(i);
                    var phrases = vec.obj;
                    for (int p = 0; p < phrases.Count;)
                    {
                        if (phrases[p].Type == SpecialPhraseType.Solo)
                        {
                            track[3]!.SpecialPhrases[vec.position].Add(new(SpecialPhraseType.StarPower_Diff, phrases[p].Duration));
                            vec.obj.RemoveAt(p);
                        }
                        else
                            ++p;
                    }

                    if (vec.obj.Count == 0)
                        track.SpecialPhrases.RemoveAt(i);
                    else
                        ++i;
                }

                midiDiff!.phrases.AddPhrase(ref diff.SpecialPhrases, position, SpecialPhraseType.StarPower_Diff, 100);
            }
            else if (lane == 9)
                midiDiff!.SliderNotes = true;
            else if (lane == 10)
                midiDiff!.phrases.AddPhrase(ref diff.SpecialPhrases, position, SpecialPhraseType.FaceOff_Player1, 100);
            else if (lane == 11)
                midiDiff!.phrases.AddPhrase(ref diff.SpecialPhrases, position, SpecialPhraseType.FaceOff_Player2, 100);
            else if (lane == 12)
                midiDiff!.phrases.AddPhrase(ref diff.SpecialPhrases, position, SpecialPhraseType.StarPower_Diff, 100);
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 59;
            int lane = lanes[noteValue];
            int diffIndex = DIFFVALUES[noteValue];

            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null && (diffIndex != 3 || lane != 8))
                return;

            ref var diff = ref track[diffIndex]!;
            if (lane < 6)
            {
                long colorPosition = difficulties[diffIndex].notes[lane];
                if (colorPosition != -1)
                {
                    diff.Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    midiDiff!.notes[lane] = -1;
                }
            }
            else if (lane == 6)
            {
                midiDiff!.HopoOn = false;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State != GuitarState.TAP)
                        guitar.State = GuitarState.NATURAL;
                }
            }
            else if (lane == 7)
            {
                midiDiff!.HopoOff = false;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State != GuitarState.TAP)
                        guitar.State = GuitarState.NATURAL;
                }
            }
            else if (lane == 8)
                phrases.AddPhrase_Off(ref track.SpecialPhrases, position, SpecialPhraseType.Solo);
            else if (lane == 9)
                midiDiff!.SliderNotes = false;
            else if (lane == 10)
                midiDiff!.phrases.AddPhrase_Off(ref diff.SpecialPhrases, position, SpecialPhraseType.FaceOff_Player1);
            else if (lane == 11)
                midiDiff!.phrases.AddPhrase_Off(ref diff.SpecialPhrases, position, SpecialPhraseType.FaceOff_Player2);
            else if (lane == 12)
                midiDiff!.phrases.AddPhrase_Off(ref diff.SpecialPhrases, position, SpecialPhraseType.StarPower_Diff);
        }

        protected override void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (str.StartsWith(SYSEXTAG))
            {
                bool enable = str[6] == 1;
                if (enable)
                    NormalizeNoteOnPosition();

                if (str[4] == (char) 0xFF)
                {
                    switch (str[5])
                    {
                        case 1:
                            {
                                int status = str[6] == 0 ? 1 : 0;
                                for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
                                    lanes[12 * diffIndex + 1] = status;
                                break;
                            }
                        case 4:
                            {
                                MidiGuitarHelper.ProcessTapSysex(track, difficulties, position, enable);
                                break;
                            }
                    }
                }
                else
                {
                    byte diffIndex = str[4];
                    ref var midiDiff = ref difficulties[diffIndex];
                    if (midiDiff == null)
                        return;

                    switch (str[5])
                    {
                        case 1:
                            lanes[12 * diffIndex + 1] = str[6] == 0 ? 1 : 0;
                            break;
                        case 4:
                            MidiGuitarHelper.ProcessTapSysex(track[diffIndex]!, midiDiff, position, enable);
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
                track.Events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }
    }
}
