using System;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;
using YARG.Core.Logging;

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
        public static unsafe InstrumentTrack2<DifficultyTrack2<FourLaneDrums>> LoadFourLane(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, bool isProDrums)
        {
            // Pre-load empty instances of all difficulties
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<FourLaneDrums>>();
            var difficulties = new DifficultyTrack2<FourLaneDrums>[InstrumentTrack2.NUM_DIFFICULTIES]
            {
                instrumentTrack.Difficulties[0] = instrumentTrack[Difficulty.Easy]   = new DifficultyTrack2<FourLaneDrums>(),
                instrumentTrack.Difficulties[1] = instrumentTrack[Difficulty.Medium] = new DifficultyTrack2<FourLaneDrums>(),
                instrumentTrack.Difficulties[2] = instrumentTrack[Difficulty.Hard]   = new DifficultyTrack2<FourLaneDrums>(),
                instrumentTrack.Difficulties[3] = instrumentTrack[Difficulty.Expert] = new DifficultyTrack2<FourLaneDrums>(),
            };

            const int NUM_LANES = 5;
            // Per-difficulty tracker of note positions
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            // By default, all non-kick notes in a four lane track are cymbals.
            // While a player may choose non-pro drums, the conversion to that state should happen after parsing.
            // The only exception is when the .ini attached the song explicitly defines the file as tom-only.
            var cymbalFlags = stackalloc bool[3] { isProDrums, isProDrums, isProDrums };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = default(DualTime);
            var note = default(MidiNote);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (midiTrack.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (midiTrack.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        if (chordSnapper.Snap(ref position) && midiTrack.Position > 0)
                        {
                            YargLogger.LogInfo("Snap occured");
                        }
                        
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = difficulties[diffIndex];
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                // Kicks don't use dynamics... I hope
                                if (lane >= SNARE_LANE)
                                {
                                    if (enableDynamics)
                                    {
                                        // The FourLaneDrums type lays all the dynamics adjacent to each other.
                                        // Having the pointer to an instance thus allows us to use arithmetic on the location
                                        // of the first dynamic enum variable to get the specific one we want.
                                        (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.velocity switch
                                        {
                                            127 => DrumDynamics.Accent,
                                            1 => DrumDynamics.Ghost,
                                            _ => DrumDynamics.None,
                                        };
                                    }

                                    // Yellow, Blue, and Green can be cymbals
                                    if (lane >= YELLOW_LANE)
                                    {
                                        // Same idea as the drum dynamics above.
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
                                difficulties[3].Notes.TryAppend(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (!isProDrums)
                            {
                                // But we ignore them because the .ini told us to
                                continue;
                            }

                            int index = note.value - TOM_MIN_VALUE;
                            cymbalFlags[index] = false;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                // If a flag flips on the same tick of any notes,
                                // we MUST flip the applicable cymbal marker for those notes to match
                                if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                {
                                    // Blah blah: pointer arithmetic
                                    (&drum->Cymbal_Yellow)[index] = false;
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    soloPosition = position;
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    // The FourLaneDrums type lays all the inner lanes adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the Bass lane variable to get the specific one we want.
                                    (&difficulties[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition)->Bass)[lane] = position - colorPosition;
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
                                    var drum = difficulties[3].Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (!isProDrums)
                            {
                                // But we ignore them because the .ini told us to
                                continue;
                            }

                            int index = note.value - TOM_MIN_VALUE;
                            cymbalFlags[index] = true;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                // If a flag flips on the same tick of any notes,
                                // we MUST flip the applicable cymbal marker for those notes to match
                                if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                {
                                    // Blah blah: pointer arithmetic
                                    (&drum->Cymbal_Yellow)[index] = true;
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                instrumentTrack.Phrases.BREs.Append(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Overdrives.Append(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Soloes.Append(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Tremolos.Append(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Trills.Append(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    // We only need to check dynamics once
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        // Unless, for some stupid-ass reason, this track contains lyrics,
                        // all actually useful events will utilize ASCII encoding for state
                        var ev = Encoding.ASCII.GetString(str);
                        instrumentTrack.Events
                            .GetLastOrAppend(position)
                            .Add(ev);
                    }
                }
            }
            return instrumentTrack;
        }

        public static unsafe InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>> LoadFiveLane(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker)
        {
            // Pre-load empty instances of all difficulties
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<FiveLaneDrums>>();
            var difficulties = new DifficultyTrack2<FiveLaneDrums>[InstrumentTrack2.NUM_DIFFICULTIES]
            {
                instrumentTrack.Difficulties[0] = instrumentTrack[Difficulty.Easy]   = new DifficultyTrack2<FiveLaneDrums>(),
                instrumentTrack.Difficulties[1] = instrumentTrack[Difficulty.Medium] = new DifficultyTrack2<FiveLaneDrums>(),
                instrumentTrack.Difficulties[2] = instrumentTrack[Difficulty.Hard]   = new DifficultyTrack2<FiveLaneDrums>(),
                instrumentTrack.Difficulties[3] = instrumentTrack[Difficulty.Expert] = new DifficultyTrack2<FiveLaneDrums>(),
            };

            const int NUM_LANES = 6;
            // Per-difficulty tracker of note positions
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = default(DualTime);
            var note = default(MidiNote);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (midiTrack.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (midiTrack.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        if (chordSnapper.Snap(ref position) && midiTrack.Position > 0)
                        {
                            YargLogger.LogInfo("Snap occured");
                        }

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = difficulties[diffIndex];
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                // Kicks don't use dynamics... I hope
                                if (enableDynamics && lane >= SNARE_LANE)
                                {
                                    // The FiveLaneDrums type lays all the dynamics adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the first dynamic enum variable to get the specific one we want.
                                    (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.velocity switch
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
                                difficulties[3].Notes.TryAppend(in position);
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    soloPosition = position;
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    // The FiveLaneDrums type lays all the inner lanes adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the Bass lane variable to get the specific one we want.
                                    (&difficulties[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition)->Bass)[lane] = position - colorPosition;
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
                                    var drum = difficulties[3].Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                instrumentTrack.Phrases.BREs.Append(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Overdrives.Append(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Soloes.Append(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Tremolos.Append(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Trills.Append(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    // We only need to check dynamics once
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        // Unless, for some stupid-ass reason, this track contains lyrics,
                        // all actually useful events will utilize ASCII encoding for state
                        var ev = Encoding.ASCII.GetString(str);
                        instrumentTrack.Events
                            .GetLastOrAppend(position)
                            .Add(ev);
                    }
                }
            }
            return instrumentTrack;
        }

        public static unsafe InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>> LoadUnknownDrums(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, ref DrumsType type)
        {
            // Pre-load empty instances of all difficulties
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<UnknownLaneDrums>>();
            var difficulties = new DifficultyTrack2<UnknownLaneDrums>[InstrumentTrack2.NUM_DIFFICULTIES]
            {
                instrumentTrack.Difficulties[0] = instrumentTrack[Difficulty.Easy]   = new DifficultyTrack2<UnknownLaneDrums>(),
                instrumentTrack.Difficulties[1] = instrumentTrack[Difficulty.Medium] = new DifficultyTrack2<UnknownLaneDrums>(),
                instrumentTrack.Difficulties[2] = instrumentTrack[Difficulty.Hard]   = new DifficultyTrack2<UnknownLaneDrums>(),
                instrumentTrack.Difficulties[3] = instrumentTrack[Difficulty.Expert] = new DifficultyTrack2<UnknownLaneDrums>(),
            };

            const int MAX_LANES = 6;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * MAX_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            // Unknown_Four cannot become five lane
            int numLanes = type != DrumsType.Unknown_Four ? 6 : 5;
            // Flips to true if it finds the text string for enabling dynamics
            bool enableDynamics = false;
            bool flamFlag = false;
            // By default, all non-kick notes are cymbals (unless the track maps to five lane).
            var cymbalFlags = stackalloc bool[3] { true, true, true };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = default(DualTime);
            var note = default(MidiNote);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (midiTrack.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (midiTrack.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        if (chordSnapper.Snap(ref position) && midiTrack.Position > 0)
                        {
                            YargLogger.LogInfo("Snap occured");
                        }

                        if (MidiLoader_Constants.DEFAULT_MIN <= note.value && note.value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            // if we detect prodrums flags or if the type is Unknown_Four, this value will disallow the fifth pad lane
                            if (lane < numLanes)
                            {
                                var diffTrack = difficulties[diffIndex];
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (lane < FIFTH_LANE)
                                {
                                    // Kicks don't use dynamics... I hope
                                    if (lane >= SNARE_LANE)
                                    {
                                        if (enableDynamics)
                                        {
                                            // The UnknownLaneDrums type lays the dynamics of the first four lanes adjacent to each other.
                                            // Having the pointer to an instance thus allows us to use arithmetic on the location
                                            // of the first dynamic enum variable to get the specific one we want.
                                            (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.velocity switch
                                            {
                                                127 => DrumDynamics.Accent,
                                                1 => DrumDynamics.Ghost,
                                                _ => DrumDynamics.None,
                                            };
                                        }

                                        // Yellow, Blue, and Green can be cymbals
                                        if (lane >= YELLOW_LANE)
                                        {
                                            // Same idea as the drum dynamics above.
                                            int cymbalIndex = lane - YELLOW_LANE;
                                            (&drum->Cymbal_Yellow)[cymbalIndex] = cymbalFlags[cymbalIndex];
                                        }
                                    }
                                }
                                else
                                {
                                    type = DrumsType.FiveLane;
                                    if (enableDynamics)
                                    {
                                        drum->Dynamics_Green = note.velocity switch
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
                                difficulties[3].Notes.TryAppend(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (type != DrumsType.FiveLane)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                cymbalFlags[index] = false;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    // If a flag flips on the same tick of any notes,
                                    // we MUST flip the applicable cymbal marker for those notes to match
                                    if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        // Blah blah: pointer arithmetic
                                        (&drum->Cymbal_Yellow)[index] = false;
                                    }
                                }
                                type = DrumsType.ProDrums;
                                numLanes = 5;
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    soloPosition = position;
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = true;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
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
                            if (lane < numLanes)
                            {
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = difficulties[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition);
                                    var duration = position - colorPosition;
                                    if (lane < FIFTH_LANE)
                                    {
                                        // The UnknownLaneDrums type lays kick and the first four pad lanes adjacent to each other.
                                        // Having the pointer to an instance thus allows us to use arithmetic on the location
                                        // of the Bass lane variable to get the specific one we want.
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
                                    var drum = difficulties[3].Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if (type != DrumsType.FiveLane)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                cymbalFlags[index] = true;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    // If a flag flips on the same tick of any notes,
                                    // we MUST flip the applicable cymbal marker for those notes to match
                                    if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        // Blah blah: pointer arithmetic
                                        (&drum->Cymbal_Yellow)[index] = true;
                                    }
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.value && note.value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                instrumentTrack.Phrases.BREs.Append(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Overdrives.Append(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Soloes.Append(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Tremolos.Append(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Trills.Append(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (difficulties[i].Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    // We only need to check dynamics once
                    if (!enableDynamics && str.SequenceEqual(DYNAMICS_STRING))
                    {
                        enableDynamics = true;
                    }
                    else
                    {
                        // Unless, for some stupid-ass reason, this track contains lyrics,
                        // all actually useful events will utilize ASCII encoding for state
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
