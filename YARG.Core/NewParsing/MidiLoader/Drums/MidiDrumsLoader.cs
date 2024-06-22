using System.Collections.Generic;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiDrumsLoader<TDrumConfig, TDiffTracker> : MidiDrumLoader_Base<DrumNote2<TDrumConfig>, TDrumConfig, FourLaneDifficulty>
        where TDrumConfig : unmanaged, IDrumPadConfig<TDrumConfig>
        where TDiffTracker : DrumsMidiDifficulty, new()
    {
        public static BasicInstrumentTrack2<DrumNote2<TDrumConfig>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            var loader = new MidiDrumsLoader<TDrumConfig, TDiffTracker>(difficulties);
            return loader.Process(midiTrack, sync);
        }

        private static readonly int NUM_BRELANES;
        private static readonly int NUM_DRUMLANES;
        static MidiDrumsLoader()
        {
            NUM_BRELANES = IDrumPadConfig<TDrumConfig>.NUM_PADS + 1;
            NUM_DRUMLANES = IDrumPadConfig<TDrumConfig>.NUM_PADS + 2;
        }

        private MidiDrumsLoader(HashSet<Difficulty>? Difficulties)
            : base(Difficulties, NUM_BRELANES)
        {

        }

        protected override void ParseLaneColor_ON()
        {
            int noteValue = _note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
            int diffIndex = MidiBasicInstrumentLoader.DIFFVALUES[noteValue];
            var midiDiff = Difficulties[diffIndex];
            if (midiDiff == null)
                return;

            var notes = Track[diffIndex]!.Notes;
            int lane = MidiDrumLoader_Base.LANEVALUES[noteValue];
            if (lane < NUM_DRUMLANES)
            {
                midiDiff.Notes[lane] = _position;

                if (notes.Capacity == 0)
                    notes.Capacity = 5000;

                ref var drum = ref notes.GetLastOrAppend(_position);
                if (midiDiff.Flam)
                    drum.IsFlammed = true;

                if (enableDynamics && lane >= MidiDrumLoader_Base.DYNAMIC_MIN)
                {
                    int padIndex = lane - MidiDrumLoader_Base.DYNAMIC_MIN;
                    if (_note.velocity > 100)
                    {
                        drum.Pads.SetDynamics(padIndex, DrumDynamics.Accent);
                    }
                    else if (_note.velocity < 100)
                    {
                        drum.Pads.SetDynamics(padIndex, DrumDynamics.Ghost);
                    }
                }
            }
        }

        protected override void ParseLaneColor_Off()
        {
            int noteValue = _note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
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
                    Track[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(_position - colorPosition);
                    colorPosition.Ticks = -1;
                }
            }
        }
    }

    public static class MidiFourLaneLoader
    {
        public static BasicInstrumentTrack2<DrumNote2<FourLane>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            return MidiDrumsLoader<FourLane, FourLaneDifficulty>.Load(midiTrack, sync, difficulties);
        }
    }

    public static class MidiFiveLaneLoader
    {
        public static BasicInstrumentTrack2<DrumNote2<FiveLane>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync, HashSet<Difficulty>? difficulties)
        {
            return MidiDrumsLoader<FiveLane, FiveLaneDifficulty>.Load(midiTrack, sync, difficulties);
        }
    }
}
