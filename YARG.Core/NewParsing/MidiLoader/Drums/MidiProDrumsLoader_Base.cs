using System.Collections.Generic;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiProDrumsLoader_Base<TDrumConfig, TDiffTracker> : MidiDrumLoader_Base<TDrumConfig, DrumPad_Pro, TDiffTracker>
        where TDrumConfig : unmanaged, IDrumPadConfig<DrumPad_Pro>
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

                if (lane >= MidiDrumLoader_Base.DYNAMIC_MIN)
                {
                    ref var pad = ref drum.Pads[lane - MidiDrumLoader_Base.DYNAMIC_MIN];
                    if (enableDynamics)
                    {
                        if (_note.velocity > 100)
                        {
                            pad.Dynamics = DrumDynamics.Accent;
                        }
                        else if (_note.velocity < 100)
                        {
                            pad.Dynamics = DrumDynamics.Ghost;
                        }
                    }

                    if (_type != DrumsType.FiveLane)
                    {
                        if (TOM_MIN_LANE <= lane && lane <= TOM_MAX_LANE)
                        {
                            pad.CymbalFlag = !_toms[lane - TOM_MIN_LANE];
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

        protected override bool ToggleExtraValues_ON()
        {
            if (!base.ToggleExtraValues_ON() && _type != DrumsType.FiveLane)
            {
                if (TOM_MIN_VALUE <= _note.value && _note.value <= TOM_MAX_VALUE)
                {
                    _type = DrumsType.ProDrums;
                    _toms[_note.value - TOM_MIN_VALUE] = true;
                }
            }
            return true;
        }

        protected override bool ToggleExtraValues_Off()
        {
            if (!base.ToggleExtraValues_Off() && _type != DrumsType.FiveLane && TOM_MIN_VALUE <= _note.value && _note.value <= TOM_MAX_VALUE)
            {
                _toms[_note.value - TOM_MIN_VALUE] = false;
            }
            return true;
        }
    }
}
