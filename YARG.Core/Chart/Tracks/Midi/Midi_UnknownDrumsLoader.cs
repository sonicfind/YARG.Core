using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Chart.Drums;

namespace YARG.Core.Chart
{
    public class Midi_UnknownDrums_Loader : Midi_DrumLoaderBase<DrumPad_5, Pro_Drums, FiveLaneMidiDiff>
    {
        private const int NOTE_MAX = 101;
        private const int LANE_MAX = 7;
        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        private const int TOM_MIN_LANE = 3;
        private const int FIVE_LANE_DRUM = 6;
        
        private DrumsType type;

        private Midi_UnknownDrums_Loader(DrumsType type, HashSet<Difficulty>? difficulties) : base(difficulties)
        {
            this.type = type;
        }

        public static (InstrumentTrack_FW<DrumNote<DrumPad_5, Pro_Drums>>, DrumsType) Load(YARGMidiTrack midiTrack, DrumsType type, HashSet<Difficulty>? difficulties)
        {
            Midi_UnknownDrums_Loader loader = new(type, difficulties);
            var track = loader.Process(midiTrack);
            return (track, loader.type);
        }

        private readonly bool[] toms = new bool[3];
        protected override bool IsNote() { return DEFAULT_MIN <= note.value && note.value <= NOTE_MAX; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - DEFAULT_MIN;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = LANEVALUES[noteValue];
            if (lane < LANE_MAX)
            {
                midiDiff.notes[lane] = position;
                ref var drum = ref track[diffIndex].notes.Get_Or_Add_Last(position);
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

                    if (TOM_MIN_LANE <= lane && lane < FIVE_LANE_DRUM)
                        drum.Cymbals[lane - TOM_MIN_LANE] = !toms[lane - TOM_MIN_LANE];
                    else if (lane == FIVE_LANE_DRUM)
                        type = DrumsType.FiveLane;
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
            if (lane < LANE_MAX)
            {
                long colorPosition = midiDiff.notes[lane];
                if (colorPosition != -1)
                {
                    track[diffIndex].notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
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
                    if (track[i].notes.ValidateLastKey(position))
                        track[i].notes.Last().IsFlammed = true;
                }
            }
            else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
            {
                toms[note.value - TOM_MIN_VALUE] = true;
                type = DrumsType.ProDrums;
            }
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
