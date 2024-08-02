using System;
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

            int tempoIndex = 0;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                                        if (note.Velocity == 127)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                        }
                                        else if (note.Velocity == 1)
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
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            int index = note.Value - TOM_MIN_VALUE;
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
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.Value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            int index = note.Value - TOM_MIN_VALUE;
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
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                var duration = position - bre;
                                instrumentTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(bre)
                                        .Add(SpecialPhraseType.BRE, (duration, 100));
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        var ev = str.GetString(Encoding.ASCII);
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

            int tempoIndex = 0;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                                    if (note.Velocity == 127)
                                    {
                                        ((DrumPad*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                    }
                                    else if (note.Velocity == 1)
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
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.Value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
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
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        var ev = str.GetString(Encoding.ASCII);
                        instrumentTrack.Events
                            .GetLastOrAppend(position)
                            .Add(ev);
                    }
                }
            }
            return instrumentTrack;
        }

        public static unsafe BasicInstrumentTrack2<DrumNote2<UnknownLane>> LoadUnknownDrums(YARGMidiTrack midiTrack, SyncTrack2 sync, ref DrumsType drumsType)
        {
            var instrumentTrack = new BasicInstrumentTrack2<DrumNote2<UnknownLane>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<DrumNote2<UnknownLane>>();
            }

            int NUM_LANES = (drumsType & DrumsType.FiveLane) == DrumsType.FiveLane ? 7 : 6;
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

            int tempoIndex = 0;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(MidiStats);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        if (lastOnNote.Ticks + MidiLoader_Constants.NOTE_SNAP_THRESHOLD > position.Ticks)
                        {
                            position = lastOnNote;
                        }
                        else
                        {
                            lastOnNote = position;
                        }

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                                        drumsType = DrumsType.FiveLane;
                                    }

                                    if (enableDynamics)
                                    {
                                        if (note.Velocity == 127)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Accent;
                                        }
                                        else if (note.Velocity == 1)
                                        {
                                            ((DrumPad_Pro*) &drum->Pads)[padIndex].Dynamics = DrumDynamics.Ghost;
                                        }
                                    }

                                    if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                                    {
                                        if (1 <= padIndex && padIndex <= 3)
                                        {
                                            ((DrumPad_Pro*)&drum->Pads)[padIndex].CymbalFlag = !tomFlags[padIndex - 1];
                                        }
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
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.Value - TOM_MIN_VALUE;
                                tomFlags[index] = true;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    var diffTrack = instrumentTrack[i]!;
                                    if (diffTrack.Notes.TryGetLastValue(position, out var drum))
                                    {
                                        ((DrumPad_Pro*)&drum->Pads)[index + 1].CymbalFlag = true;
                                    }
                                }
                                drumsType = DrumsType.ProDrums;
                                NUM_LANES = 6;
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.Value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
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
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.Value - TOM_MIN_VALUE;
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
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                var duration = position - bre;
                                instrumentTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(bre)
                                        .Add(SpecialPhraseType.BRE, (duration, 100));
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
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
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        var ev = str.GetString(Encoding.ASCII);
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
