using System;
using System.Diagnostics;

namespace YARG.Core.NewParsing.Midi
{
    public abstract class GuitarMidiDifficulty<TDiffTracker, TFretConfig>
        where TFretConfig : unmanaged, IFretConfig<TFretConfig>
        where TDiffTracker : GuitarMidiDifficulty<TDiffTracker, TFretConfig>
    {
        internal bool SliderNotes;
        internal bool HopoOn;
        internal bool HopoOff;

        internal void ProcessTapSysex(DifficultyTrack2<GuitarNote2<TFretConfig>> diff, in DualTime position, bool state)
        {
            if (state)
            {
                ProcessTapSysex_ON(diff, position);
            }
            else
            {
                ProcessTapSysex_Off(diff, position);
            }
        }

        internal void ProcessTapSysex_ON(DifficultyTrack2<GuitarNote2<TFretConfig>> diff, in DualTime position)
        {
            SliderNotes = true;
            unsafe
            {
                if (diff.Notes.TryGetLastValue(position, out var note))
                {
                    note->State = GuitarState.Tap;
                }
            }
        }

        internal void ProcessTapSysex_Off(DifficultyTrack2<GuitarNote2<TFretConfig>> diff, in DualTime position)
        {
            SliderNotes = false;
            unsafe
            {
                if (diff.Notes.TryGetLastValue(position, out var note))
                {
                    if (HopoOn)
                    {
                        note->State = GuitarState.Hopo;
                    }
                    else if (HopoOff)
                    {
                        note->State = GuitarState.Strum;
                    }
                    else
                    {
                        note->State = GuitarState.Natural;
                    }
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
