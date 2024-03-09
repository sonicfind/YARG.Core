using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiSixFretLoader : MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty>
    {
        public static BasicInstrumentTrack2<GuitarNote2<SixFret>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiSixFretLoader(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private const int NUM_BRELANES = 6;
        private static readonly int[] LANEVALUES = new int[] {
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
        };

        private MidiSixFretLoader(HashSet<Difficulty>? difficulties)
            : base(difficulties, NUM_BRELANES) { }

        protected override void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (58 <= _note.value && _note.value <= 103)
            {
                ParseLaneColor_ON();
            }
            else if (!AddPhrase_ON())
            {
                ParseBRE_ON();
            }
        }

        protected override void ParseNote_Off()
        {
            if (58 <= _note.value && _note.value <= 103)
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off())
            {
                ParseBRE_Off();
            }
        }

        protected override void ParseSysEx(ReadOnlySpan<byte> str)
        {
            if (!str.StartsWith(SYSEXTAG))
            {
                return;
            }

            bool enable = str[6] == 1;
            if (enable)
            {
                NormalizeNoteOnPosition();
            }

            if (str[5] == 4)
            {
                if (str[4] == (char) 0xFF)
                {
                    for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
                    {
                        Difficulties[diffIndex]?.ProcessTapSysex(Track[diffIndex]!, _position, enable);
                    }
                }
                else
                {
                    Difficulties[str[4]]?.ProcessTapSysex(Track[str[4]]!, _position, enable);
                }
            }
        }

        private void ParseLaneColor_ON()
        {
            int noteValue = _note.value - 58;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = LANEVALUES[noteValue];

            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            ref var diff = ref Track[diffIndex]!;
            switch (lane)
            {
                case < 7:
                    midiDiff.Notes[lane] = _position;
                    if (diff.Notes.Capacity == 0)
                    {
                        diff.Notes.Capacity = 5000;
                    }

                    unsafe
                    {
                        if (!diff.Notes.TryAppend(_position, out var note))
                        {
                            if (midiDiff.SliderNotes)
                            {
                                note->State = GuitarState.Tap;
                            }
                            else if (midiDiff.HopoOn)
                            {
                                note->State = GuitarState.Hopo;
                            }
                            else if (midiDiff.HopoOff)
                            {
                                note->State = GuitarState.Strum;
                            }
                        }
                    }
                    break;
                case 7:
                    midiDiff.HopoOn = true;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(_position, out var note))
                        {
                            if (note->State == GuitarState.Natural)
                            {
                                note->State = GuitarState.Hopo;
                            }
                        }
                    }
                    break;
                case 8:
                    midiDiff.HopoOff = true;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(_position, out var note))
                        {
                            if (note->State == GuitarState.Natural)
                            {
                                note->State = GuitarState.Strum;
                            }
                        }
                    }
                    break;
                case 10:
                    midiDiff.SliderNotes = true;
                    break;
            }
        }

        private void ParseLaneColor_Off()
        {
            int noteValue = _note.value - 58;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = LANEVALUES[noteValue];

            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            ref var diff = ref Track[diffIndex]!;
            switch (lane)
            {
                case < 7:
                    ref var colorPosition = ref Difficulties[diffIndex].Notes[lane];
                    if (colorPosition.Ticks != -1)
                    {
                        diff.Notes.TraverseBackwardsUntil(colorPosition).Frets[lane] = DualTime.Truncate(_position - colorPosition);
                        colorPosition.Ticks = -1;
                    }
                    break;
                case 7:
                    midiDiff.HopoOn = false;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(_position, out var note))
                        {
                            if (note->State != GuitarState.Tap)
                            {
                                note->State = GuitarState.Natural;
                            }
                        }
                    }
                    break;
                case 8:
                    midiDiff.HopoOff = false;
                    unsafe
                    {
                        if (diff.Notes.TryGetLastValue(_position, out var note))
                        {
                            if (note->State != GuitarState.Tap)
                            {
                                note->State = GuitarState.Natural;
                            }
                        }
                    }
                    break;
                case 10:
                    midiDiff.SliderNotes = false;
                    break;
            }
        }
    }
}
