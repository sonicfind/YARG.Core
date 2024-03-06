using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    internal static class MidiDrumLoader_Base
    {
        internal static readonly byte[] DYNAMICS_STRING = Encoding.ASCII.GetBytes("[ENABLE_CHART_DYNAMICS]");
        internal const int DOUBLEBASS_VALUE = 95;
        internal const int DOUBLEBASS_INDEX = 1;
        internal const int EXPERT_INDEX = 3;
        internal const int DYNAMIC_MIN = 2;
        internal const int FLAM_VALUE = 109;
        internal static readonly int[] LANEVALUES = new int[] {
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
        };

        static MidiDrumLoader_Base() { }
    }

    public abstract class MidiDrumLoader_Base<TDrumNote, TDrumConfig, TDiffTracker> : MidiBasicInstrumentLoader<TDrumNote, TDiffTracker>
        where TDrumNote : unmanaged, IDrumNote<TDrumConfig>
        where TDrumConfig : unmanaged, IDrumPadConfig
        where TDiffTracker : DrumsMidiDifficulty, new()
    {
        protected bool enableDynamics = false;

        protected MidiDrumLoader_Base(HashSet<Difficulty>? difficulties, int numBRELanes)
            : base(difficulties, numBRELanes) { }

        internal BasicInstrumentTrack2<TDrumNote> Process(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                Position.Ticks = midiTrack.Position;
                Position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref Note);
                    if (Note.velocity > 0)
                    {
                        ParseNote_ON();
                    }
                    else
                    {
                        ParseNote_Off();
                    }
                }
                else if (midiTrack.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref Note);
                    ParseNote_Off();
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                    ParseText(midiTrack.ExtractTextOrSysEx());
            }

            Track.TrimExcess();
            return Track;
        }

        protected virtual bool IsNote()
        {
            return MidiBasicInstrumentLoader.DEFAULT_MIN <= Note.value && Note.value <= MidiBasicInstrumentLoader.DEFAULT_MAX;
        }

        protected virtual bool ToggleExtraValues_ON()
        {
            if (Note.value != MidiDrumLoader_Base.FLAM_VALUE)
            {
                return false;
            }

            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                if (Difficulties[i] != null)
                {
                    Difficulties[i].Flam = true;
                    unsafe
                    {
                        if (Track[i]!.Notes.TryGetLastValue(Position, out var note))
                        {
                            note->IsFlammed = true;
                        }
                    }
                    
                }
            }
            return true;
        }

        protected virtual bool ToggleExtraValues_Off()
        {
            if (Note.value != MidiDrumLoader_Base.FLAM_VALUE)
            {
                return false;
            }

            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                if (Difficulties[i] != null)
                {
                    Difficulties[i].Flam = false;
                }
            }
            return true;
        }

        protected abstract void ParseLaneColor_ON();

        protected abstract void ParseLaneColor_Off();

        private void ParseNote_ON()
        {
            NormalizeNoteOnPosition();
            if (ProcessSpecialNote_ON())
                return;

            if (IsNote())
            {
                ParseLaneColor_ON();
            }
            else if (!AddPhrase_ON())
            {
                if (!ParseBRE_ON())
                {
                    ToggleExtraValues_ON();
                }
            }
        }

        private void ParseNote_Off()
        {
            if (ProcessSpecialNote_Off())
                return;

            if (IsNote())
            {
                ParseLaneColor_Off();
            }
            else if (!AddPhrase_Off())
            {
                if (!ParseBRE_Off())
                {
                    ToggleExtraValues_Off();
                }
            }
        }

        private bool ProcessSpecialNote_ON()
        {
            if (Note.value != MidiDrumLoader_Base.DOUBLEBASS_VALUE)
            {
                return false;
            }

            if (Difficulties[MidiDrumLoader_Base.EXPERT_INDEX] != null)
            {
                Difficulties[MidiDrumLoader_Base.EXPERT_INDEX].Notes[MidiDrumLoader_Base.DOUBLEBASS_INDEX] = Position;
                Track[MidiDrumLoader_Base.EXPERT_INDEX]!.Notes.TryAppend(Position);
            }
            return true;
        }

        private bool ProcessSpecialNote_Off()
        {
            if (Note.value != MidiDrumLoader_Base.DOUBLEBASS_VALUE)
            {
                return false;
            }

            if (Difficulties[MidiDrumLoader_Base.EXPERT_INDEX] != null)
            {
                ref var colorPosition = ref Difficulties[MidiDrumLoader_Base.EXPERT_INDEX].Notes[MidiDrumLoader_Base.DOUBLEBASS_INDEX];
                if (colorPosition.Ticks != -1)
                {
                    Track[MidiDrumLoader_Base.EXPERT_INDEX]!.Notes.TraverseBackwardsUntil(colorPosition)[MidiDrumLoader_Base.DOUBLEBASS_INDEX] = DualTime.Truncate(Position - colorPosition);
                    colorPosition.Ticks = -1;
                }
            }
            return true;
        }

        private void ParseText(ReadOnlySpan<byte> str)
        {
            if (!enableDynamics && str.SequenceEqual(MidiDrumLoader_Base.DYNAMICS_STRING))
            {
                enableDynamics = true;
            }
            else
            {
                Track.Events.GetLastOrAppend(Position).Add(Encoding.UTF8.GetString(str));
            }
        }
    }
}
