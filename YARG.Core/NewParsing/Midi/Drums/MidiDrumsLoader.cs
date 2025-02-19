using System.Text;
using YARG.Core.Chart;
using YARG.Core.Containers;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiDrumsLoader
    {
        private static readonly byte[] DYNAMICS_STRING = Encoding.ASCII.GetBytes("[ENABLE_CHART_DYNAMICS]");
        private const int DOUBLEBASS_VALUE = 95;
        private const int EXPERT_INDEX = 3;
        private const int KICK_LANE = 0;
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
        public static unsafe void Load(YARGMidiTrack midiTrack, InstrumentTrack2<FourLaneDrums> instrumentTrack, ref TempoTracker tempoTracker, bool isProDrums)
        {
            if (!instrumentTrack.IsEmpty())
            {
                return;
            }

            using var overdrives = new YARGNativeSortedList<DualTime, DualTime>();
            using var soloes = new YARGNativeSortedList<DualTime, DualTime>();
            using var trills = new YARGNativeSortedList<DualTime, DualTime>();
            using var tremolos = new YARGNativeSortedList<DualTime, DualTime>();
            using var bres = new YARGNativeSortedList<DualTime, DualTime>();

            const int NUM_LANES = 6;
            const int DOUBLEKICK_LANE = 5;
            // Per-difficulty tracker of note positions
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                // Kick             Snare             Yellow             Blue               Green              Double Kick
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            bool enableDynamics = false;
            bool flamFlag = false;
            // Some authors will format their charts so that Expert & Expert+ kicks are entirely separate difficulty-wise, where non-double kicks
            // should only show up in Expert, not Expert+.
            // In those situations, kicks that should appear in both will have both note 95 & 96 activated on the same tick.
            // However, the majority of charts will assume that note value 96 applies to both X & X+, thus only writing note 96.
            // This boolean flag determines whether we need to perform the X -> X/X+ conversion on those notes.
            // It will be flipped to false if the chart contains 95 & 96 together.
            bool convertExpectKicksToShared = true;

            // By default, all non-kick notes in a four lane track are cymbals.
            // While a player may choose non-pro drums, the conversion to that state should happen after parsing.
            // The only exception is when the .ini attached the song explicitly defines the file as tom-only.
            var cymbalFlags = stackalloc bool[3] { isProDrums, isProDrums, isProDrums };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5] { DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive };
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = DualTime.Zero;
            var note = default(MidiNote);
            var stats = default(MidiStats);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < DOUBLEKICK_LANE)
                            {
                                var diffTrack = instrumentTrack[diffIndex];
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do not exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAdd(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                lanes[diffIndex * NUM_LANES + lane] = position;

                                // Kicks don't use dynamics... I hope
                                if (SNARE_LANE <= lane)
                                {
                                    if (enableDynamics)
                                    {
                                        // The FourLaneDrums type lays all the dynamics adjacent to each other.
                                        // Having the pointer to an instance thus allows us to use arithmetic on the location
                                        // of the first dynamic enum variable to get the specific one we want.
                                        (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.Velocity switch
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
                                lanes[diffIndex * NUM_LANES + DOUBLEKICK_LANE] = position;
                                instrumentTrack.Expert.Notes.TryAdd(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if (!isProDrums)
                            {
                                // But we ignore them because the .ini told us to
                                continue;
                            }

                            int index = note.Value - TOM_MIN_VALUE;
                            cymbalFlags[index] = false;
                            foreach (var diff in instrumentTrack)
                            {
                                // If a flag flips on the same tick of any notes,
                                // we MUST flip the applicable cymbal marker for those notes to match
                                if (diff.Notes.TryGetLastValue(in position, out var drum))
                                {
                                    // Blah blah: pointer arithmetic
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
                            switch (note.Value)
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
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
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
                                    // The FourLaneDrums type lays all the inner lanes adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the Bass lane variable to get the specific one we want.
                                    var drum = instrumentTrack[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition);
                                    (&drum->Kick)[lane] = position - colorPosition;
                                    colorPosition.Ticks = -1;

                                    if (lane == 0)
                                    {
                                        if (drum->KickState == KickState.PlusOnly)
                                        {
                                            convertExpectKicksToShared = false;
                                            drum->KickState = KickState.Shared;
                                        }
                                        else
                                        {
                                            drum->KickState = KickState.NonPlusOnly;
                                        }
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + DOUBLEKICK_LANE];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack.Expert.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Kick = position - colorPosition;
                                    colorPosition.Ticks = -1;

                                    if (drum->KickState == KickState.NonPlusOnly)
                                    {
                                        convertExpectKicksToShared = false;
                                        drum->KickState = KickState.Shared;
                                    }
                                    else
                                    {
                                        drum->KickState = KickState.PlusOnly;
                                    }
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if (!isProDrums)
                            {
                                // But we ignore them because the .ini told us to
                                continue;
                            }

                            int index = note.Value - TOM_MIN_VALUE;
                            cymbalFlags[index] = true;
                            foreach (var diff in instrumentTrack)
                            {
                                // If a flag flips on the same tick of any notes,
                                // we MUST flip the applicable cymbal marker for those notes to match
                                if (diff.Notes.TryGetLastValue(in position, out var drum))
                                {
                                    // Blah blah: pointer arithmetic
                                    (&drum->Cymbal_Yellow)[index] = true;
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                bres.Add(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        soloes.Add(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        tremolos.Add(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        trills.Add(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
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
                        var ev = str.GetString(Encoding.ASCII);
                        instrumentTrack.Events
                            .GetLastOrAdd(position)
                            .Add(ev);
                    }
                }
            }

            if (convertExpectKicksToShared)
            {
                var expertNotes = instrumentTrack.Expert.Notes;
                for (int i = 0; i < expertNotes.Count; ++i)
                {
                    ref var drum = ref expertNotes[i].Value;
                    if (drum.KickState == KickState.NonPlusOnly)
                    {
                        drum.KickState = KickState.Shared;
                    }
                }
            }

            foreach (var diff in instrumentTrack)
            {
                diff.Overdrives.CopyFrom(overdrives);
                diff.Soloes.CopyFrom(soloes);
                diff.BREs.CopyFrom(bres);
                diff.Tremolos.CopyFrom(tremolos);
                diff.Trills.CopyFrom(trills);
            }
        }

        public static unsafe void Load(YARGMidiTrack midiTrack, InstrumentTrack2<FiveLaneDrums> instrumentTrack, ref TempoTracker tempoTracker)
        {
            if (!instrumentTrack.IsEmpty())
            {
                return;
            }

            using var overdrives = new YARGNativeSortedList<DualTime, DualTime>();
            using var soloes = new YARGNativeSortedList<DualTime, DualTime>();
            using var trills = new YARGNativeSortedList<DualTime, DualTime>();
            using var tremolos = new YARGNativeSortedList<DualTime, DualTime>();
            using var bres = new YARGNativeSortedList<DualTime, DualTime>();

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
            // Some authors will format their charts so that Expert & Expert+ kicks are entirely separate difficulty-wise, where non-double kicks
            // should only show up in Expert, not Expert+.
            // In those situations, kicks that should appear in both will have both note 95 & 96 activated on the same tick.
            // However, the majority of charts will assume that note value 96 applies to both X & X+, thus only writing note 96.
            // This boolean flag determines whether we need to perform the X -> X/X+ conversion on those notes.
            // It will be flipped to false if the chart contains 95 & 96 together.
            bool convertExpectKicksToShared = true;

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5] { DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive };
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = DualTime.Zero;
            var note = default(MidiNote);
            var stats = default(MidiStats);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                var diffTrack = instrumentTrack[diffIndex];
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do not exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAdd(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                // Kicks don't use dynamics... I hope
                                if (enableDynamics && lane >= SNARE_LANE)
                                {
                                    // The FiveLaneDrums type lays all the dynamics adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the first dynamic enum variable to get the specific one we want.
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
                                lanes[diffIndex * NUM_LANES + KICK_LANE] = position;
                                instrumentTrack.Expert.Notes.TryAdd(in position);
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.Value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            switch (note.Value)
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
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
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
                                    // The FiveLaneDrums type lays all the inner lanes adjacent to each other.
                                    // Having the pointer to an instance thus allows us to use arithmetic on the location
                                    // of the Bass lane variable to get the specific one we want.
                                    var drum = instrumentTrack[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition);
                                    (&drum->Kick)[lane] = position - colorPosition;
                                    colorPosition.Ticks = -1;

                                    if (lane == 0)
                                    {
                                        if (drum->KickState == KickState.PlusOnly)
                                        {
                                            convertExpectKicksToShared = false;
                                            drum->KickState = KickState.Shared;
                                        }
                                        else
                                        {
                                            drum->KickState = KickState.NonPlusOnly;
                                        }
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
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + KICK_LANE];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack.Expert.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Kick = position - colorPosition;
                                    colorPosition.Ticks = -1;

                                    if (drum->KickState == KickState.NonPlusOnly)
                                    {
                                        convertExpectKicksToShared = false;
                                        drum->KickState = KickState.Shared;
                                    }
                                    else
                                    {
                                        drum->KickState = KickState.PlusOnly;
                                    }
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                bres.Add(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        soloes.Add(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        tremolos.Add(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        trills.Add(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
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
                        var ev = str.GetString(Encoding.ASCII);
                        instrumentTrack.Events
                            .GetLastOrAdd(position)
                            .Add(ev);
                    }
                }
            }

            if (convertExpectKicksToShared)
            {
                var expertNotes = instrumentTrack.Expert.Notes;
                for (int i = 0; i < expertNotes.Count; ++i)
                {
                    ref var drum = ref expertNotes[i].Value;
                    if (drum.KickState == KickState.NonPlusOnly)
                    {
                        drum.KickState = KickState.Shared;
                    }
                }
            }

            foreach (var diff in instrumentTrack)
            {
                diff.Overdrives.CopyFrom(overdrives);
                diff.Soloes.CopyFrom(soloes);
                diff.BREs.CopyFrom(bres);
                diff.Tremolos.CopyFrom(tremolos);
                diff.Trills.CopyFrom(trills);
            }
        }

        public static unsafe InstrumentTrack2<UnknownLaneDrums> LoadUnknownDrums(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, ref DrumsType drumsType)
        {
            var instrumentTrack = new InstrumentTrack2<UnknownLaneDrums>();
            using var overdrives = new YARGNativeSortedList<DualTime, DualTime>();
            using var soloes = new YARGNativeSortedList<DualTime, DualTime>();
            using var trills = new YARGNativeSortedList<DualTime, DualTime>();
            using var tremolos = new YARGNativeSortedList<DualTime, DualTime>();
            using var bres = new YARGNativeSortedList<DualTime, DualTime>();

            const int MAX_LANES = 6;
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * MAX_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            // By design, we only call this function only if five lane is possible.
            // Therefore, we have allow that lane by default.
            int numLanes = 6;
            // Flips to true if it finds the text string for enabling dynamics
            bool enableDynamics = false;
            bool flamFlag = false;
            // Some authors will format their charts so that Expert & Expert+ kicks are entirely separate difficulty-wise, where non-double kicks
            // should only show up in Expert, not Expert+.
            // In those situations, kicks that should appear in both will have both note 95 & 96 activated on the same tick.
            // However, the majority of charts will assume that note value 96 applies to both X & X+, thus only writing note 96.
            // This boolean flag determines whether we need to perform the X -> X/X+ conversion on those notes.
            // It will be flipped to false if the chart contains 95 & 96 together.
            bool convertExpectKicksToShared = true;
            // By default, all non-kick notes are cymbals (unless the track maps to five lane).
            var cymbalFlags = drumsType.Has(DrumsType.ProDrums)
                ? stackalloc bool[3] { true, true, true }
                : stackalloc bool[3] { false, false, false };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5] { DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive };
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = DualTime.Zero;
            var note = default(MidiNote);
            var stats = default(MidiStats);
            // Used for snapping together notes that get accidentally misaligned during authoring
            var chordSnapper = new ChordSnapper();
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Only noteOn events with non-zero velocities actually count as "ON"
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (MidiLoader_Constants.DEFAULT_MIN <= note.Value && note.Value <= DRUMNOTE_MAX)
                        {
                            int noteValue = note.Value - MidiLoader_Constants.DEFAULT_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            int lane = LANEVALUES[noteValue];
                            // if we detect prodrums flags, this value changes to disallow the fifth pad lane
                            if (lane < numLanes)
                            {
                                var diffTrack = instrumentTrack[diffIndex];
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do not exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the flam flag when we add a new note.
                                // Any changes to the flag after this point will automatically occur from a separate scope.
                                if (diffTrack.Notes.GetLastOrAdd(in position, out var drum))
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
                                            (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.Velocity switch
                                            {
                                                127 => DrumDynamics.Accent,
                                                1 => DrumDynamics.Ghost,
                                                _ => DrumDynamics.None,
                                            };
                                        }

                                        // Only Yellow, Blue, and Green can be cymbals
                                        if (drumsType.Has(DrumsType.ProDrums) && lane >= YELLOW_LANE)
                                        {
                                            // Same idea as the drum dynamics above.
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
                                lanes[diffIndex * MAX_LANES + KICK_LANE] = position;
                                instrumentTrack.Expert.Notes.TryAdd(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if (drumsType.Has(DrumsType.ProDrums))
                            {
                                int index = note.Value - TOM_MIN_VALUE;
                                cymbalFlags[index] = false;
                                foreach (var diff in instrumentTrack)
                                {
                                    // If a flag flips on the same tick of any notes,
                                    // we MUST flip the applicable cymbal marker for those notes to match
                                    if (diff.Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        // Blah blah: pointer arithmetic
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
                            switch (note.Value)
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
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
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
                                    var drum = instrumentTrack[diffIndex].Notes.TraverseBackwardsUntil(in colorPosition);
                                    var duration = position - colorPosition;
                                    if (lane < FIFTH_LANE)
                                    {
                                        // The UnknownLaneDrums type lays kick and the first four pad lanes adjacent to each other.
                                        // Having the pointer to an instance thus allows us to use arithmetic on the location
                                        // of the Bass lane variable to get the specific one we want.
                                        (&drum->Kick)[lane] = duration;

                                        if (lane == 0)
                                        {
                                            if (drum->KickState == KickState.PlusOnly)
                                            {
                                                convertExpectKicksToShared = false;
                                                drum->KickState = KickState.Shared;
                                            }
                                            else
                                            {
                                                drum->KickState = KickState.NonPlusOnly;
                                            }
                                        }
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
                                ref var colorPosition = ref lanes[diffIndex * MAX_LANES + KICK_LANE];
                                if (colorPosition.Ticks != -1)
                                {
                                    var drum = instrumentTrack.Expert.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Kick = position - colorPosition;
                                    colorPosition.Ticks = -1;

                                    if (drum->KickState == KickState.NonPlusOnly)
                                    {
                                        convertExpectKicksToShared = false;
                                        drum->KickState = KickState.Shared;
                                    }
                                    else
                                    {
                                        drum->KickState = KickState.PlusOnly;
                                    }
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.Value && note.Value <= TOM_MAX_VALUE)
                        {
                            if (drumsType.Has(DrumsType.ProDrums))
                            {
                                int index = note.Value - TOM_MIN_VALUE;
                                cymbalFlags[index] = true;
                                foreach (var diff in instrumentTrack)
                                {
                                    // If a flag flips on the same tick of any notes,
                                    // we MUST flip the applicable cymbal marker for those notes to match
                                    if (diff.Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        // Blah blah: pointer arithmetic
                                        (&drum->Cymbal_Yellow)[index] = true;
                                    }
                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.Value - MidiLoader_Constants.BRE_MIN];
                            // We only want to add a BRE phrase if we can confirm that all the BRE lanes
                            // were set to "ON" on the same tick
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4])
                            {
                                bres.Add(in bre, position - bre);
                            }
                            bre.Ticks = -1;
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.SOLO:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        soloes.Add(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        tremolos.Add(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        trills.Add(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                                case FLAM_VALUE:
                                    flamFlag = false;
                                    foreach (var diff in instrumentTrack)
                                    {
                                        // If a flag flips on the same tick of any notes,
                                        // we MUST flip the applicable flam marker for those notes to match
                                        if (diff.Notes.TryGetLastValue(in position, out var drum))
                                        {
                                            drum->IsFlammed = false;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
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
                        var ev = str.GetString(Encoding.ASCII);
                        instrumentTrack.Events
                            .GetLastOrAdd(position)
                            .Add(ev);
                    }
                }
            }

            if (convertExpectKicksToShared)
            {
                var expertNotes = instrumentTrack.Expert.Notes;
                for (int i = 0; i < expertNotes.Count; ++i)
                {
                    ref var drum = ref expertNotes[i].Value;
                    if (drum.KickState == KickState.NonPlusOnly)
                    {
                        drum.KickState = KickState.Shared;
                    }
                }
            }

            foreach (var diff in instrumentTrack)
            {
                diff.Overdrives.CopyFrom(overdrives);
                diff.Soloes.CopyFrom(soloes);
                diff.BREs.CopyFrom(bres);
                diff.Tremolos.CopyFrom(tremolos);
                diff.Trills.CopyFrom(trills);
            }
            return instrumentTrack;
        }
    }
}
