using System;
using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Guitar;

namespace YARG.Core.Chart
{
    public class SixFretMidiDifficulty
    {
        public bool SliderNotes { get; set; }
        public bool HopoOn { get; set; }
        public bool HopoOff { get; set; }
        public readonly long[] notes = new long[7] { -1, -1, -1, -1, -1, -1, -1 };
        public SixFretMidiDifficulty() { }
    }

    public class Midi_SixFretLoader : MidiInstrumentLoader_Common<GuitarNote<SixFret>, SixFretMidiDifficulty>
    {
        private static readonly int[] LANEVALUES = new int[] {
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
        };

        static Midi_SixFretLoader() { }

        private Midi_SixFretLoader(HashSet<Difficulty>? difficulties) : base(difficulties) { }

        public static InstrumentTrack_FW<GuitarNote<SixFret>> Load(YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties)
        {
            Midi_SixFretLoader loader = new(difficulties);
            return loader.Process(midiTrack);
        }

        protected override bool IsNote() { return 58 <= note.value && note.value <= 103; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 58;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            if (lane < 7)
            {
                midiDiff.notes[lane] = position;
                if (!track[diffIndex]!.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref track[diffIndex]!.Notes.Add(position);
                    if (midiDiff.SliderNotes)
                        guitar.IsTap = true;

                    if (midiDiff.HopoOn)
                        guitar.Forcing = ForceStatus.HOPO;
                    else if (midiDiff.HopoOff)
                        guitar.Forcing = ForceStatus.STRUM;
                }
            }
            else if (lane == 7)
            {
                midiDiff.HopoOn = true;
                if (track[diffIndex]!.Notes.ValidateLastKey(position))
                    track[diffIndex]!.Notes.Last().Forcing = ForceStatus.HOPO;
            }
            // HopoOff marker
            else if (lane == 8)
            {
                midiDiff.HopoOff = true;
                if (track[diffIndex]!.Notes.ValidateLastKey(position))
                    track[diffIndex]!.Notes.Last().Forcing = ForceStatus.STRUM;
            }
            else if (lane == 10)
                midiDiff.SliderNotes = true;
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 58;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            if (lane < 7)
            {
                long colorPosition = midiDiff.notes[lane];
                if (colorPosition != -1)
                {
                    track[diffIndex]!.Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    midiDiff.notes[lane] = -1;
                }
            }
            else if (lane == 7)
                midiDiff.HopoOn = false;
            else if (lane == 8)
                midiDiff.HopoOff = false;
            else if (lane == 10)
                midiDiff.SliderNotes = false;
        }

        protected override void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (str.StartsWith(SYSEXTAG))
            {
                if (str[6] == 1)
                    NormalizeNoteOnPosition();

                if (str[5] == 4)
                {
                    if (str[4] == (char) 0xFF)
                    {
                        for (int diff = 0; diff < 4; ++diff)
                        {
                            if (difficulties[diff] == null)
                                continue;

                            difficulties[diff].SliderNotes = str[6] == 1;
                            if (str[6] == 1 && track[diff]!.Notes.ValidateLastKey(position))
                                track[diff]!.Notes.Last().IsTap = true;
                        }
                    }
                    else
                    {
                        byte diff = str[4];
                        if (difficulties[diff] == null)
                            return;

                        if (str[6] == 1)
                        {
                            difficulties[diff].SliderNotes = true;
                            if (track[diff]!.Notes.ValidateLastKey(position))
                                track[diff]!.Notes.Last().IsTap = true;
                        }
                        else
                            difficulties[diff].SliderNotes = false;
                    }
                }
            }
        }
    }
}
