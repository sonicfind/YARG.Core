using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiSixFretLoader
    {
        public static BasicInstrumentTrack2<GuitarNote2<SixFret>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty>(difficulties, 6);
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
                else if (midiTrack.Type == MidiEventType.SysEx || midiTrack.Type == MidiEventType.SysEx_End)
                {
                    loader.ParseSysEx(midiTrack.ExtractTextOrSysEx());
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

        private static readonly int[] LANEVALUES = new int[] {
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
        };

        private static void ParseNote_ON(this MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty> loader)
        {
            loader.NormalizeNoteOnPosition();
            if (58 <= loader.Note.value && loader.Note.value <= 103)
            {
                ParseLaneColor_ON(loader);
            }
            else if (!loader.AddPhrase_ON())
            {
                loader.ParseBRE_ON();
            }
        }

        private static void ParseNote_Off(this MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty> loader)
        {
            if (58 <= loader.Note.value && loader.Note.value <= 103)
            {
                ParseLaneColor_Off(loader);
            }
            else if (!loader.AddPhrase_Off())
            {
                loader.ParseBRE_Off();
            }
        }

        private static void ParseLaneColor_ON(this MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty> loader)
        {
            int noteValue = loader.Note.value - 58;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = LANEVALUES[noteValue];

            var midiDiff = loader.Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            ref var diff = ref loader.Track[diffIndex]!;
            switch (lane)
            {
                case < 7:
                    midiDiff.Notes[lane] = loader.Position;
                    if (!diff.Notes.ValidateLastKey(loader.Position))
                    {
                        if (diff.Notes.Capacity == 0)
                        {
                            diff.Notes.Capacity = 5000;
                        }

                        ref var guitar = ref diff.Notes.Append(loader.Position);
                        if (midiDiff.SliderNotes)
                        {
                            guitar.State = GuitarState.Tap;
                        }
                        else if (midiDiff.HopoOn)
                        {
                            guitar.State = GuitarState.Hopo;
                        }
                        else if (midiDiff.HopoOff)
                        {
                            guitar.State = GuitarState.Strum;
                        }
                    }
                    break;
                case 7:
                    midiDiff.HopoOn = true;
                    if (diff.Notes.ValidateLastKey(loader.Position))
                    {
                        ref var guitar = ref diff.Notes.Last();
                        if (guitar.State == GuitarState.Natural)
                        {
                            guitar.State = GuitarState.Hopo;
                        }
                    }
                    break;
                case 8:
                    midiDiff.HopoOff = true;
                    if (diff.Notes.ValidateLastKey(loader.Position))
                    {
                        ref var guitar = ref diff.Notes.Last();
                        if (guitar.State == GuitarState.Natural)
                        {
                            guitar.State = GuitarState.Strum;
                        }
                    }
                    break;
                case 10:
                    midiDiff.SliderNotes = true;
                    break;
            }
        }

        private static void ParseLaneColor_Off(this MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty> loader)
        {
            int noteValue = loader.Note.value - 58;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            int lane = LANEVALUES[noteValue];

            var midiDiff = loader.Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            ref var diff = ref loader.Track[diffIndex]!;
            switch (lane)
            {
                case < 7:
                    ref var colorPosition = ref loader.Difficulties[diffIndex].Notes[lane];
                    if (colorPosition.Ticks != -1)
                    {
                        diff.Notes.Traverse_Backwards_Until(colorPosition)[lane] = DualTime.Truncate(loader.Position - colorPosition);
                        colorPosition.Ticks = -1;
                    }
                    break;
                case 7:
                    midiDiff.HopoOn = false;
                    if (diff.Notes.ValidateLastKey(loader.Position))
                    {
                        ref var guitar = ref diff.Notes.Last();
                        if (guitar.State != GuitarState.Tap)
                        {
                            guitar.State = GuitarState.Natural;
                        }
                    }
                    break;
                case 8:
                    midiDiff.HopoOff = false;
                    if (diff.Notes.ValidateLastKey(loader.Position))
                    {
                        ref var guitar = ref diff.Notes.Last();
                        if (guitar.State != GuitarState.Tap)
                        {
                            guitar.State = GuitarState.Natural;
                        }
                    }
                    break;
                case 10:
                    midiDiff.SliderNotes = false;
                    break;
            }
        }

        private static void ParseSysEx(this MidiBasicInstrumentLoader<GuitarNote2<SixFret>, SixFretMidiDifficulty> loader, ReadOnlySpan<byte> str)
        {
            if (!str.StartsWith(MidiTrackLoader.SYSEXTAG))
            {
                return;
            }

            bool enable = str[6] == 1;
            if (enable)
            {
                loader.NormalizeNoteOnPosition();
            }

            if (str[5] == 4)
            {
                if (str[4] == (char) 0xFF)
                {
                    SixFretMidiDifficulty.ProcessTapSysex(loader.Track, loader.Difficulties, in loader.Position, enable);
                }
                else
                {
                    byte diffIndex = str[4];
                    ref var midiDiff = ref loader.Difficulties[diffIndex];
                    if (midiDiff != null)
                    {
                        SixFretMidiDifficulty.ProcessTapSysex(loader.Track[diffIndex]!, midiDiff, in loader.Position, enable);
                    }
                }
            }
        }
    }
}
