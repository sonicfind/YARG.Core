using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Parsing.Guitar;

namespace YARG.Core.Parsing.Midi
{
    public abstract class GuitarMidiDifficulty
    {
        public bool SliderNotes;
        public bool HopoOn;
        public bool HopoOff;
    }

    public class FiveFretMidiDifficulty : GuitarMidiDifficulty
    {
        private static readonly int[] PHRASE = { 0 };

        public readonly DualTime[] Notes = new DualTime[6] {
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };

        public readonly Midi_PhraseList phrases;

        public FiveFretMidiDifficulty()
        {
            phrases = new Midi_PhraseList(
                (PHRASE, new(SpecialPhraseType.StarPower_Diff)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player1)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player2))
            );
        }
    }

    public class SixFretMidiDifficulty : GuitarMidiDifficulty
    {
        public readonly DualTime[] Notes = new DualTime[7] {
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };

        public SixFretMidiDifficulty() { }
    }

    public static class MidiGuitarHelper
    {
        public static void ProcessTapSysex<TConfig, TDiffTracker>(InstrumentTrack_FW<GuitarNote<TConfig>> track, TDiffTracker?[] trackers, in DualTime position, bool enable)
            where TConfig : unmanaged, IFretConfig
            where TDiffTracker : GuitarMidiDifficulty
        {
            for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
            {
                var tracker = trackers[diffIndex];
                if (tracker != null)
                    ProcessTapSysex(track[diffIndex]!, tracker, position, enable);
            }
        }

        public static void ProcessTapSysex<TConfig, TDiffTracker>(DifficultyTrack_FW<GuitarNote<TConfig>> diff, TDiffTracker tracker, in DualTime position, bool enable)
            where TConfig : unmanaged, IFretConfig
            where TDiffTracker : GuitarMidiDifficulty
        {
            tracker.SliderNotes = enable;
            if (diff.Notes.ValidateLastKey(position))
            {
                ref var note = ref diff.Notes.Last();
                if (enable)
                    note.State = GuitarState.TAP;
                else if (note.State == GuitarState.TAP)
                {
                    if (tracker.HopoOn)
                        note.State = GuitarState.HOPO;
                    else if (tracker.HopoOff)
                        note.State = GuitarState.STRUM;
                    else
                        note.State = GuitarState.NATURAL;
                }
            }
        }
    }
}
