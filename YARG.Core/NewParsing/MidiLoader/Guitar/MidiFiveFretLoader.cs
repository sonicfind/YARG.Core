﻿using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Song;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiFiveFretLoader
    {
        private const int ALTERNATE_SP_NOTE = 103;
        private static bool _useAlternateOverdrive = false;
        public static void SetOverdriveMidiNote(int note)
        {
            _useAlternateOverdrive = note == ALTERNATE_SP_NOTE;
        }

        private const int FIVEFRET_MIN = 59;
        private const int FIVEFRET_MAX = 106;
        private const int NUM_LANES = 6;
        private const int HOPO_ON_INDEX = 6;
        private const int HOPO_OFF_INDEX = 7;
        private const int SP_SOLO_INDEX = 8;
        private const int TAP_INDEX = 9;
        private const int FACEOFF_1_INDEX = 10;
        private const int FACEOFF_2_INDEX = 11;

        private const int SYSEX_LENGTH = 8;
        private const int SYSEX_DIFF_INDEX = 4;
        private const int SYSEX_TYPE_INDEX = 5;
        private const int SYSEX_STATUS_INDEX = 6;
        private const int SYSEX_OPEN_TYPE = 1;
        private const int SYSEX_TAP_TYPE = 4;
        private const int SYSEX_ALLDIFF = 0xFF;

        private static readonly byte[][] ENHANCED_STRINGS = new byte[][] { Encoding.ASCII.GetBytes("[ENHANCED_OPENS]"), Encoding.ASCII.GetBytes("ENHANCED_OPENS") };

        private struct FiveFretDiff
        {
            public static readonly FiveFretDiff Default = new()
            {
                FaceOff_Player1 = DualTime.Inactive,
                FaceOff_Player2 = DualTime.Inactive,
            };

            public DualTime FaceOff_Player1;
            public DualTime FaceOff_Player2;
            public GuitarMidiDifficulty BaseDiff;
        }

        public static unsafe InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<GuitarNote2<FiveFret>>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<GuitarNote2<FiveFret>>();
            }

            var diffModifiers = stackalloc FiveFretDiff[InstrumentTrack2.NUM_DIFFICULTIES]
            {
                FiveFretDiff.Default, FiveFretDiff.Default, FiveFretDiff.Default, FiveFretDiff.Default,
            };

            // Zero is reserved for open notes. Open notes apply in two situations:
            // 1. The 13s will swap to zeroes when the ENHANCED_OPENS toggle occurs
            // 2. The '1'(green) in a difficulty will swap to zero and back depending on the Open note sysex state
            //
            // Note: the 13s account for the -1 offset of the minimum note value
            var laneIndices = stackalloc int[InstrumentTrack2.NUM_DIFFICULTIES * MidiLoader_Constants.NOTES_PER_DIFFICULTY]
            {
                13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
                13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
                13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
                13, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            };

            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            ReadOnlySpan<byte> SYSEXTAG = stackalloc byte[] { (byte) 'P', (byte) 'S', (byte) '\0', };
            int tempoIndex = 0;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (midiTrack.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (midiTrack.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }

                        if (FIVEFRET_MIN <= note.value && note.value <= FIVEFRET_MAX)
                        {
                            int noteValue = note.value - FIVEFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = laneIndices[noteValue];
                            if (lane < NUM_LANES)
                            {
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (!diffTrack.Notes.TryAppend(position, out var guitar))
                                {
                                    if (diffModifier.BaseDiff.SliderNotes)
                                        guitar->State = GuitarState.Tap;
                                    else if (diffModifier.BaseDiff.HopoOn)
                                        guitar->State = GuitarState.Hopo;
                                    else if (diffModifier.BaseDiff.HopoOff)
                                        guitar->State = GuitarState.Strum;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifier.BaseDiff.HopoOn = true;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                    guitar->State = GuitarState.Hopo;
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifier.BaseDiff.HopoOff = true;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Natural)
                                                    guitar->State = GuitarState.Strum;
                                            }
                                            break;
                                        }
                                    case SP_SOLO_INDEX:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      59      +     3     *   12                                         +   8
                                        // = 103 = Solo
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            if (_useAlternateOverdrive)
                                            {
                                                overdrivePosition = position;
                                            }
                                            else
                                            {
                                                soloPosition = position;
                                            }
                                            instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                        }
                                        break;
                                    case TAP_INDEX:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      59      +     3     *   12                                         +   9
                                        // = 104 = CH-founded Tap
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                            {
                                                diffModifiers[i].BaseDiff.SliderNotes = true;
                                            }
                                        }
                                        break;
                                    case FACEOFF_1_INDEX:
                                        diffModifier.FaceOff_Player1 = position;
                                        diffTrack.SpecialPhrases.GetLastOrAppend(position);
                                        break;
                                    case FACEOFF_2_INDEX:
                                        diffModifier.FaceOff_Player2 = position;
                                        diffTrack.SpecialPhrases.GetLastOrAppend(position);
                                        break;

                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the fivefret note scope for optimization reasons
                            switch (note.value)
                            {
                                // If the alternate overdrive value of 103 is in use, then 116 should be vacant
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (FIVEFRET_MIN <= note.value && note.value <= FIVEFRET_MAX)
                        {
                            int noteValue = note.value - FIVEFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = laneIndices[noteValue];
                            if (lane < NUM_LANES)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    diffTrack.Notes.TraverseBackwardsUntil(colorPosition).Frets[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifier.BaseDiff.HopoOn = false;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                {
                                                    guitar->State = diffModifier.BaseDiff.HopoOff ? GuitarState.Strum : GuitarState.Natural;
                                                }
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifier.BaseDiff.HopoOff = false;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Strum)
                                                    guitar->State = GuitarState.Natural;
                                            }
                                            break;
                                        }
                                    case SP_SOLO_INDEX:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      59      +     3     *   12                                         +   8
                                        // = 103 = Solo
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            if (_useAlternateOverdrive)
                                            {
                                                if (overdrivePosition.Ticks > -1)
                                                {
                                                    var duration = position - overdrivePosition;
                                                    instrumentTrack.SpecialPhrases
                                                            .TraverseBackwardsUntil(overdrivePosition)
                                                            .Add(SpecialPhraseType.StarPower, (duration, 100));
                                                    overdrivePosition.Ticks = -1;
                                                }
                                            }
                                            else
                                            {
                                                if (soloPosition.Ticks > -1)
                                                {
                                                    var duration = position - soloPosition;
                                                    instrumentTrack.SpecialPhrases
                                                            .TraverseBackwardsUntil(soloPosition)
                                                            .Add(SpecialPhraseType.Solo, (duration, 100));
                                                    soloPosition.Ticks = -1;
                                                }
                                            }
                                        }
                                        break;
                                    case TAP_INDEX:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      59      +     3     *   12                                         +   9
                                        // = 104 = CH-founded Tap
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                            {
                                                diffModifiers[i].BaseDiff.SliderNotes = false;
                                            }
                                        }
                                        break;
                                    case FACEOFF_1_INDEX:
                                        if (diffModifier.FaceOff_Player1.Ticks > -1)
                                        {
                                            var duration = position - diffModifier.FaceOff_Player1;
                                            instrumentTrack.SpecialPhrases
                                                    .TraverseBackwardsUntil(diffModifier.FaceOff_Player1)
                                                    .Add(SpecialPhraseType.FaceOff_Player1, (duration, 100));
                                            diffModifier.FaceOff_Player1.Ticks = -1;
                                        }
                                        break;
                                    case FACEOFF_2_INDEX:
                                        if (diffModifier.FaceOff_Player2.Ticks > -1)
                                        {
                                            var duration = position - diffModifier.FaceOff_Player2;
                                            instrumentTrack.SpecialPhrases
                                                    .TraverseBackwardsUntil(diffModifier.FaceOff_Player2)
                                                    .Add(SpecialPhraseType.FaceOff_Player2, (duration, 100));
                                            diffModifier.FaceOff_Player2.Ticks = -1;
                                        }
                                        break;

                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.value - MidiLoader_Constants.BRE_MIN];
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                var duration = position - bre;
                                instrumentTrack.SpecialPhrases[bre].Add(SpecialPhraseType.BRE, (duration, 100));
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the fivefret note scope for optimization reasons
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        var duration = position - overdrivePosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(overdrivePosition)
                                                .Add(SpecialPhraseType.StarPower, (duration, 100));
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        var duration = position - tremoloPostion;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(tremoloPostion)
                                                .Add(SpecialPhraseType.Tremolo, (duration, 100));
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        var duration = position - trillPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(trillPosition)
                                                .Add(SpecialPhraseType.Trill, (duration, 100));
                                        trillPosition.Ticks = -1;
                                    }
                                    break;

                            }
                        }
                    }
                }
                else if (midiTrack.Type is MidiEventType.SysEx or MidiEventType.SysEx_End)
                {
                    var sysex = midiTrack.ExtractTextOrSysEx();
                    if (sysex.Length != 8 || !sysex.SequenceEqual(SYSEXTAG))
                    {
                        continue;
                    }

                    bool enable = sysex[SYSEX_STATUS_INDEX] == 1;
                    if (enable)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }
                    }

                    if (sysex[SYSEX_DIFF_INDEX] == SYSEX_ALLDIFF)
                    {
                        switch (sysex[SYSEX_TYPE_INDEX])
                        {
                            case SYSEX_TAP_TYPE:
                                // 1 - Green; 0 - Open
                                int status = !enable ? 1 : 0;
                                for (int diffIndex = 0; diffIndex < InstrumentTrack2.NUM_DIFFICULTIES; ++diffIndex)
                                {
                                    laneIndices[MidiLoader_Constants.NOTES_PER_DIFFICULTY * diffIndex + 1] = status;
                                }
                                break;
                            case SYSEX_OPEN_TYPE:
                                for (int diffIndex = 0; diffIndex < InstrumentTrack2.NUM_DIFFICULTIES; ++diffIndex)
                                {
                                    var diffTrack = instrumentTrack[diffIndex]!;
                                    diffModifiers[diffIndex].BaseDiff.SliderNotes = enable;
                                    if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                    {
                                        diffModifiers[diffIndex].BaseDiff.ModifyNote(ref *guitar);
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        byte diffIndex = sysex[SYSEX_DIFF_INDEX];
                        switch (sysex[SYSEX_TYPE_INDEX])
                        {
                            case SYSEX_TAP_TYPE:
                                // 1 - Green; 0 - Open
                                laneIndices[MidiLoader_Constants.NOTES_PER_DIFFICULTY * diffIndex + 1] = !enable ? 1 : 0;
                                break;
                            case SYSEX_OPEN_TYPE:
                                {
                                    var diffTrack = instrumentTrack[diffIndex]!;
                                    diffModifiers[diffIndex].BaseDiff.SliderNotes = enable;
                                    if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                    {
                                        diffModifiers[diffIndex].BaseDiff.ModifyNote(ref *guitar);
                                    }
                                    break;
                                }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (laneIndices[0] == 13 && (str.SequenceEqual(ENHANCED_STRINGS[0]) || str.SequenceEqual(ENHANCED_STRINGS[1])))
                    {
                        for (int diff = 0; diff < InstrumentTrack2.NUM_DIFFICULTIES; ++diff)
                        {
                            laneIndices[MidiLoader_Constants.NOTES_PER_DIFFICULTY * diff] = 0;
                        }
                    }
                    else
                    {
                        var ev = Encoding.ASCII.GetString(str);
                        instrumentTrack.Events
                            .GetLastOrAppend(position)
                            .Add(ev);
                    }
                }
            }
            return instrumentTrack;
        }
    }
}
