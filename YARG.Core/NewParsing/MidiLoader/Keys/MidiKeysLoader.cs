using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiKeysLoader
    {
        public static BasicInstrumentTrack2<KeysNote2> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff>(difficulties, 5);
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
                    {
                        loader.ParseNote_Off();
                    }
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

        private class KeysMidiDiff
        {
            public readonly DualTime[] Notes =
            {
                DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
            };
        }

        private static readonly int[] LANEVALUES = new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        private static void ParseNote_ON(this MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff> loader)
        {
            loader.NormalizeNoteOnPosition();
            if (MidiBasicInstrumentLoader.DEFAULT_MIN <= loader.Note.value && loader.Note.value <= MidiBasicInstrumentLoader.DEFAULT_MAX)
            {
                loader.ParseLaneColor_ON();
            }
            else if (!loader.AddPhrase_ON())
            {
                loader.ParseBRE_ON();
            }
        }

        private static void ParseNote_Off(this MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff> loader)
        {
            if (MidiBasicInstrumentLoader.DEFAULT_MIN <= loader.Note.value && loader.Note.value <= MidiBasicInstrumentLoader.DEFAULT_MAX)
            {
                ParseLaneColor_Off(loader);
            }
            else if (!loader.AddPhrase_Off())
            {
                loader.ParseBRE_Off();
            }
        }

        private static void ParseLaneColor_ON(this MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff> loader)
        {
            int noteValue = loader.Note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int lane = LANEVALUES[noteValue];
            if (lane < 5)
            {
                int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
                var midiDiff =loader.Difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                midiDiff.Notes[lane] = loader.Position;

                var notes = loader.Track[diffIndex]!.Notes;
                if (notes.Capacity == 0)
                {
                    notes.Capacity = 5000;
                }
                notes.TryAppend(loader.Position);
            }
        }

        private static void ParseLaneColor_Off(this MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff> loader)
        {
            int noteValue = loader.Note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int lane = LANEVALUES[noteValue];
            if (lane < 5)
            {
                int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
                var midiDiff = loader.Difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                ref var colorPosition = ref midiDiff.Notes[lane];
                if (colorPosition.Ticks != -1)
                {
                    loader.Track[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(loader.Position - colorPosition);
                    colorPosition.Ticks = -1;
                }
            }
        }
    }
}
