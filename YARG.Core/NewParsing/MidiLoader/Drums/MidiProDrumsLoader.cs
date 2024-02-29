using System.Collections.Generic;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiProDrumsLoader : MidiDrumLoader_Base<ProDrumNote2<FourLane>, FourLane, FourLaneDifficulty>
    {
        public static BasicInstrumentTrack2<ProDrumNote2<FourLane>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiProDrumsLoader(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        private const int TOM_MIN_LANE = 3;
        private const int NUM_BRELANES = 5;
        private const int NUM_DRUMLANES = 6;

        private readonly bool[] _toms = new bool[3];

        private MidiProDrumsLoader(HashSet<Difficulty>? Difficulties) : base(Difficulties, NUM_BRELANES) { }

        protected override void ParseLaneColor_ON()
        {
            int noteValue = Note.value - 60;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            var notes = Track[diffIndex]!.Notes;
            int lane = MidiDrumLoader_Base.LANEVALUES[noteValue];
            if (lane < NUM_DRUMLANES)
            {
                midiDiff.Notes[lane] = Position;

                if (notes.Capacity == 0)
                    notes.Capacity = 5000;

                ref var drum = ref notes.GetLastOrAppend(Position);
                if (midiDiff.Flam)
                    drum.IsFlammed = true;

                if (lane >= MidiDrumLoader_Base.DYNAMIC_MIN)
                {
                    if (enableDynamics)
                    {
                        int padIndex = lane - MidiDrumLoader_Base.DYNAMIC_MIN;
                        if (Note.velocity > 100)
                        {
                            switch (padIndex)
                            {
                                case 0: drum.Pads.Snare.Dynamics  = DrumDynamics.Accent; break;
                                case 1: drum.Pads.Yellow.Dynamics = DrumDynamics.Accent; break;
                                case 2: drum.Pads.Blue.Dynamics   = DrumDynamics.Accent; break;
                                case 3: drum.Pads.Green.Dynamics  = DrumDynamics.Accent; break;
                            }
                        }
                        else if (Note.velocity < 100)
                        {
                            switch (padIndex)
                            {
                                case 0: drum.Pads.Snare.Dynamics  = DrumDynamics.Ghost; break;
                                case 1: drum.Pads.Yellow.Dynamics = DrumDynamics.Ghost; break;
                                case 2: drum.Pads.Blue.Dynamics   = DrumDynamics.Ghost; break;
                                case 3: drum.Pads.Green.Dynamics  = DrumDynamics.Ghost; break;
                            }
                        }                    
                    }

                    int index = lane - TOM_MIN_LANE;
                    if (index >= 0)
                        drum.Cymbals[index] = !_toms[index];
                }
            }
        }

        protected override void ParseLaneColor_Off()
        {
            int noteValue = Note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            int lane = MidiDrumLoader_Base.LANEVALUES[noteValue];
            if (lane < NUM_DRUMLANES)
            {
                ref var colorPosition = ref midiDiff.Notes[lane];
                if (colorPosition.Ticks != -1)
                {
                    Track[diffIndex]!.Notes.Traverse_Backwards_Until(colorPosition)[lane] = DualTime.Truncate(Position - colorPosition);
                    colorPosition.Ticks = -1;
                }
            }
        }

        protected override bool ToggleExtraValues_ON()
        {
            if (!base.ToggleExtraValues_ON() && TOM_MIN_VALUE <= Note.value && Note.value <= TOM_MAX_VALUE)
            {
                _toms[Note.value - TOM_MIN_VALUE] = true;
            }
            return true;
        }

        protected override bool ToggleExtraValues_Off()
        {
            if (!base.ToggleExtraValues_Off() && TOM_MIN_VALUE <= Note.value && Note.value <= TOM_MAX_VALUE)
            {
                _toms[Note.value - TOM_MIN_VALUE] = false;
            }
            return true;
        }
    }
}
