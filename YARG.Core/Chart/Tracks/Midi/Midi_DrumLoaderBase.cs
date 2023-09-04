using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Chart.Drums;

namespace YARG.Core.Chart
{
    public class DrumsMidiDiff
    {
        public bool Flam { get; set; }
        public readonly long[] notes;

        protected DrumsMidiDiff(int numNotes)
        {
            notes = new long[numNotes];
            for (int i = 0; i < numNotes; i++)
                notes[i] = -1;
        }
    }

    public class FourLaneMidiDiff : DrumsMidiDiff
    {
        public FourLaneMidiDiff() : base(6) { }
    }

    public class FiveLaneMidiDiff : DrumsMidiDiff
    {
        public FiveLaneMidiDiff() : base(7) { }
    }

    public abstract class Midi_DrumLoaderBase<TDrumConfig, TCymbalConfig, TDiffTracker> : MidiInstrumentLoader_Common<DrumNote<TDrumConfig, TCymbalConfig>, TDiffTracker>
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

            difficulties[EXPERT_INDEX].notes[DOUBLEBASS_INDEX] = position;
            if (!track[EXPERT_INDEX].notes.ValidateLastKey(position))
                track[EXPERT_INDEX].notes.Add_NoReturn(position);
            return true;
        }

        protected override bool ProcessSpecialNote_Off(YARGMidiTrack midiTrack)
        {
            if (note.value != DOUBLEBASS_VALUE)
                return false;

            if (difficulties[EXPERT_INDEX] == null)
                return true;

            long colorPosition = difficulties[EXPERT_INDEX].notes[DOUBLEBASS_INDEX];
            if (colorPosition != -1)
            {
                track[EXPERT_INDEX].notes.Traverse_Backwards_Until(colorPosition)[DOUBLEBASS_INDEX] = position - colorPosition;
                difficulties[EXPERT_INDEX].notes[DOUBLEBASS_INDEX] = -1;
            }
            return true;
        }

        protected override void ParseText(ReadOnlySpan<byte> str)
        {
            if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                enableDynamics = true;
            else
                track.events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }
    }
}
