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
                instrumentTrack.Difficulties[i] = new DifficultyTrack2<FourLaneDrums>();
            }
            var expertTrack = instrumentTrack[Difficulty.Expert]!;

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

            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(YARGMidiTrack.Stats);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.velocity > 0)
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
                                var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (lane >= SNARE_LANE)
                                {
                                    if (enableDynamics)
                                    {
                                        (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.velocity switch
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
                                expertTrack.Notes.TryAppend(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            int index = note.value - TOM_MIN_VALUE;
                            cymbalFlags[index] = false;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
                                {
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
                                        if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
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
                                    (&instrumentTrack.Difficulties[diffIndex]!.Notes.TraverseBackwardsUntil(in colorPosition)->Bass)[lane] = position - colorPosition;
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
                                    var drum = expertTrack.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            int index = note.value - TOM_MIN_VALUE;
                            cymbalFlags[index] = true;
                            for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                            {
                                if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
                                {
                                    (&drum->Cymbal_Yellow)[index] = true;
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
                                        if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
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
                instrumentTrack.Difficulties[i] = new DifficultyTrack2<FiveLaneDrums>();
            }
            var expertTrack = instrumentTrack[Difficulty.Expert]!;

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

            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(YARGMidiTrack.Stats);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.velocity > 0)
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
                                var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                                lanes[diffIndex * NUM_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (enableDynamics && lane >= SNARE_LANE)
                                {
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
                                expertTrack.Notes.TryAppend(in position);
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
                                        if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
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
                                    (&instrumentTrack.Difficulties[diffIndex]!.Notes.TraverseBackwardsUntil(in colorPosition)->Bass)[lane] = position - colorPosition;
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
                                    var drum = expertTrack.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
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
                                        if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
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
                instrumentTrack.Difficulties[i] = new DifficultyTrack2<UnknownLaneDrums>();
            }
            var expertTrack = instrumentTrack[Difficulty.Expert]!;

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

            var position = default(DualTime);
            var lastOnNote = default(DualTime);
            var note = default(MidiNote);
            var stats = default(YARGMidiTrack.Stats);
            var tempoTracker = new TempoTracker(sync);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = tempoTracker.Traverse(position.Ticks);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    if (stats.Type == MidiEventType.Note_On && note.velocity > 0)
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
                            // if we detect prodrums flags, this value changes to disallow the fifth pad lane
                            if (lane < numLanes)
                            {
                                var diffTrack = instrumentTrack.Difficulties[diffIndex]!;
                                lanes[diffIndex * MAX_LANES + lane] = position;
                                if (diffTrack.Notes.Capacity == 0)
                                {
                                    diffTrack.Notes.Capacity = 5000;
                                }

                                if (diffTrack.Notes.GetLastOrAppend(in position, out var drum))
                                {
                                    drum->IsFlammed = flamFlag;
                                }

                                if (lane < FIFTH_LANE)
                                {
                                    if (lane >= SNARE_LANE)
                                    {
                                        if (enableDynamics)
                                        {
                                            (&drum->Dynamics_Snare)[lane - SNARE_LANE] = note.velocity switch
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
                                expertTrack.Notes.TryAppend(in position);
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                cymbalFlags[index] = false;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        (&drum->Cymbal_Yellow)[index] = false;
                                    }
                                }
                                drumsType = DrumsType.ProDrums;
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
                                        if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
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
                                    var drum = instrumentTrack.Difficulties[diffIndex]!.Notes.TraverseBackwardsUntil(in colorPosition);
                                    var duration = position - colorPosition;
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
                                    var drum = expertTrack.Notes.TraverseBackwardsUntil(in colorPosition);
                                    drum->Bass = position - colorPosition;
                                    drum->IsDoubleBass = true;
                                    colorPosition.Ticks = -1;
                                }
                            }
                        }
                        else if (TOM_MIN_VALUE <= note.value && note.value <= TOM_MAX_VALUE)
                        {
                            if ((drumsType & DrumsType.ProDrums) == DrumsType.ProDrums)
                            {
                                int index = note.value - TOM_MIN_VALUE;
                                cymbalFlags[index] = true;
                                for (int i = 0; i < InstrumentTrack2.NUM_DIFFICULTIES; ++i)
                                {
                                    if (instrumentTrack.Difficulties[i]!.Notes.TryGetLastValue(in position, out var drum))
                                    {
                                        (&drum->Cymbal_Yellow)[index] = true;
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
                                        var diffTrack = instrumentTrack.Difficulties[i]!;
                                        if (diffTrack.Notes.TryGetLastValue(in position, out var drum))
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
