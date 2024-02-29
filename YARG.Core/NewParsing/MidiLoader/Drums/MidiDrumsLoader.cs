using System.Collections.Generic;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiDrumsLoader<TDrumConfig, TDiffTracker> : MidiDrumLoader_Base<DrumNote2<TDrumConfig>, TDrumConfig, FourLaneDifficulty>
            where TDrumConfig : unmanaged, IDrumPadConfig
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
            NUM_BRELANES = IDrumNote<TDrumConfig>.NUM_PADS + 1;
            NUM_DRUMLANES = IDrumNote<TDrumConfig>.NUM_PADS + 2;
        }

        private MidiDrumsLoader(HashSet<Difficulty>? Difficulties)
            : base(Difficulties, NUM_BRELANES)
        {

        }

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
                        unsafe
                        {
                            fixed (void* ptr = &drum.Pads)
                            {
                                if (Note.velocity > 100)
                                {
                                    ((DrumPad*) ptr)->Dynamics = DrumDynamics.Accent;
                                }
                                else if (Note.velocity < 100)
                                {
                                    ((DrumPad*) ptr)->Dynamics = DrumDynamics.Ghost;
                                }
                            }
                        }
                    }
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
