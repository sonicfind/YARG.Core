using System;

namespace YARG.Core.NewParsing.Midi
{
    public abstract class GuitarMidiDifficulty<TDiffTracker, TFretConfig>
        where TFretConfig : unmanaged, IFretConfig
        where TDiffTracker : GuitarMidiDifficulty<TDiffTracker, TFretConfig>
    {
        internal bool SliderNotes;
        internal bool HopoOn;
        internal bool HopoOff;

        internal static void ProcessTapSysex(BasicInstrumentTrack2<GuitarNote2<TFretConfig>> track, TDiffTracker?[] trackers, in DualTime position, bool state)
        {
            for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
            {
                var tracker = trackers[diffIndex];
                if (tracker != null)
                {
                    ProcessTapSysex(track[diffIndex]!, tracker, position, state);
                }
            }
        }

        internal static void ProcessTapSysex(DifficultyTrack2<GuitarNote2<TFretConfig>> diff, TDiffTracker tracker, in DualTime position, bool state)
        {
            tracker.SliderNotes = state;
            if (diff.Notes.ValidateLastKey(position))
            {
                ref var note = ref diff.Notes.Last();
                if (state)
                {
                    note.State = GuitarState.Tap;
                }
                else if (note.State == GuitarState.Tap)
                {
                    if (tracker.HopoOn)
                        note.State = GuitarState.Hopo;
                    else if (tracker.HopoOff)
                        note.State = GuitarState.Strum;
                    else
                        note.State = GuitarState.Natural;
                }
            }
        }
    }

    public class FiveFretMidiDifficulty : GuitarMidiDifficulty<FiveFretMidiDifficulty, FiveFret>
    {
        // These are used for the difficulty-based override, so the value mapping is irrelevant
        private static readonly Midi_PhraseMapping OVERDRIVE_DIFFICULTY = new(Array.Empty<int>(), SpecialPhraseType.StarPower_Diff);
        private static readonly Midi_PhraseMapping FACEOFF_PLAYER1 = new(Array.Empty<int>(), SpecialPhraseType.FaceOff_Player1);
        private static readonly Midi_PhraseMapping FACEOFF_PLAYER2 = new(Array.Empty<int>(), SpecialPhraseType.FaceOff_Player2);


        internal readonly DualTime[] Notes = new DualTime[6]
        {
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };

        internal readonly Midi_PhraseMapping[] Phrases =
        {
            OVERDRIVE_DIFFICULTY, FACEOFF_PLAYER1, FACEOFF_PLAYER2
        };
    }

    public class SixFretMidiDifficulty : GuitarMidiDifficulty<SixFretMidiDifficulty, SixFret>
    {
        internal readonly DualTime[] Notes = new DualTime[7]
        {
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive
        };
    }
}
