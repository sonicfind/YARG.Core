using System.Text;
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

        public static unsafe bool Load(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, InstrumentTrack2<ProKeysDifficultyTrack> instrumentTrack, int diffIndex)
        {
            ref var diffTrack = ref instrumentTrack.Difficulties[diffIndex];
            if (diffTrack != null)
            {
                return false;
            }

            diffTrack = new ProKeysDifficultyTrack();
            // We do this on the commonality that most charts do exceed this number of notes.
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

                        if (PROKEY_MIN <= note.value && note.value <= PROKEY_MAX)
                        {
                            lanes[note.value - PROKEY_MIN] = position;
                            diffTrack.Notes.TryAppend(in position);
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
                                case 0: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.C1_E2); break;
                                case 2: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.D1_F2); break;
                                case 4: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.E1_G2); break;
                                case 5: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.F1_A2); break;
                                case 7: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.G1_B2); break;
                                case 9: diffTrack.Ranges.AppendOrUpdate(in position, ProKey_Ranges.A1_C3); break;
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
                                if (!ProKeyNote.Add(diffTrack.Notes.TraverseBackwardsUntil(in lane), note.value, position - lane))
                                {
                                    YargLogger.LogWarning("Illegal pro keys charting discovered");
                                }
                                lane.Ticks = -1;
                            }
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
                                case SOLO_MIDI:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Soloes.Append(in soloPosition, position - soloPosition);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case BRE_MIDI:
                                    if (brePosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.BREs.Append(in brePosition, position - brePosition);
                                        brePosition.Ticks = -1;
                                    }
                                    break;
                                case GLISSANDO_MIDI:
                                    if (glissPostion.Ticks > -1)
                                    {
                                        diffTrack.Glissandos.Append(in glissPostion, position - glissPostion);
                                        glissPostion.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    if (trillPosition.Ticks > -1)
                                    {
                                        instrumentTrack.Phrases.Trills.Append(in trillPosition, position - trillPosition);
                                        trillPosition.Ticks = -1;
                                    }
                                    break;
                            };

                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit && midiTrack.Type != MidiEventType.Text_TrackName)
                {
                    // Unless, for some stupid-ass reason, this track contains lyrics,
                    // all actually useful events will utilize ASCII encoding for state
                    var ev = Encoding.ASCII.GetString(midiTrack.ExtractTextOrSysEx());
                    instrumentTrack.Events
                        .GetLastOrAppend(position)
                        .Add(ev);
                }
            }
            return true;
        }
    }
}
