using System;
using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Guitar;

namespace YARG.Core.Chart
{
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

            ref var diff = ref track[diffIndex]!;
            int lane = LANEVALUES[noteValue];
            if (lane < 7)
            {
                midiDiff.notes[lane] = position;
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
            else if (lane == 7)
            {
                midiDiff.HopoOn = true;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State == GuitarState.NATURAL)
                        guitar.State = GuitarState.HOPO;
                }
            }
            // HopoOff marker
            else if (lane == 8)
            {
                midiDiff.HopoOff = true;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State == GuitarState.NATURAL)
                        guitar.State = GuitarState.STRUM;
                }
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

            ref var diff = ref track[diffIndex]!;
            int lane = LANEVALUES[noteValue];
            if (lane < 7)
            {
                long colorPosition = midiDiff.notes[lane];
                if (colorPosition != -1)
                {
                    diff.Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    midiDiff.notes[lane] = -1;
                }
            }
            else if (lane == 7)
            {
                midiDiff.HopoOn = false;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State != GuitarState.TAP)
                        guitar.State = GuitarState.NATURAL;
                }
            }
            else if (lane == 8)
            {
                midiDiff.HopoOff = false;
                if (diff.Notes.ValidateLastKey(position))
                {
                    ref var guitar = ref diff.Notes.Last();
                    if (guitar.State != GuitarState.TAP)
                        guitar.State = GuitarState.NATURAL;
                }
            }
            else if (lane == 10)
                midiDiff.SliderNotes = false;
        }

        protected override void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (str.StartsWith(SYSEXTAG))
            {
                bool enable = str[6] == 1;
                if (enable)
                    NormalizeNoteOnPosition();

                if (str[5] == 4)
                {
                    if (str[4] == (char) 0xFF)
                        MidiGuitarHelper.ProcessTapSysex(track, difficulties, position, enable);
                    else
                    {
                        byte diffIndex = str[4];
                        var midiDiff = difficulties[diffIndex];
                        if (midiDiff == null)
                            return;

                        MidiGuitarHelper.ProcessTapSysex(track[diffIndex]!, midiDiff, position, enable);
                    }
                }
            }
        }
    }
}
