using System.Collections.Generic;
using YARG.Core.IO;
using YARG.Core.Parsing.Drums;
using YARG.Core.Parsing.Midi.Drums;

namespace YARG.Core.Parsing.Midi
{
    public abstract class Midi_BasicDrum_Loader<TDrumConfig, TDiffTracker> : Midi_DrumLoaderBase<TDrumConfig, Basic_Drums, TDiffTracker>
        where TDrumConfig : unmanaged, IDrumPadConfig
        where TDiffTracker : DrumsMidiDiff, new()
    {
        private readonly int numLanes;
        private readonly int maxNoteValue;
        protected Midi_BasicDrum_Loader(int numLanes, int maxNoteValue, HashSet<Difficulty>? difficulties) : base(difficulties)
        {
            this.numLanes = numLanes;
            this.maxNoteValue = maxNoteValue;
        }

        protected override bool IsNote() { return DEFAULT_MIN <= note.value && note.value <= maxNoteValue; }

        protected override void ParseLaneColor(YARGMidiTrack midiTrack)
        {
            int noteValue = note.value - DEFAULT_MIN;
            int diffIndex = DIFFVALUES[noteValue];
            var midiDiff = difficulties[diffIndex];
            if (midiDiff == null)
                return;

            var notes = track[diffIndex]!.Notes;
            int lane = LANEVALUES[noteValue];
            if (lane < numLanes)
            {
                midiDiff.Notes[lane] = position;

                if (notes.Capacity == 0)
                    notes.Capacity = 5000;

                ref var drum = ref notes.Get_Or_Add_Last(position);
                if (midiDiff.Flam)
                    drum.IsFlammed = true;

                if (enableDynamics && lane >= DYNAMIC_MIN)
                {
                    ref var pad = ref drum.Pads[lane - DYNAMIC_MIN];
                    if (note.velocity > 100)
                        pad.Dynamics = DrumDynamics.Accent;
                    else if (note.velocity < 100)
                        pad.Dynamics = DrumDynamics.Ghost;
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
            if (lane < numLanes)
            {
                ref var colorPosition = ref difficulties[diffIndex].Notes[lane];
                if (colorPosition.ticks != -1)
                {
                    track[diffIndex]!.Notes.Traverse_Backwards_Until(colorPosition)[lane] = position - colorPosition;
                    colorPosition.ticks = -1;
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
        }

        protected override void ToggleExtraValues_Off(YARGMidiTrack midiTrack)
        {
            if (note.value == FLAM_VALUE)
                for (uint i = 0; i < NUM_DIFFICULTIES; ++i)
                    if (difficulties[i] != null)
                        difficulties[i].Flam = false;
        }
    }

    public class Midi_FourLane_Loader : Midi_BasicDrum_Loader<DrumPad_4, FourLaneDiff>
    {
        private const int NUMLANES = 6;
        private Midi_FourLane_Loader(HashSet<Difficulty>? difficulties) : base(NUMLANES, DEFAULT_MAX, difficulties) { }

        public static InstrumentTrack_FW<DrumNote<DrumPad_4, Basic_Drums>> Load(YARGMidiTrack midiTrack, SyncTrack_FW sync, HashSet<Difficulty>? difficulties)
        {
            Midi_FourLane_Loader loader = new(difficulties);
            return loader.Process(sync, midiTrack);
        }
    }

    public class Midi_FiveLane_Loader : Midi_BasicDrum_Loader<DrumPad_5, FiveLaneDiff>
    {
        private const int NUMLANES = 7;
        private const int FIVELANE_MAX = 101;
        private Midi_FiveLane_Loader(HashSet<Difficulty>? difficulties) : base(NUMLANES, FIVELANE_MAX, difficulties) { }

        public static InstrumentTrack_FW<DrumNote<DrumPad_5, Basic_Drums>> Load(YARGMidiTrack midiTrack, SyncTrack_FW sync, HashSet<Difficulty>? difficulties)
        {
            Midi_FiveLane_Loader loader = new(difficulties);
            return loader.Process(sync, midiTrack);
        }
    }
}
