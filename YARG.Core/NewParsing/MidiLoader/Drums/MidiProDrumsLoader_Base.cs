using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    internal class MidiProDrumsLoader_Base<TDrumConfig, TDiffTracker> : MidiDrumLoader_Base<ProDrumNote2<TDrumConfig>, TDrumConfig, TDiffTracker>
        where TDrumConfig : unmanaged, IDrumPadConfig
        where TDiffTracker : DrumsMidiDifficulty, new()
    {
        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        private const int TOM_MIN_LANE = 3;
        private const int TOM_MAX_LANE = 5;
        private const int NUM_BRELANES = 5;
        private const int NUM_DRUMLANES = 6;

        private readonly bool[] _toms = new bool[3];
        internal DrumsType _type;

        internal MidiProDrumsLoader_Base(HashSet<Difficulty>? Difficulties, DrumsType type)
            : base(Difficulties, NUM_BRELANES)
        {
            _type = type;
        }

        protected override void ParseLaneColor_ON()
        {
            int noteValue = Note.value - MidiBasicInstrumentLoader.DEFAULT_MIN;
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
                            drum.Pads.SetDynamics(padIndex, DrumDynamics.Accent);
                        }
                        else if (Note.velocity < 100)
                        {
                            drum.Pads.SetDynamics(padIndex, DrumDynamics.Ghost);
                        }
                    }

                    if (_type != DrumsType.FiveLane)
                    {
                        if (TOM_MIN_LANE <= lane && lane <= TOM_MAX_LANE)
                        {
                            int cymbalIndex = lane - TOM_MIN_LANE;
                            drum.Cymbals[cymbalIndex] = !_toms[cymbalIndex];
                        }
                        else if (_type == DrumsType.UnknownPro && lane > TOM_MAX_LANE)
                        {
                            _type = DrumsType.FiveLane;
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

        protected override bool ToggleExtraValues_ON()
        {
            if (!base.ToggleExtraValues_ON() && _type != DrumsType.FiveLane)
            {
                if (TOM_MIN_VALUE <= Note.value && Note.value <= TOM_MAX_VALUE)
                {
                    _type = DrumsType.ProDrums;
                    _toms[Note.value - TOM_MIN_VALUE] = true;
                }
            }
            return true;
        }

        protected override bool ToggleExtraValues_Off()
        {
            if (!base.ToggleExtraValues_Off() && _type != DrumsType.FiveLane && TOM_MIN_VALUE <= Note.value && Note.value <= TOM_MAX_VALUE)
            {
                _toms[Note.value - TOM_MIN_VALUE] = false;
            }
            return true;
        }
    }
}
