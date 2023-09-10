using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Drums;

namespace YARG.Core.Chart
{
    public class Midi_ProDrum_Loader : Midi_DrumLoaderBase<DrumPad_4, Pro_Drums, FourLaneMidiDiff>
    {
        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        private const int TOM_MIN_LANE = 3;
        private const int NUMLANES = 6;
        private readonly bool[] toms = new bool[3];

        private Midi_ProDrum_Loader(HashSet<Difficulty>? difficulties) : base(difficulties) { }

        public static InstrumentTrack_FW<DrumNote<DrumPad_4, Pro_Drums>> Load(YARGMidiTrack midiTrack, HashSet<Difficulty>? difficulties)
        {
            Midi_ProDrum_Loader loader = new(difficulties);
            return loader.Process(midiTrack);
        }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - 60;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            var notes = track[diffIndex]!.Notes;
            int lane = LANEVALUES[noteValue];
            if (lane < NUMLANES)
            {
                midiDiff.notes[lane] = position;

                if (notes.Capacity == 0)
                    notes.Capacity = 5000;

                ref var drum = ref notes.Get_Or_Add_Last(position);
                if (midiDiff.Flam)
                    drum.IsFlammed = true;

                if (lane >= DYNAMIC_MIN)
                {
                    if (enableDynamics)
                    {
                        ref var pad = ref drum.Pads[lane - DYNAMIC_MIN];
                        if (note.velocity > 100)
                            pad.Dynamics = DrumDynamics.Accent;
                        else if (note.velocity < 100)
                            pad.Dynamics = DrumDynamics.Ghost;
                    }

                    int index = lane - TOM_MIN_LANE;
                    if (index >= 0)
                        drum.Cymbals[index] = !toms[index];
                }
            }
        }

        protected override void ParseLaneColor_Off(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - DEFAULT_MIN;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            if (lane < NUMLANES)
            {
                long colorPosition = midiDiff.notes[lane];
                if (colorPosition != -1)
                {
                    track[diffIndex]!.Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    midiDiff.notes[lane] = -1;
                }
            }
        }

        protected override void ToggleExtraValues(YARGMidiTrack midiTrack)
        {
            if (note.value == FLAM_VALUE)
            {
                for (int i = 0; i < NUM_DIFFICULTIES; ++i)
                {
                    if (difficulties[i] == null)
                        continue;

                    difficulties[i].Flam = true;
                    if (track[i]!.Notes.ValidateLastKey(position))
                        track[i]!.Notes.Last().IsFlammed = true;
                }
            }
            else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                toms[note.value - TOM_MIN_VALUE] = true;
        }

        protected override void ToggleExtraValues_Off(YARGMidiTrack midiTrack)
        {
            if (note.value == FLAM_VALUE)
            {
                for (uint i = 0; i < NUM_DIFFICULTIES; ++i)
                    if (difficulties[i] != null)
                        difficulties[i].Flam = false;
            }
            else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                toms[note.value - TOM_MIN_VALUE] = false;
        }
    }
}
