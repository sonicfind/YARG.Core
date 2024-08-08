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

        public static VocalTrack2 LoadPartVocals(YARGMidiTrack midiTrack, SyncTrack2 sync, ref Encoding encoding)
        {
            var vocalTrack = new VocalTrack2(1);
            Load(midiTrack, sync, vocalTrack, 0, ref encoding);
            return vocalTrack;
        }

        public static bool Load(YARGMidiTrack midiTrack, SyncTrack2 sync, VocalTrack2 vocalTrack, int trackIndex, ref Encoding encoding)
        {
            var part = vocalTrack[trackIndex];
            if (!part.IsEmpty())
            {
                return false;
            }
            
            // In the case of bad rips with overlaps, we need this to apply the correct pitch
            // instead of using `note.value`
            int vocalPitch = 0;
            var vocalPosition = DualTime.Inactive;
            var vocalNote = default(VocalNote2);

            var percussionPosition = DualTime.Inactive;

            var phrasePosition_1 = DualTime.Inactive;
            var phrasePosition_2 = DualTime.Inactive;
            var overdrivePosition = DualTime.Inactive;
            var rangeShiftPosition = DualTime.Inactive;

            int tempoIndex = 0;
            var note = default(MidiNote);
            var position = default(DualTime);
            while (midiTrack.ParseEvent())
            {
                position.Ticks = midiTrack.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (midiTrack.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Note Ons with no velocity equates to a note Off by spec
                    if (midiTrack.Type == MidiEventType.Note_On && note.velocity > 0)
                    {
                        if ((VOCAL_MIN <= note.value && note.value <= VOCAL_MAX) || note.value == GH_TALKIE)
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
                            vocalPitch = note.value;
                        }
                        else if (trackIndex == 0)
                        {
                            if (note.value == VOCAL_PHRASE_1)
                            {
                                phrasePosition_1 = position;
                            }
                            else if (note.value == VOCAL_PHRASE_2)
                            {
                                phrasePosition_2 = position;
                            }
                            else if (note.value == MidiLoader_Constants.OVERDRIVE)
                            {
                                overdrivePosition = position;
                            }
                            else if (note.value == PERCUSSION_NOTE || note.value == PERCUSSION_NOISE)
                            {
                                percussionPosition = position;
                            }
                            else if (note.value == RANGESHIFT)
                            {
                                rangeShiftPosition = position;
                            }
                            else if (note.value == LYRICSHIFT)
                            {
                                vocalTrack.LyricShifts.Append(position);
                            }
                        }
                        else if (trackIndex == 1 && (note.value == VOCAL_PHRASE_1 || note.value == VOCAL_PHRASE_2))
                        {
                            phrasePosition_1 = position;
                        }
                    }
                    // NoteOff from this point
                    else
                    {
                        if ((VOCAL_MIN <= note.value && note.value <= VOCAL_MAX) || note.value == GH_TALKIE)
                        {
                            if (vocalPosition.Ticks > -1)
                            {
                                AddNote(position - vocalPosition);
                            }
                            vocalPosition.Ticks = -1;
                        }
                        else if (trackIndex == 0)
                        {
                            switch (note.value)
                            {
                                case VOCAL_PHRASE_1:
                                    if (phrasePosition_1.Ticks > -1)
                                    {
                                        vocalTrack.VocalPhrases_1.Append_NoReturn(phrasePosition_1, position - phrasePosition_1);
                                        phrasePosition_1.Ticks = -1;
                                    }
                                    break;
                                case VOCAL_PHRASE_2:
                                    if (phrasePosition_2.Ticks > -1)
                                    {
                                        vocalTrack.VocalPhrases_2.Append_NoReturn(phrasePosition_2, position - phrasePosition_2);
                                        phrasePosition_2.Ticks = -1;
                                    }
                                    break;
                                case MidiLoader_Constants.OVERDRIVE:
                                    if (overdrivePosition.Ticks > -1)
                                    {
                                        vocalTrack.Overdrives.Append_NoReturn(overdrivePosition, position - overdrivePosition);
                                        overdrivePosition.Ticks = -1;
                                    }
                                    break;
                                case PERCUSSION_NOTE:
                                case PERCUSSION_NOISE:
                                    if (percussionPosition.Ticks > -1)
                                    {
                                        vocalTrack.Percussion.Append_NoReturn(percussionPosition, note.value == PERCUSSION_NOTE);
                                        percussionPosition.Ticks = -1;
                                    }
                                    break;
                                case RANGESHIFT:
                                    if (rangeShiftPosition.Ticks > -1)
                                    {
                                        vocalTrack.RangeShifts.Append_NoReturn(rangeShiftPosition, position - rangeShiftPosition);
                                        rangeShiftPosition.Ticks = -1;
                                    }
                                    break;
                            }
                        }
                        else if (trackIndex == 1 && (note.value == VOCAL_PHRASE_1 || note.value == VOCAL_PHRASE_2))
                        {
                            if (phrasePosition_1.Ticks > -1)
                            {
                                vocalTrack.HarmonyLines.Append_NoReturn(phrasePosition_1, position - phrasePosition_1);
                                phrasePosition_1.Ticks = -1;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    var str = midiTrack.ExtractTextOrSysEx();
                    if (str.Length == 0 || str[0] != '[')
                    {
                        string lyric;
                        try
                        {
                            lyric = encoding.GetString(str);
                        }
                        catch
                        {
                            if (encoding != Encoding.UTF8)
                            {
                                throw;
                            }
                            encoding = YARGTextReader.Latin1;
                            lyric = encoding.GetString(str);
                        }

                        vocalNote.TalkieState = TalkieState.None;
                        if (lyric.Length > 0)
                        {
                            vocalNote.TalkieState = lyric[^1] switch
                            {
                                '#' or '*' => TalkieState.Talkie,
                                '^'        => TalkieState.Lenient,
                                _          => TalkieState.None,
                            };
                        }

                        part.Lyrics.AppendOrUpdate(position, lyric);
                    }
                    else if (trackIndex == 0)
                    {
                        if (str.SequenceEqual(RANGESHIFT_TEXT))
                        {
                            var endPoint = position;
                            endPoint.Ticks += sync.Tickrate;
                            endPoint.Seconds = sync.ConvertToSeconds(endPoint.Ticks, ref tempoIndex);
                            vocalTrack.RangeShifts.AppendOrUpdate(position, endPoint - position);
                        }
                        else
                        {
                            var ev = Encoding.ASCII.GetString(str);
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
                if (note.value != GH_TALKIE)
                {
                    vocalNote.Pitch = note.value;
                }
                else if (vocalNote.TalkieState == TalkieState.None)
                {
                    vocalNote.Pitch = 0;
                    vocalNote.TalkieState = TalkieState.Talkie;
                }

                if (part.Notes.Capacity == 0)
                {
                    part.Notes.Capacity = 500;
                }
                part.Notes.Append_NoReturn(vocalPosition, in vocalNote);
                vocalNote.TalkieState = TalkieState.None;
            }
        }
    }
}
