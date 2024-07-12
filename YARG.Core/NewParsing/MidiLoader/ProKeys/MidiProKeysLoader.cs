using System.Text;
using YARG.Core.IO;

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

        public static unsafe bool Load(YARGMidiTrack midiTrack, SyncTrack2 sync, InstrumentTrack2<ProKeysDifficultyTrack> instrumentTrack, int diffIndex)
        {
            ref var diffTrack = ref instrumentTrack[diffIndex];
            if (diffTrack != null)
            {
                return false;
            }

            diffTrack = new ProKeysDifficultyTrack();
            var lanes = stackalloc DualTime[NUM_NOTES]
            {
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
                DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive, DualTime.Inactive,
            };

            var overdrivePosition = DualTime.Inactive;
            var soloPosition = DualTime.Inactive;
            var brePosition = DualTime.Inactive;
            var glissPostion = DualTime.Inactive;
            var trillPosition = DualTime.Inactive;
            var phraseInfo = default(SpecialPhraseInfo);
            phraseInfo.Velocity = 100;

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
                    // Note Ons with no velocity equates to a note Off by spec
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

                        if (PROKEY_MIN <= note.Value && note.Value <= PROKEY_MAX)
                        {
                            lanes[note.Value - PROKEY_MIN] = position;
                            diffTrack.Notes.GetLastOrAppend(position);
                        }
                        else
                        {
                            switch (note.Value)
                            {
                                case MidiLoader_Constants.OVERDRIVE:
                                    overdrivePosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case SOLO_MIDI:
                                    soloPosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case BRE_MIDI:
                                    brePosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case GLISSANDO_MIDI:
                                    glissPostion = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case MidiLoader_Constants.TRILL:
                                    trillPosition = position;
                                    instrumentTrack.SpecialPhrases.GetLastOrAppend(position);
                                    break;
                                case 0: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.C1_E2); break;
                                case 2: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.D1_F2); break;
                                case 4: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.E1_G2); break;
                                case 5: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.F1_A2); break;
                                case 7: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.G1_B2); break;
                                case 9: diffTrack.Ranges.AppendOrUpdate(position, ProKey_Ranges.A1_C3); break;
                            };
                        }
                    }
                    // NoteOff from this point
                    else
                    {
                        if (PROKEY_MIN <= note.Value && note.Value <= PROKEY_MAX)
                        {
                            ref var lane = ref lanes[note.Value - PROKEY_MIN];
                            if (lane.Ticks > -1)
                            {
                                var duration = position - lane;
                                diffTrack.Notes
                                    .TraverseBackwardsUntil(lane)
                                    .Add(note.Value, duration);
                                lane.Ticks = -1;
                            }
                        }
                        else
                        {
                            switch (note.Value)
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
                                case SOLO_MIDI:
                                    if (soloPosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - soloPosition;
                                        instrumentTrack.SpecialPhrases
                                            .TraverseBackwardsUntil(soloPosition)
                                            .Add(SpecialPhraseType.Solo, phraseInfo);
                                        soloPosition.Ticks = -1;
                                    }
                                    break;
                                case BRE_MIDI:
                                    if (brePosition.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - brePosition;
                                        instrumentTrack.SpecialPhrases
                                            .TraverseBackwardsUntil(brePosition)
                                            .Add(SpecialPhraseType.BRE, phraseInfo);
                                        brePosition.Ticks = -1;
                                    }
                                    break;
                                case GLISSANDO_MIDI:
                                    if (glissPostion.Ticks > -1)
                                    {
                                        phraseInfo.Duration = position - glissPostion;
                                        instrumentTrack.SpecialPhrases
                                            .TraverseBackwardsUntil(glissPostion)
                                            .Add(SpecialPhraseType.Glissando, phraseInfo);
                                        glissPostion.Ticks = -1;
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
                            };

                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    var ev = str.GetString(Encoding.ASCII);
                    instrumentTrack.Events
                        .GetLastOrAppend(position)
                        .Add(ev);
                }
            }
            return true;
        }
    }
}
