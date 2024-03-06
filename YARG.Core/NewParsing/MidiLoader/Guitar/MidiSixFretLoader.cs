using System;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiSixFretLoader
    {
        private const int SIXFRET_MIN = 58;
        private const int SIXFRET_MAX = 105;
        private const int NUM_LANES = 7;

        private const int BRE_MIN = 120;
        private const int BRE_MAX = 124;

        private const int STARPOWER_DIFF_OFFSET = 8;
        private const int STARPOWER_DIFF_VALUE = 12;

        private static readonly int[] LANEVALUES = new int[] {
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
            0, 4, 5, 6, 1, 2, 3, 7, 8, 9, 10, 11,
        };


        public static unsafe BasicInstrumentTrack2<GuitarNote2<SixFret>> Load(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var instrumentTrack = new BasicInstrumentTrack2<GuitarNote2<SixFret>>();
            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
            {
                instrumentTrack[i] = new DifficultyTrack2<GuitarNote2<SixFret>>();
            }

            var diffModifiers = stackalloc GuitarMidiDifficulty[InstrumentTrack2.NUM_DIFFICULTIES];

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
            var phraseInfo = default(SpecialPhraseInfo);
            phraseInfo.Velocity = 100;

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

                        if (SIXFRET_MIN <= note.value && note.value <= SIXFRET_MAX)
                        {
                            int noteValue = note.value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.TryAppend(position, out var guitar))
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
                                    case 7:
                                        {
                                            diffModifier.HopoOn = true;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                    guitar->State = GuitarState.Hopo;
                                            }
                                            break;
                                        }
                                    case 8:
                                        {
                                            diffModifier.HopoOff = true;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Natural)
                                                    guitar->State = GuitarState.Strum;
                                            }
                                            break;
                                        }
                                    case 9:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      58      +     3     *   12                                         +   9
                                        // = 103 = Solo
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            soloPosition = position;
                                            instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                        }
                                        break;
                                    case 10:
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
                        if (SIXFRET_MIN <= note.value && note.value <= SIXFRET_MAX)
                        {
                            int noteValue = note.value - SIXFRET_MIN;
                            int diffIndex = MidiLoader_Constants.DIFFVALUES[noteValue];
                            var diffTrack = instrumentTrack[diffIndex]!;
                            ref var diffModifier = ref diffModifiers[diffIndex];
                            int lane = LANEVALUES[noteValue];
                            if (lane < NUM_LANES)
                            {
                                ref var colorPosition = ref lanes[diffIndex * NUM_LANES + lane];
                                if (colorPosition.Ticks != -1)
                                {
                                    diffTrack.Notes.TraverseBackwardsUntil(colorPosition)[lane] = DualTime.Truncate(position - colorPosition);
                                    colorPosition.Ticks = -1;
                                }
                            }
                            else
                            {
                                switch (lane)
                                {
                                    case 7:
                                        {
                                            diffModifier.HopoOn = false;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State != GuitarState.Tap)
                                                {
                                                    guitar->State = diffModifier.HopoOff ? GuitarState.Strum : GuitarState.Natural;
                                                }
                                            }
                                            break;
                                        }
                                    case 8:
                                        {
                                            diffModifier.HopoOff = false;
                                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                                            {
                                                if (guitar->State == GuitarState.Strum)
                                                    guitar->State = GuitarState.Natural;
                                            }
                                            break;
                                        }
                                    case 9:
                                        // FIVEFRET_MIN + diffIndex * MidiPreparser_Constants.NOTES_PER_DIFFICULTY + lane
                                        //      58      +     3     *   12                                         +   9
                                        // = 103 = Solo
                                        //
                                        // Accessing this value in this manner allows actual notes to parse in the fastest path
                                        if (diffIndex == 3)
                                        {
                                            if (soloPosition.Ticks > -1)
                                            {
                                                phraseInfo.Duration = position - soloPosition;
                                                instrumentTrack.SpecialPhrases
                                                        .TraverseBackwardsUntil(soloPosition)
                                                        .Add(SpecialPhraseType.Solo, phraseInfo);
                                                soloPosition.Ticks = -1;
                                            }
                                        }
                                        break;
                                    case 10:
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

                            }
                        }
                    }
                }
                else if (midiTrack.Type is MidiEventType.SysEx or MidiEventType.SysEx_End)
                {
                    var sysex = midiTrack.ExtractTextOrSysEx();
                    if (sysex.Length != 8 || !sysex.SequenceEqual(SYSEXTAG) || sysex[5] != 4)
                    {
                        continue;
                    }

                    bool enable = sysex[6] == 1;
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

                    if (sysex[4] == (char) 0xFF)
                    {
                        for (int diffIndex = 0; diffIndex < 4; ++diffIndex)
                        {
                            var diffTrack = instrumentTrack[diffIndex]!;
                            diffModifiers[diffIndex].SliderNotes = enable;
                            if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                            {
                                diffModifiers[diffIndex].ModifyNote(ref *guitar);
                            }
                        }
                    }
                    else
                    {
                        byte diffIndex = sysex[4];
                        var diffTrack = instrumentTrack[diffIndex]!;
                        diffModifiers[diffIndex].SliderNotes = enable;
                        if (diffTrack.Notes.TryGetLastValue(position, out var guitar))
                        {
                            diffModifiers[diffIndex].ModifyNote(ref *guitar);
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
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
