using System;
using System.Text;
using YARG.Core.Containers;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiSixFretLoader
    {
        private const int SIXFRET_MIN = 58;
        private const int SIXFRET_MAX = 104;
        private const int NUM_LANES = 7;
        private const int HOPO_ON_INDEX = 7;
        private const int HOPO_OFF_INDEX = 8;
        private const int SP_SOLO_INDEX = 9;
        private const int TAP_INDEX = 10;

        private const int STARPOWER_DIFF_OFFSET = 8;
        private const int STARPOWER_DIFF_VALUE = 12;

        private static readonly byte[] SYSEXTAG = { (byte) 'P', (byte) 'S', (byte) '\0', };
        private const int SYSEX_LENGTH = 8;
        private const int SYSEX_DIFF_INDEX = 4;
        private const int SYSEX_TYPE_INDEX = 5;
        private const int SYSEX_STATUS_INDEX = 6;
        private const int SYSEX_TAP_TYPE = 4;
        private const int SYSEX_ALLDIFF = 0xFF;

        private static readonly int[] LANEVALUES = new int[] {
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
        };

        public static unsafe void Load(YARGMidiTrack midiTrack, InstrumentTrack2<SixFretGuitar> instrumentTrack, ref TempoTracker tempoTracker)
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

            var diffModifiers = stackalloc (bool SliderNotes, bool HopoOn, bool HopoOff)[InstrumentTrack2.NUM_DIFFICULTIES];
            // Per-difficulty tracker of note positions
            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

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
                // Only noteOn events with non-zero velocities actually count as "ON"
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.Velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (SIXFRET_MIN <= note.Value && note.Value <= SIXFRET_MAX)
                        {
                            int noteValue = note.Value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            int lane = LANEVALUES[noteValue];
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
                                        //      58      +     3     *   12                                         +   9
                                        // = 103 = Solo
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            soloPosition = position;
                                        }
                                        break;
                                    case TAP_INDEX:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      58      +     3     *   12                                         +  10
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
                        if (SIXFRET_MIN <= note.Value && note.Value <= SIXFRET_MAX)
                        {
                            int noteValue = note.Value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex];
                            int lane = LANEVALUES[noteValue];
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
                                        //      58      +     3     *   12                                         +   9
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
                                        //      58      +     3     *   12                                         +   10
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
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
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
                    if (sysex.length != SYSEX_LENGTH || !sysex.SequenceEqual(SYSEXTAG) || sysex[SYSEX_TYPE_INDEX] != SYSEX_TAP_TYPE)
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

                    int diffIndex = sysex[SYSEX_DIFF_INDEX];
                    int max = InstrumentTrack2.NUM_DIFFICULTIES;
                    if (diffIndex == SYSEX_ALLDIFF)
                    {
                        // Loops through all the diffs
                        diffIndex = 0;
                    }
                    else
                    {
                        // Limited to only the current one
                        max = diffIndex + 1;
                    }

                    while (diffIndex < max)
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
                        ++diffIndex;
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
                {
                    // Unless, for some stupid-ass reason, this track contains lyrics,
                    // all actually useful events will utilize ASCII encoding for state
                    var str = midiTrack.ExtractTextOrSysEx();
                    var ev = str.GetString(Encoding.ASCII);
                    instrumentTrack.Events
                        .GetLastOrAdd(position)
                        .Add(ev);
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
    }
}
