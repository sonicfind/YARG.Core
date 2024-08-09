using System;
using System.Text;
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

        private const int BRE_MIN = 120;
        private const int BRE_MAX = 124;

        private const int STARPOWER_DIFF_OFFSET = 8;
        private const int STARPOWER_DIFF_VALUE = 12;

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

        private struct SixFretDiff
        {
            public bool SliderNotes;
            public bool HopoOn;
            public bool HopoOff;
        }

        public static unsafe InstrumentTrack2<DifficultyTrack2<SixFretGuitar>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new InstrumentTrack2<DifficultyTrack2<SixFretGuitar>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack.Difficulties[i] = new DifficultyTrack2<SixFretGuitar>();
            }

            var diffModifiers = stackalloc SixFretDiff[InstrumentTrack2.NUM_DIFFICULTIES];

            var lanes = stackalloc DualTime[InstrumentTrack2.NUM_DIFFICULTIES * NUM_LANES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            var brePositions = stackalloc DualTime[6];
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var tremoloPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            ReadOnlySpan<byte> SYSEXTAG = stackalloc byte[] { (byte) 'P', (byte) 'S', (byte) '\0', };
            var currTempo = sync.TempoMarkers.Data;
            var tempoEnd = sync.TempoMarkers.End;
            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref currTempo, tempoEnd);
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

                        if (SIXFRET_MIN <= note.value && note.value <= SIXFRET_MAX)
                        {
                            int noteValue = note.value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.GetLastOrAppend(in position, out var guitar))
                                {
                                    if (diffModifier.SliderNotes)
                                        guitar->State = GuitarState.Tap;
                                    else if (diffModifier.HopoOn)
                                        guitar->State = GuitarState.Hopo;
                                    else if (diffModifier.HopoOff)
                                        guitar->State = GuitarState.Strum;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifier.HopoOn = true;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                    guitar->State = GuitarState.Hopo;
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifier.HopoOff = true;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Natural)
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
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (BRE_MIN <= note.value && note.value <= BRE_MAX)
                        {
                            brePositions[note.value - BRE_MIN] = position;
                        }
                        else
                        {
                            // Note: Solo phrase is handled in the sixfret note scope for optimization reasons
                            switch (note.value)
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
                        if (SIXFRET_MIN <= note.value && note.value <= SIXFRET_MAX)
                        {
                            int noteValue = note.value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    ((DualTime*) diffTrack.Notes.TraverseBackwardsUntil(in colorPosition))[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case HOPO_ON_INDEX:
                                        {
                                            diffModifier.HopoOn = false;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                {
                                                    guitar->State = diffModifier.HopoOff ? GuitarState.Strum : GuitarState.Natural;
                                                }
                                            }
                                            break;
                                        }
                                    case HOPO_OFF_INDEX:
                                        {
                                            diffModifier.HopoOff = false;
                                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Strum)
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
                                                instrumentTrack.Soloes.Append(in soloPosition, position - soloPosition);
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
                                                diffModifiers[i].SliderNotes = false;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (BRE_MIN <= note.value && note.value <= BRE_MAX)
                        {
                            ref var bre = ref brePositions[note.value - BRE_MIN];
                            if (bre.Ticks > -1
                                && brePositions[0] == brePositions[1]
                                && brePositions[1] == brePositions[2]
                                && brePositions[2] == brePositions[3]
                                && brePositions[3] == brePositions[4]
                                && brePositions[4] == brePositions[5])
                            {
                                instrumentTrack.BREs.Append(in bre, position - bre);
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
                                        instrumentTrack.Overdrives.Append(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TREMOLO:
                                    if (tremoloPostion.Ticks > -1)
                                    {
                                        instrumentTrack.Tremolos.Append(in tremoloPostion, position - tremoloPostion);
                                        tremoloPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Trills.Append(in trillPosition, position - trillPosition);
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
                    if (sysex.Length != SYSEX_LENGTH || !sysex.SequenceEqual(SYSEXTAG) || sysex[SYSEX_TYPE_INDEX] != SYSEX_TAP_TYPE)
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

                    byte diffValue = sysex[SYSEX_DIFF_INDEX];
                    if (diffValue == SYSEX_ALLDIFF)
                    {
                        for (int diffIndex = 0; diffIndex < InstrumentTrack2.NUM_DIFFICULTIES; ++diffIndex)
                        {
                            var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                            diffModifiers[diffIndex].SliderNotes = enable;
                            if (diffTrack.Notes.TryGetLastValue(in position, out var guitar))
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
                        }
                    }
                    else
                    {
                        if (instrumentTrack.Difficulties[diffValue]!.Notes.TryGetLastValue(in position, out var guitar))
                        {
                            if (enable)
                            {
                                guitar->State = GuitarState.Tap;
                            }
                            else if (guitar->State == GuitarState.Tap)
                            {
                                if (diffModifiers[diffValue].HopoOn)
                                    guitar->State = GuitarState.Hopo;
                                else if (diffModifiers[diffValue].HopoOff)
                                    guitar->State = GuitarState.Strum;
                                else
                                    guitar->State = GuitarState.Natural;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    var ev = Encoding.ASCII.GetString(str);
                    instrumentTrack.Events
                        .GetLastOrAppend(position)
                        .Add(ev);
                }
            }
            return instrumentTrack;
        }
    }
}
