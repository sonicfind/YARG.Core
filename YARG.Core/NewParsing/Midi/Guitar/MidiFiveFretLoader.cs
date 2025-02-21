using System;
using System.Text;
using YARG.Core.Containers;
using YARG.Core.IO;

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
        private const int OVERDIVE_DIFFICULTY_INDEX = 12;

        private static readonly byte[] SYSEXTAG = { (byte) 'P', (byte) 'S', (byte) '\0', };
        private const int SYSEX_LENGTH = 8;
        private const int SYSEX_DIFF_INDEX = 4;
        private const int SYSEX_TYPE_INDEX = 5;
        private const int SYSEX_STATUS_INDEX = 6;
        private const int SYSEX_OPEN_TYPE = 1;
        private const int SYSEX_TAP_TYPE = 4;
        private const int SYSEX_ALLDIFF = 0xFF;

        private static readonly byte[][] ENHANCED_STRINGS = new byte[][] { Encoding.ASCII.GetBytes("[ENHANCED_OPENS]"), Encoding.ASCII.GetBytes("ENHANCED_OPENS") };

        public static unsafe void Load(YARGMidiTrack midiTrack, InstrumentTrack2<FiveFretGuitar> instrumentTrack, ref TempoTracker tempoTracker)
        {
            if (!instrumentTrack.IsEmpty())
            {
                return;
            }

            using var overdrives = new YargNativeSortedList<DualTime, DualTime>();
            using var soloes = new YargNativeSortedList<DualTime, DualTime>();
            using var trills = new YargNativeSortedList<DualTime, DualTime>();
            using var tremolos = new YargNativeSortedList<DualTime, DualTime>();
            using var bres = new YargNativeSortedList<DualTime, DualTime>();
            using var faceoff_P1 = new YargNativeSortedList<DualTime, DualTime>();
            using var faceoff_P2 = new YargNativeSortedList<DualTime, DualTime>();

            var diffModifiers = stackalloc (bool SliderNotes, bool HopoOn, bool HopoOff)[InstrumentTrack2.NUM_DIFFICULTIES];

            // Zero is reserved for open notes. Open notes apply in two situations:
            // 1. The 13s will swap to zeroes when if find the ENHANCED_OPENS event
            // 2. The '1'(green) in a difficulty will swap to zero and back depending on the Open note sysex state
            //
            // Note: the 13s account for the -1 offset of the minimum note value
            //
            // Note 2: If we're using the alternate overdrive note, then the file is most likely tailored for GH1/2.
            // Therefore, we need to alter the index meant for soloes to the difficulty-based SP index (8->12)
            var laneIndices = stackalloc int[InstrumentTrack2.NUM_DIFFICULTIES * MidiLoader_Constants.NOTES_PER_DIFFICULTY]
            {
                13, 1, 2, 3, 4, 5, HOPO_ON_INDEX, HOPO_OFF_INDEX, (_useAlternateOverdrive ? OVERDIVE_DIFFICULTY_INDEX : SP_SOLO_INDEX), TAP_INDEX, FACEOFF_1_INDEX, FACEOFF_2_INDEX,
                13, 1, 2, 3, 4, 5, HOPO_ON_INDEX, HOPO_OFF_INDEX, (_useAlternateOverdrive ? OVERDIVE_DIFFICULTY_INDEX : SP_SOLO_INDEX), TAP_INDEX, FACEOFF_1_INDEX, FACEOFF_2_INDEX,
                13, 1, 2, 3, 4, 5, HOPO_ON_INDEX, HOPO_OFF_INDEX, (_useAlternateOverdrive ? OVERDIVE_DIFFICULTY_INDEX : SP_SOLO_INDEX), TAP_INDEX, FACEOFF_1_INDEX, FACEOFF_2_INDEX,
                13, 1, 2, 3, 4, 5, HOPO_ON_INDEX, HOPO_OFF_INDEX, (_useAlternateOverdrive ? OVERDIVE_DIFFICULTY_INDEX : SP_SOLO_INDEX), TAP_INDEX, FACEOFF_1_INDEX, FACEOFF_2_INDEX,
            };

            // Per-difficulty tracker of note positions
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            // Various special phrases trackers
            var brePositions = stackalloc DualTime[5] { DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, };
            var overdriveTrackPosition = DualTime.Inactive;
            var overdriveDiffPositions = stackalloc DualTime[4] { DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, };
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            var FaceOffPosition_1 = DualTime.Inactive;
            var FaceOffPosition_2 = DualTime.Inactive;

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
                        if (FIVEFRET_MIN <= note.Value && note.Value <= FIVEFRET_MAX)
                        {
                            int noteValue = note.Value - FIVEFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            int lane = laneIndices[noteValue];
                            if (lane < NUM_LANES)
                            {
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    // We do this on the commonality that most charts do not exceed this number of notes.
                                    // Helps keep reallocations to a minimum.
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                // We only need to touch the guitar state when we add a new note.
                                // Any changes to the state afterwards will only occur if one of the applicable
                                // flags undergoes a flip on the same tick, but solely within that scope.
                                if (diffTrack.Notes.GetLastOrAdd(in position, out var guitar))
                                {
                                    ref var diffModifier = ref diffModifiers[diffIndex];
                                    // Hierarchy: Tap > Hopo > Strum > Natural
                                    if (diffModifier.SliderNotes)
                                    {
                                        guitar->State = GuitarState.Tap;
                                    }
                                    else if (diffModifier.HopoOn)
                                    {
                                        guitar->State = GuitarState.Hopo;
                                    }
                                    else if (diffModifier.HopoOff)
                                    {
                                        guitar->State = GuitarState.Strum;
                                    }
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifiers[diffIndex].HopoOn = true;
                                            // We must alter any notes present on the same tick to match the correct state
                                            //
                                            // However, Tap, alone, overrides Hopo
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar) && guitar->State != GuitarState.Tap)
                                            {
                                                guitar->State = GuitarState.Hopo;
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifiers[diffIndex].HopoOff = true;
                                            // We must alter any notes present on the same tick to match the correct state
                                            //
                                            // However, Tap & Hopo override Strum
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar) && guitar->State == GuitarState.Natural)
                                            {
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
                                            soloPosition = position;
                                            break;
                                        }

                                        // If the difficulty is anything other than expert, then this means that the file uses GH1 or GH2
                                        // difficulty-based star power phrases. So we need to...

                                        // 1. convert all prior added solo phrases to expert star power phrases,
                                        instrumentTrack.Expert.Overdrives.MoveFrom(soloes);

                                        // 2. remove and disallow any track-wise star power phrases (as 116 becomes invalid),
                                        overdrives.Clear();
                                        _useAlternateOverdrive = true;

                                        // 3. map Index 8 for soloes to index 12 for difficulty-based star power,
                                        laneIndices[SP_SOLO_INDEX] = OVERDIVE_DIFFICULTY_INDEX;
                                        laneIndices[MidiLoader_Constants.NOTES_PER_DIFFICULTY + SP_SOLO_INDEX] = OVERDIVE_DIFFICULTY_INDEX;
                                        laneIndices[2 * MidiLoader_Constants.NOTES_PER_DIFFICULTY + SP_SOLO_INDEX] = OVERDIVE_DIFFICULTY_INDEX;
                                        laneIndices[3 * MidiLoader_Constants.NOTES_PER_DIFFICULTY + SP_SOLO_INDEX] = OVERDIVE_DIFFICULTY_INDEX;

                                        // and 4. set the current star power phrase position.
                                        overdriveDiffPositions[diffIndex] = position;
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
                                                diffModifiers[i].SliderNotes = true;
                                                // If any note exists on the same tick, we must change the state to match
                                                if (instrumentTrack[i].Notes.TryGetLastValue(in position, out var guitar))
                                                {
                                                    guitar->State = GuitarState.Tap;
                                                }
                                            }
                                        }
                                        break;
                                    case FACEOFF_1_INDEX:
                                        FaceOffPosition_1 = position;
                                        break;
                                    case FACEOFF_2_INDEX:
                                        FaceOffPosition_2 = position;
                                        break;
                                    case OVERDIVE_DIFFICULTY_INDEX:
                                        overdriveDiffPositions[diffIndex] = position;
                                        break;

                                }
                            }
                        }
                        else if (MidiLoader_Constants.BRE_MIN <= note.Value && note.Value <= MidiLoader_Constants.BRE_MAX)
                        {
                            brePositions[note.Value - MidiLoader_Constants.BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the fivefret note scope for optimization reasons
                            switch (note.Value)
                            {
                                // If the alternate overdrive value of 103 is in use, then 116 should be vacant
                                case MidiLoader_Constants.OVERDRIVE:
                                    // But... to be safe
                                    if (!_useAlternateOverdrive)
                                    {
                                        overdriveTrackPosition = position;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    tremoloPostion = position;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (FIVEFRET_MIN <= note.Value && note.Value <= FIVEFRET_MAX)
                        {
                            int noteValue = note.Value - FIVEFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            int lane = laneIndices[noteValue];
                            if (lane < NUM_LANES)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    // FiveFretGuitar lays all the lanes adjacent to each other right at the top of the type.
                                    // Having a pointer to an instance therefore equates to having the pointer to the first lane.
                                    // This means we can use simple indexing to grab the lane that we desire
                                    ((DualTime*)diffTrack.Notes.TraverseBackwardsUntil(in colorPosition))[lane] = position - colorPosition;
                                    colorPosition.Ticks = -1;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifiers[diffIndex].HopoOn = false;
                                            // We must alter any notes present on the same tick to match the correct state
                                            //
                                            // However, Tap overrides all others
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar) && guitar->State != GuitarState.Tap)
                                            {
                                                guitar->State = diffModifiers[diffIndex].HopoOff ? GuitarState.Strum : GuitarState.Natural;
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifiers[diffIndex].HopoOff = false;
                                            // We must alter any notes present on the same tick to match the correct state
                                            //
                                            // However, Tap & Hopo override Strum
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar) && guitar->State == GuitarState.Strum)
                                            {
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
                                            if (soloPosition.Ticks > -1)
                                            {
                                                soloes.Add(in soloPosition, position - soloPosition);
                                                soloPosition.Ticks = -1;
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
                                                ref var diffModifier = ref diffModifiers[i];
                                                diffModifier.SliderNotes = false;
                                                // If any note exists on the same tick, we must change the state to match
                                                // From state heirarchy rules, the state for a found note IS already set to Tap.
                                                // We don't need to check.
                                                if (instrumentTrack[i].Notes.TryGetLastValue(in position, out var guitar))
                                                {
                                                    if (diffModifier.HopoOn)
                                                    {
                                                        guitar->State = GuitarState.Hopo;
                                                    }
                                                    else if (diffModifier.HopoOff)
                                                    {
                                                        guitar->State = GuitarState.Strum;
                                                    }
                                                    else
                                                    {
                                                        guitar->State = GuitarState.Natural;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case FACEOFF_1_INDEX:
                                        if (FaceOffPosition_1.Ticks > -1)
                                        {
                                            faceoff_P1.Add(in FaceOffPosition_1, position - FaceOffPosition_1);
                                            FaceOffPosition_1.Ticks = -1;
                                        }
                                        break;
                                    case FACEOFF_2_INDEX:
                                        if (FaceOffPosition_2.Ticks > -1)
                                        {
                                            faceoff_P2.Add(in FaceOffPosition_2, position - FaceOffPosition_2);
                                            FaceOffPosition_2.Ticks = -1;
                                        }
                                        break;
                                    case OVERDIVE_DIFFICULTY_INDEX:
                                        {
                                            ref var diffOverdrivePosition = ref overdriveDiffPositions[diffIndex];
                                            if (diffOverdrivePosition.Ticks > -1)
                                            {
                                                instrumentTrack[diffIndex].Overdrives.Add(in diffOverdrivePosition, position - diffOverdrivePosition);
                                                diffOverdrivePosition.Ticks = -1;
                                            }
                                            break;
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
                            // Note: Solo phrase is handled in the fivefret note scope for optimization reasons
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdriveTrackPosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdriveTrackPosition, position - overdriveTrackPosition);
                                        overdriveTrackPosition.Ticks = -1;
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

                            }
                        }
                    }
                }
                else if (stats.Type is MidiEventType.SysEx or MidiEventType.SysEx_End)
                {
                    var sysex = midiTrack.ExtractTextOrSysEx();
                    if (sysex.length != 8 || !sysex.SequenceEqual(SYSEXTAG))
                    {
                        continue;
                    }

                    bool enable = sysex[SYSEX_STATUS_INDEX] == 1;
                    if (enable)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        //
                        // While this isn't an actual NoteOn midi event, we should interpret it like one in this instance for safety.
                        chordSnapper.Snap(ref position);
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
                                    diffModifiers[diffIndex].SliderNotes = enable;
                                    // If any note exists on the same tick, we must change the state to match
                                    if (instrumentTrack[diffIndex].Notes.TryGetLastValue(in position, out var guitar))
                                    {
                                        if (enable)
                                        {
                                            guitar->State = GuitarState.Tap;
                                        }
                                        // Disabling past this point
                                        else if (diffModifiers[diffIndex].HopoOn)
                                        {
                                            guitar->State = GuitarState.Hopo;
                                        }
                                        else if (diffModifiers[diffIndex].HopoOff)
                                        {
                                            guitar->State = GuitarState.Strum;
                                        }
                                        else
                                        {
                                            guitar->State = GuitarState.Natural;
                                        }
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
                                //                                                                      1 - Green; 0 - Open
                                laneIndices[MidiLoader_Constants.NOTES_PER_DIFFICULTY * diffIndex + 1] = !enable ? 1 : 0;
                                break;
                            case SYSEX_OPEN_TYPE:
                                {
                                    diffModifiers[diffIndex].SliderNotes = enable;
                                    // If any note exists on the same tick, we must change the state to match
                                    if (instrumentTrack[diffIndex].Notes.TryGetLastValue(in position, out var guitar))
                                    {
                                        if (enable)
                                        {
                                            guitar->State = GuitarState.Tap;
                                        }
                                        else if (guitar->State == GuitarState.Tap)
                                        {
                                            if (diffModifiers[diffIndex].HopoOn)
                                                guitar->State = GuitarState.Hopo;
                                            else if (diffModifiers[diffIndex].HopoOff)
                                                guitar->State = GuitarState.Strum;
                                            else
                                                guitar->State = GuitarState.Natural;
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
                    // If we encounter the open note tag, we have to flip the `13` indices to zero so that are accepted
                    // as valid within the FiveFret midiNote code scopes
                    if (laneIndices[0] == 13 && (str.SequenceEqual(ENHANCED_STRINGS[0]) || str.SequenceEqual(ENHANCED_STRINGS[1])))
                    {
                        for (int offset = 0;
                            offset < InstrumentTrack2.NUM_DIFFICULTIES * MidiLoader_Constants.NOTES_PER_DIFFICULTY;
                            offset += MidiLoader_Constants.NOTES_PER_DIFFICULTY)
                        {
                            laneIndices[offset] = 0;
                        }
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

            foreach (var diff in instrumentTrack)
            {
                if (!_useAlternateOverdrive)
                {
                    diff.Overdrives.CopyFrom(overdrives);
                }
                diff.Soloes.CopyFrom(soloes);
                diff.BREs.CopyFrom(bres);
                diff.Tremolos.CopyFrom(tremolos);
                diff.Trills.CopyFrom(trills);
                diff.Faceoff_Player1.MoveFrom(faceoff_P1);
                diff.Faceoff_Player2.MoveFrom(faceoff_P2);
            }
        }
    }
}
