﻿using System;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiDrumsLoader
    {
        private static readonly byte[] DYNAMICS_STRING = Encoding.ASCII.GetBytes("[ENABLE_CHART_DYNAMICS]");
        private const int DOUBLEBASS_VALUE = 95;
        private const int DOUBLEBASS_INDEX = 1;
        private const int EXPERT_INDEX = 3;
        private const int DYNAMIC_MIN = 2;
        private const int FLAM_VALUE = 109;
        private const int MAX_LANES = 7;
        private const int DRUMNOTE_MAX = 101;
        private static readonly int[] LANEVALUES = new int[] {
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
        };

        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        public static unsafe BasicInstrumentTrack2<DrumNote2<FourLane>> LoadFourLane(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new BasicInstrumentTrack2<DrumNote2<FourLane>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<DrumNote2<FourLane>>();
            }

            const int NUM_LANES = 6;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            var tomFlags = stackalloc bool[3];
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            var phraseInfo = default(SpecialPhraseInfo);
            phraseInfo.Velocity = 100;

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

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                var drum = diffTrack.Notes.GetLastOrAppend(position);
                                drum->IsFlammed = flamFlag;

                                if (lane >= DYNAMIC_MIN)
                                {
                                    int padIndex = lane - DYNAMIC_MIN;
                                    if (enableDynamics)
                                    {
                                        if (note.velocity == 127)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                        }
                                        else if (note.velocity == 1)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Ghost;
                                        }
                                    }

                                    if (padIndex >= 1)
                                    {
                                        ((DrumPad_Pro*) &drum->Pads)[padIndex].CymbalFlag = !tomFlags[padIndex - 1];
                                    }
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane - Kick Index Offset
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.GetLastOrAppend(position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            int index = note.value - TOM_MIN_VALUE;
                            tomFlags[index] = true;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                var diffTrack = instrumentTrack[i]!;
                                if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                {
                                    ((DrumPad_Pro*) &drum->Pads)[index + 1].CymbalFlag = true;
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
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
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = true;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];

                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    diffTrack.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane (- Kick Index Offset)
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    ref var drum = ref instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum.Bass = DualTime.Truncate(position - colorPosition);
                                    drum.IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            int index = note.value - TOM_MIN_VALUE;
                            tomFlags[index] = false;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                var diffTrack = instrumentTrack[i]!;
                                if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                {
                                    ((DrumPad_Pro*) &drum->Pads)[index + 1].CymbalFlag = false;
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
                                phraseInfo.Duration = position - bre;
                                instrumentTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(bre)
                                        .Add(SpecialPhraseType.BRE, phraseInfo);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - overdrivePosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(overdrivePosition)
                                                .Add(SpecialPhraseType.StarPower, phraseInfo);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - tremoloPostion;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(tremoloPostion)
                                                .Add(SpecialPhraseType.Tremolo, phraseInfo);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - trillPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(trillPosition)
                                                .Add(SpecialPhraseType.Trill, phraseInfo);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
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

        public static unsafe BasicInstrumentTrack2<DrumNote2<FiveLane>> LoadFiveLane(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new BasicInstrumentTrack2<DrumNote2<FiveLane>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<DrumNote2<FiveLane>>();
            }

            const int NUM_LANES = 7;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            var phraseInfo = default(SpecialPhraseInfo);
            phraseInfo.Velocity = 100;

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

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                var drum = diffTrack.Notes.GetLastOrAppend(position);
                                drum->IsFlammed = flamFlag;

                                if (enableDynamics && lane >= DYNAMIC_MIN)
                                {
                                    int padIndex = lane - DYNAMIC_MIN;
                                    if (note.velocity == 127)
                                    {
                                        ((DrumPad*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                    }
                                    else if (note.velocity == 1)
                                    {
                                        ((DrumPad*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Ghost;
                                    }
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane - Kick Index Offset
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.GetLastOrAppend(position);
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
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
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = true;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                           
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    diffTrack.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane - Kick Index Offset
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    ref var drum = ref instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum.Bass = DualTime.Truncate(position - colorPosition);
                                    drum.IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
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
                                phraseInfo.Duration = position - bre;
                                instrumentTrack.SpecialPhrases[bre].Add(SpecialPhraseType.BRE, phraseInfo);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - overdrivePosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(overdrivePosition)
                                                .Add(SpecialPhraseType.StarPower, phraseInfo);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - tremoloPostion;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(tremoloPostion)
                                                .Add(SpecialPhraseType.Tremolo, phraseInfo);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - trillPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(trillPosition)
                                                .Add(SpecialPhraseType.Trill, phraseInfo);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
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

        public static unsafe BasicInstrumentTrack2<DrumNote2<UnknownLane>> LoadUnknownDrums(YARGMidiTrack midiTrack, SyncTrack2 sync, ref DrumsType type)
        {
            var instrumentTrack = new BasicInstrumentTrack2<DrumNote2<UnknownLane>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<DrumNote2<UnknownLane>>();
            }

            int NUM_LANES = 7;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * MAX_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            var tomFlags = stackalloc bool[3];
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            var phraseInfo = default(SpecialPhraseInfo);
            phraseInfo.Velocity = 100;

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

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                var drum = diffTrack.Notes.GetLastOrAppend(position);
                                drum->IsFlammed = flamFlag;

                                if (lane >= DYNAMIC_MIN)
                                {
                                    int padIndex = lane - DYNAMIC_MIN;
                                    if (padIndex == 4)
                                    {
                                        type = DrumsType.FiveLane;
                                    }

                                    if (enableDynamics)
                                    {
                                        if (note.velocity == 127)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                        }
                                        else if (note.velocity == 1)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Ghost;
                                        }
                                    }

                                    if (1 <= padIndex && padIndex <= 3)
                                    {
                                        ((DrumPad_Pro*) &drum->Pads)[padIndex].CymbalFlag = !tomFlags[padIndex - 1];
                                    }
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane - Kick Index Offset
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.GetLastOrAppend(position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (type != DrumsType.FiveLane)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                tomFlags[index] = true;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    var diffTrack = instrumentTrack[i]!;
                                    if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                    {
                                        ((DrumPad_Pro*)&drum->Pads)[index + 1].CymbalFlag = true;
                                    }
                                }
                                type = DrumsType.ProDrums;
                                NUM_LANES = 6;
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
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
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = true;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];

                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    diffTrack.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane (- Kick Index Offset)
                            //      60     +     2     *   12                                         +  12    (- 1)
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 12 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + DOUBLEBASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    ref var drum = ref instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum.Bass = DualTime.Truncate(position - colorPosition);
                                    drum.IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (type != DrumsType.FiveLane)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                tomFlags[index] = false;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    var diffTrack = instrumentTrack[i]!;
                                    if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                    {
                                        ((DrumPad_Pro*)&drum->Pads)[index + 1].CymbalFlag = false;
                                    }
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
                                phraseInfo.Duration = position - bre;
                                instrumentTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(bre)
                                        .Add(SpecialPhraseType.BRE, phraseInfo);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - overdrivePosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(overdrivePosition)
                                                .Add(SpecialPhraseType.StarPower, phraseInfo);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - tremoloPostion;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(tremoloPostion)
                                                .Add(SpecialPhraseType.Tremolo, phraseInfo);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - trillPosition;
                                        instrumentTrack.SpecialPhrases
                                                .TraverseBackwardsUntil(trillPosition)
                                                .Add(SpecialPhraseType.Trill, phraseInfo);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        var diffTrack = instrumentTrack[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
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
