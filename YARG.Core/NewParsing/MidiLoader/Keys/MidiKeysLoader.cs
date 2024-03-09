using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class KeysMidiDiff
    {
        public readonly DualTime[] Notes =
        {
            DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };
    }

    public class MidiKeysLoader : MidiBasicInstrumentLoader<KeysNote2, KeysMidiDiff>
    {
        public static BasicInstrumentTrack2<KeysNote2> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiKeysLoader(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private const int NUM_BRELANES = 5;
        private static readonly int[] LANEVALUES = new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };
        private MidiKeysLoader(HashSet<Difficulty>? difficulties)
            : base(difficulties, NUM_BRELANES) { }

        protected override void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (MidiBasicInstrumentLoader.DEFAULT_MIN <= _note.value && _note.value <= MidiBasicInstrumentLoader.DEFAULT_MAX)
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
            if (MidiBasicInstrumentLoader.DEFAULT_MIN <= _note.value && _note.value <= MidiBasicInstrumentLoader.DEFAULT_MAX)
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off())
            {
                ParseBRE_Off();
            }
        }

        private void ParseLaneColor_ON()
        {
            int noteValue = _note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int lane = LANEVALUES[noteValue];
            if (lane < 5)
            {
                int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
                var midiDiff = Difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                midiDiff.Notes[lane] = _position;

                var notes = Track[diffIndex]!.Notes;
                if (notes.Capacity == 0)
                {
                    notes.Capacity = 5000;
                }
                notes.TryAppend(_position);
            }
        }

        private void ParseLaneColor_Off()
        {
            int noteValue = _note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int lane = LANEVALUES[noteValue];
            if (lane < 5)
            {
                int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
                var midiDiff = Difficulties[diffIndex];
                if (midiDiff == null)
                    return;

                ref var colorPosition = ref midiDiff.Notes[lane];
                if (colorPosition.Ticks != -1)
                {
                    Track[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(_position - colorPosition);
                    colorPosition.Ticks = -1;
                }
            }
        }
    }
}
