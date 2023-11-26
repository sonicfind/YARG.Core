using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Parsing.Keys;

namespace YARG.Core.Parsing.Midi
{
    public class KeysMidiDiff
    {
        public DualTime[] Notes = {
            DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };
    }

    public class Midi_KeysLoader : MidiInstrumentLoader_Common<KeyNote, KeysMidiDiff>
    {
        private static readonly int[] lanes = new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        static Midi_KeysLoader() { }

        private Midi_KeysLoader(HashSet<Difficulty>? difficulties) : base(difficulties) { }

        public static InstrumentTrack_FW<KeyNote> Load(YARGMidiTrack midiTrack, SyncTrack_FW sync, HashSet<Difficulty>? difficulties)
        {
            Midi_KeysLoader loader = new(difficulties);
            return loader.Process(sync, midiTrack);
        }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 60;
            int lane = lanes[noteValue];
            if (lane < 5)
            {
                int diffIndex = DIFFVALUES[noteValue];
                var midiDiff = difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                
                midiDiff.Notes[lane] = position;

                var notes = track[diffIndex]!.Notes;
                if (notes.Capacity == 0)
                    notes.Capacity = 5000;

                if (!notes.ValidateLastKey(position))
                    notes.Add_NoReturn(position);
            }
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 60;
            int lane = lanes[noteValue];
            if (lane < 5)
            {
                int diffIndex = DIFFVALUES[noteValue];
                var midiDiff = difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                ref var colorPosition = ref midiDiff.Notes[lane];
                if (colorPosition.ticks != -1)
                {
                    track[diffIndex]!.Notes.Traverse_Backwards_Until(colorPosition)[lane] = DualTime.Truncate(position - colorPosition);
                    colorPosition.ticks = -1;
                }
            }
        }
    }
}
