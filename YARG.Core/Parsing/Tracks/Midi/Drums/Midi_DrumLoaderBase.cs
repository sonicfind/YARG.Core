using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Parsing.Drums;
using YARG.Core.Parsing.Midi.Drums;

namespace YARG.Core.Parsing.Midi
{
    public abstract class Midi_DrumLoaderBase<TDrumConfig, TCymbalConfig, TDiffTracker>
        : MidiInstrumentLoader_Common<DrumNote<TDrumConfig, TCymbalConfig>, TDiffTracker>
        where TDrumConfig : unmanaged, IDrumPadConfig
        where TCymbalConfig : unmanaged, ICymbalConfig
        where TDiffTracker : DrumsMidiDiff, new()
    {
        private static readonly byte[] DYNAMICS_STRING = Encoding.ASCII.GetBytes("[ENABLE_CHART_DYNAMICS]");
        private const int DOUBLEBASS_VALUE = 95;
        private const int DOUBLEBASS_INDEX = 1;
        private const int EXPERT_INDEX = 3;

        protected const int DYNAMIC_MIN = 2;
        protected const int FLAM_VALUE = 109;
        protected static readonly int[] LANEVALUES = new int[] {
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
        };

        static Midi_DrumLoaderBase() { }

        protected Midi_DrumLoaderBase(HashSet<Difficulty>? difficulties) : base(difficulties) { }

        protected bool enableDynamics = false;

        protected override bool ProcessSpecialNote(YARGMidiTrack midiTrack)
        {
            if (note.value != DOUBLEBASS_VALUE)
                return false;
            
            if (difficulties[EXPERT_INDEX] == null)
                return true;

            difficulties[EXPERT_INDEX].Notes[DOUBLEBASS_INDEX] = position;
            if (!track[EXPERT_INDEX]!.Notes.ValidateLastKey(position))
                track[EXPERT_INDEX]!.Notes.Add_NoReturn(position);
            return true;
        }

        protected override bool ProcessSpecialNote_Off(YARGMidiTrack midiTrack)
        {
            if (note.value != DOUBLEBASS_VALUE)
                return false;

            if (difficulties[EXPERT_INDEX] == null)
                return true;

            ref var colorPosition = ref difficulties[EXPERT_INDEX].Notes[DOUBLEBASS_INDEX];
            if (colorPosition.ticks != -1)
            {
                track[EXPERT_INDEX]!.Notes.Traverse_Backwards_Until(colorPosition)[DOUBLEBASS_INDEX] = position - colorPosition;
                colorPosition.ticks = -1;
            }
            return true;
        }

        protected override void ParseText(ReadOnlySpan<byte> str)
        {
            if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                enableDynamics = true;
            else
                track.Events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }
    }
}
