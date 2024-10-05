using System;
using System.Text;
using YARG.Core.Chart;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public static class MidiVocalsLoader
    {
        private const int GH_TALKIE = 26;
        private const int VOCAL_MIN = 36;
        private const int VOCAL_MAX = 84;
        private const int PERCUSSION_NOTE = 96;
        private const int PERCUSSION_NOISE = 97;

        private const int VOCAL_PHRASE_1 = 105;
        private const int VOCAL_PHRASE_2 = 106;
        private const int RANGESHIFT = 0;
        private const int LYRICSHIFT = 1;

        private static readonly byte[] RANGESHIFT_TEXT = Encoding.ASCII.GetBytes("[range_shift]");

        public static LeadVocalsTrack LoadPartVocals(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, ref Encoding encoding)
        {
            var vocalTrack = new LeadVocalsTrack();
            Load(midiTrack, ref tempoTracker, vocalTrack, 0, ref encoding);
            return vocalTrack;
        }

        public static unsafe bool Load(YARGMidiTrack midiTrack, ref TempoTracker tempoTracker, VocalTrack2 vocalTrack, int trackIndex, ref Encoding encoding)
        {
            var part = vocalTrack[trackIndex];
            if (!part.IsEmpty())
            {
                return false;
            }

            // In the case of bad rips with overlaps, we need this to apply the correct pitch
            // instead of using `note.Value`
            int vocalPitch = 0;
            var vocalPosition = DualTime.Inactive;
            var vocalNote = default(VocalNote2);

            var percussionPosition = DualTime.Inactive;

            // Various special phrases trackers
            var phrasePosition_1 = DualTime.Inactive;
            var phrasePosition_2 = DualTime.Inactive;
            var overdrivePosition = DualTime.Inactive;
            var rangeShiftPosition = DualTime.Inactive;

            var note = default(MidiNote);
            var position = default(DualTime);
            var stats = default(MidiStats);
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
                        if ((VOCAL_MIN <= note.Value && note.Value <= VOCAL_MAX) || note.Value == GH_TALKIE)
                        {
                            // Accounts for bad midis where a new vocal note is started before clearing the previous note
                            if (vocalPosition.Ticks > -1)
                            {
                                var duration = position - vocalPosition;
                                // Obviously, can't have them overlap
                                if (duration.Ticks > 240)
                                {
                                    long newticks = duration.Ticks - 120;
                                    duration.Seconds = (newticks * duration.Seconds / duration.Ticks);
                                    duration.Ticks = newticks;
                                }
                                else
                                {
                                    duration.Ticks /= 2;
                                    duration.Seconds /= 2;
                                }

                                AddNote(in duration);
                            }

                            vocalPosition = position;
                            vocalPitch = note.Value;
                        }
                        // Only the lead track can hold non-harmony line phrases...
                        else if (trackIndex == 0)
                        {
                            if (note.Value == VOCAL_PHRASE_1)
                            {
                                phrasePosition_1 = position;
                            }
                            else if (note.Value == VOCAL_PHRASE_2)
                            {
                                phrasePosition_2 = position;
                            }
                            else if (note.Value == MidiLoader_Constants.OVERDRIVE)
                            {
                                overdrivePosition = position;
                            }
                            else if (note.Value == PERCUSSION_NOTE || note.Value == PERCUSSION_NOISE)
                            {
                                percussionPosition = position;
                            }
                            else if (note.Value == RANGESHIFT)
                            {
                                rangeShiftPosition = position;
                            }
                            else if (note.Value == LYRICSHIFT)
                            {
                                vocalTrack.LyricShifts.Push(position);
                            }
                        }
                        // and only harmony 2 can specify harmony lines
                        else if (trackIndex == 1 && (note.Value == VOCAL_PHRASE_1 || note.Value == VOCAL_PHRASE_2))
                        {
                            phrasePosition_1 = position;
                        }
                    }
                    // NoteOff from this point
                    else
                    {
                        if ((VOCAL_MIN <= note.Value && note.Value <= VOCAL_MAX) || note.Value == GH_TALKIE)
                        {
                            if (vocalPosition.Ticks > -1)
                            {
                                AddNote(position - vocalPosition);
                            }
                            vocalPosition.Ticks = -1;
                        }
                        // Only the lead track can add non-harmony line phrases...
                        else if (trackIndex == 0)
                        {
                            switch (note.Value)
                            {
                                case VOCAL_PHRASE_1:
                                    if (phrasePosition_1.Ticks > -1)
                                    {
                                        vocalTrack.VocalPhrases_1.Append(in phrasePosition_1, position - phrasePosition_1);
                                        phrasePosition_1.Ticks = -1;
                                    }
                                    break;
                                case VOCAL_PHRASE_2:
                                    if (phrasePosition_2.Ticks > -1)
                                    {
                                        vocalTrack.VocalPhrases_2.Append(in phrasePosition_2, position - phrasePosition_2);
                                        phrasePosition_2.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        vocalTrack.Overdrives.Append(in overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case PERCUSSION_NOTE:
                                case PERCUSSION_NOISE:
                                    if (percussionPosition.Ticks > -1)
                                    {
                                        vocalTrack.Percussion.Append(in percussionPosition, note.Value == PERCUSSION_NOTE);
                                        percussionPosition.Ticks = -1;
                                    }
                                    break;
                                case RANGESHIFT:
                                    if (rangeShiftPosition.Ticks > -1)
                                    {
                                        vocalTrack.RangeShifts.Append(in rangeShiftPosition, position - rangeShiftPosition);
                                        rangeShiftPosition.Ticks = -1;
                                    }
                                    break;
                            }
                        }
                        // and only harmony 2 can add harmony lines
                        else if (trackIndex == 1 && (note.Value == VOCAL_PHRASE_1 || note.Value == VOCAL_PHRASE_2))
                        {
                            if (phrasePosition_1.Ticks > -1)
                            {
                                vocalTrack.HarmonyLines.Append(in phrasePosition_1, position - phrasePosition_1);
                                phrasePosition_1.Ticks = -1;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit && stats.Type != MidiEventType.Text_TrackName)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (str.length == 0 || str[0] != '[')
                    {
                        string lyric;
                        try
                        {
                            lyric = str.GetString(encoding);
                        }
                        catch
                        {
                            if (encoding != Encoding.UTF8)
                            {
                                throw;
                            }
                            encoding = YARGTextReader.Latin1;
                            lyric = str.GetString(encoding);
                        }

                        if (lyric.Length > 0)
                        {
                            vocalNote.TalkieState = lyric[^1] switch
                            {
                                '#' or '*' => TalkieState.Talkie,
                                '^'        => TalkieState.Lenient,
                                _          => TalkieState.None,
                            };
                        }
                        else
                        {
                            vocalNote.TalkieState = TalkieState.None;
                        }
                        part.Lyrics.AppendOrUpdate(position, lyric);
                    }
                    else if (trackIndex == 0)
                    {
                        if (str.SequenceEqual(RANGESHIFT_TEXT))
                        {
                            var endPoint = position;
                            endPoint.Ticks += tempoTracker.Resolution;
                            endPoint.Seconds = tempoTracker.UnmovingConvert(endPoint.Ticks);
                            vocalTrack.RangeShifts.AppendOrUpdate(in position, endPoint - position);
                        }
                        else
                        {
                            var ev = str.GetString(Encoding.ASCII);
                            vocalTrack.Events
                                .GetLastOrAppend(position)
                                .Add(ev);
                        }
                    }
                }
            }
            return true;

            void AddNote(in DualTime duration)
            {
                vocalNote.Duration = duration;
                if (vocalPitch != GH_TALKIE)
                {
                    vocalNote.Pitch = vocalPitch;
                }
                else
                {
                    // Solely to account for Gh talkite notes
                    // But if the lyric already tells you it's talkie, then no override
                    vocalNote.Pitch = 0;
                    if (vocalNote.TalkieState == TalkieState.None)
                    {
                        vocalNote.TalkieState = TalkieState.Talkie;
                    }
                }

                if (part.Notes.Capacity == 0)
                {
                    // We do this on the commonality that most charts do exceed this number of notes.
                    // Helps keep reallocations to a minimum.
                    part.Notes.Capacity = 1000;
                }
                part.Notes.Append(in vocalPosition, in vocalNote);
                vocalNote.TalkieState = TalkieState.None;
            }
        }
    }
}
