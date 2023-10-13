using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.Chart.Guitar;

namespace YARG.Core.Chart
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

        public readonly long[] notes = new long[6] { -1, -1, -1, -1, -1, -1 };
        public readonly Midi_PhraseList phrases;

        public FiveFretMidiDifficulty()
        {
            phrases = new(new (int[], Midi_Phrase)[] {
                (PHRASE, new(SpecialPhraseType.StarPower_Diff)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player1)),
                (PHRASE, new(SpecialPhraseType.FaceOff_Player2)),
            });
        }
    }

    public class SixFretMidiDifficulty : GuitarMidiDifficulty
    {
        public readonly long[] notes = new long[7] { -1, -1, -1, -1, -1, -1, -1 };
        public SixFretMidiDifficulty() { }
    }

    public static class MidiGuitarHelper
    {
        public static void ProcessTapSysex<TConfig, TDiffTracker>(InstrumentTrack_FW<GuitarNote<TConfig>> track, TDiffTracker?[] trackers, long position, bool enable)
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

        public static void ProcessTapSysex<TConfig, TDiffTracker>(DifficultyTrack_FW<GuitarNote<TConfig>> diff, TDiffTracker tracker, long position, bool enable)
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
