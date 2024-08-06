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
        private const int BASS_INDEX = 0;
        private const int EXPERT_INDEX = 3;
        private const int SNARE_LANE = 1;
        private const int YELLOW_LANE = 2;
        private const int FIFTH_LANE = 5;
        private const int FLAM_VALUE = 109;
        private const int DRUMNOTE_MAX = 101;
        private static readonly int[] LANEVALUES = new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
        };

        private const int TOM_MIN_VALUE = 110;
        private const int TOM_MAX_VALUE = 112;
        public static unsafe InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> LoadFourLane(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<FourLaneDrums>();
            }

            const int NUM_LANES = 5;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            var cymbalFlags = stackalloc bool[3] { true, true, true };
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
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.TryAppend(position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (lane >= SNARE_LANE)
                                {
                                    if (enableDynamics)
                                    {
                                        (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.Velocity switch
                                        {
                                            127 => DrumDynamics.Accent,
                                            1 => DrumDynamics.Ghost,
                                            _ => DrumDynamics.None,
                                        };
                                    }

                                    if (lane >= YELLOW_LANE)
                                    {
                                        int index = lane - YELLOW_LANE;
                                        (&drum->Cymbal_Yellow)[index] = cymbalFlags[index];
                                    }
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                lanes[diffIndex * NUM_LANES + BASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.TryAppend(position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            int index = note.Value - TOM_MIN_VALUE;
                            cymbalFlags[index] = false;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
                                {
                                    (&drum->Cymbal_Yellow)[index] = false;
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
                                        if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    (&instrumentTrack[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)->Bass)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + BASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum->Bass = DualTime.Truncate(position - colorPosition);
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            int index = note.Value - TOM_MIN_VALUE;
                            cymbalFlags[index] = true;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
                                {
                                    (&drum->Cymbal_Yellow)[index] = true;
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
                                        if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
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

        public static unsafe InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> LoadFiveLane(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<FiveLaneDrums>();
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
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.TryAppend(position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (enableDynamics && lane >= SNARE_LANE)
                                {
                                    (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.Velocity switch
                                    {
                                        127 => DrumDynamics.Accent,
                                        1 => DrumDynamics.Ghost,
                                        _ => DrumDynamics.None,
                                    };
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                lanes[diffIndex * NUM_LANES + BASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.TryAppend(position);
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
                                        if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    (&instrumentTrack[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition)->Bass)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + BASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum->Bass = DualTime.Truncate(position - colorPosition);
                                    drum->IsDoubleBass = true;
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
                                        if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
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

        public static unsafe InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> LoadUnknownDrums(YARGMidiTrack midiTrack, SyncTrack2 sync, ref DrumsType drumsType)
        {
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<UnknownLaneDrums>();
            }

            const int MAX_LANES = 6;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * MAX_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            int numLanes = 6;
            bool enableDynamics = false;
            bool flamFlag = false;
            var cymbalFlags = stackalloc bool[3] { true, true, true };
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
                            // if we detect prodrums flags, this value changes to disallow the fifth pad lane
                            if (lane < numLanes)
                            {
                                var diffTrack = instrumentTrack[diffIndex]!;
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.TryAppend(position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (lane < FIFTH_LANE)
                                {
                                    if (lane >= SNARE_LANE)
                                    {
                                        if (enableDynamics)
                                        {
                                            (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.Velocity switch
                                            {
                                                127 => DrumDynamics.Accent,
                                                1 => DrumDynamics.Ghost,
                                                _ => DrumDynamics.None,
                                            };
                                        }

                                        if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums && lane >= YELLOW_LANE)
                                        {
                                            int cymbalIndex = lane - YELLOW_LANE;
                                            (&drum->Cymbal_Yellow)[cymbalIndex] = cymbalFlags[cymbalIndex];
                                        }
                                    }
                                }
                                else
                                {
                                    drumsType = DrumsType.FiveLane;
                                    if (enableDynamics)
                                    {
                                        drum->Dynamics_Green = note.Velocity switch
                                        {
                                            127 => DrumDynamics.Accent,
                                            1 => DrumDynamics.Ghost,
                                            _ => DrumDynamics.None,
                                        };
                                    }
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                lanes[diffIndex * MAX_LANES + BASS_INDEX] = position;
                                instrumentTrack[Difficulty.Expert]!.Notes.TryAppend(position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.Value - TOM_MIN_VALUE;
                                cymbalFlags[index] = false;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
                                    {
                                        (&drum->Cymbal_Yellow)[index] = false;
                                    }
                                }
                                drumsType = DrumsType.ProDrums;
                                numLanes = 5;
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
                                        if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
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
                            if (lane < numLanes)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack[diffIndex]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    var duration = DualTime.Truncate(position - colorPosition);
                                    if (lane < FIFTH_LANE)
                                    {
                                        (&drum->Bass)[lane] = duration;
                                    }
                                    else
                                    {
                                        drum->Green = duration;
                                    }
                                    colorPosition.Ticks = -1;
                                }
                            }
                            // DEFAULT_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                            //      60     +     2     *   12                                         +  11
                            // = 95 = Double Kick
                            //
                            // Accessing this value in this manner allows other more common notes to parse in the fastest path
                            else if (lane == 11 && diffIndex == 2)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + BASS_INDEX];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack[Difficulty.Expert]!.Notes.TraverseBackwardsUntil(colorPosition);
                                    drum->Bass = DualTime.Truncate(position - colorPosition);
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.Value - TOM_MIN_VALUE;
                                cymbalFlags[index] = true;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    if (instrumentTrack[i]!.Notes.TryGetLastValue(position, out var drum))
                                    {
                                        (&drum->Cymbal_Yellow)[index] = true;
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
    }
}
