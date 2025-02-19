﻿using System.Text;
using YARG.Core.Containers;
using YARG.Core.IO;
using YARG.Core.Logging;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiProKeysLoader
    {
        private const int PROKEY_MIN = 48;
        private const int PROKEY_MAX = 72;
        private const int NUM_NOTES = 25;
        private const int SOLO_MIDI = 115;
        private const int BRE_MIDI = 120;
        private const int GLISSANDO_MIDI = 126;

        public static unsafe bool Load(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, ProKeysInstrumentTrack instrumentTrack, int diffIndex)
        {
            var diffTrack = instrumentTrack[diffIndex];
            if (!diffTrack.Notes.IsEmpty())
            {
                return false;
            }

            using var overdrives = new YARGNativeSortedList<DualTime, DualTime>();
            using var soloes = new YARGNativeSortedList<DualTime, DualTime>();
            using var trills = new YARGNativeSortedList<DualTime, DualTime>();
            using var bres = new YARGNativeSortedList<DualTime, DualTime>();

            // We do this on the commonality that most charts do not exceed this number of notes.
            // Helps keep reallocations to a minimum.
            diffTrack.Notes.Capacity = 5000;

            var lanes = stackalloc DualTime[NUM_NOTES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            // Various special phrases trackers
            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var brePosition = DualTime.Inactive;
            var glissPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;

            var position = DualTime.Zero;
            var note = default(MidiNote);
            var stats = default(YARGMidiTrack.Stats);
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
                    if (stats.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        // If the distance between the current NoteOn and the previous NoteOn is less than a certain threshold
                        // the previous position will override the current one, to "chord" multiple notes together
                        chordSnapper.Snap(ref position);
                        if (PROKEY_MIN <= note.value && note.value <= PROKEY_MAX)
                        {
                            lanes[note.value - PROKEY_MIN] = position;
                            diffTrack.Notes.TryAdd(in position);
                        }
                        else
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    break;
                                case SOLO_MIDI:
                                    soloPosition = position;
                                    break;
                                case BRE_MIDI:
                                    brePosition = position;
                                    break;
                                case GLISSANDO_MIDI:
                                    glissPostion = position;
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    break;
                                case 0: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.C1_E2); break;
                                case 2: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.D1_F2); break;
                                case 4: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.E1_G2); break;
                                case 5: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.F1_A2); break;
                                case 7: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.G1_B2); break;
                                case 9: diffTrack.Ranges.AddOrUpdate(in position, ProKey_Ranges.A1_C3); break;
                            };
                        }
                    }
                    // NoteOff from this point
                    else
                    {
                        if (PROKEY_MIN <= note.value && note.value <= PROKEY_MAX)
                        {
                            ref var lane = ref lanes[note.value - PROKEY_MIN];
                            if (lane.Ticks > -1)
                            {
                                // Attempts to add a lane to the found note
                                //
                                // Will fail if four lanes were already applied to said note
                                if (!diffTrack.Notes.TraverseBackwardsUntil(in lane)->Add(note.value, position - lane))
                                {
                                    YargLogger.LogWarning("Illegal pro keys charting discovered");
                                }
                                lane.Ticks = -1;
                            }
                        }
                        else if (diffIndex == 3)
                        {
                            switch (note.value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        overdrives.Add(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case SOLO_MIDI:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        soloes.Add(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case BRE_MIDI:
                                    if (brePosition.Ticks > -1)
                                    {
                                        bres.Add(in brePosition, position - brePosition);
                                        brePosition.Ticks = -1;
                                    }
                                    break;
                                case GLISSANDO_MIDI:
                                    if (glissPostion.Ticks > -1)
                                    {
                                        instrumentTrack.Glissandos.Add(in glissPostion, position - glissPostion);
                                        glissPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        trills.Add(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                            };

                        }
                    }
                }
                else if (diffIndex == 3 && MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
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

            if (diffIndex == 3)
            {
                foreach (var diff in instrumentTrack)
                {
                    diff.Overdrives.CopyFrom(overdrives);
                    diff.Soloes.CopyFrom(soloes);
                    diff.BREs.CopyFrom(bres);
                    diff.Trills.CopyFrom(trills);
                }
            }
            return true;
        }
    }
}
