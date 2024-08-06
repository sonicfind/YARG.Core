﻿using System;
using System.Text;
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
            if (!part.IsEmpty() || (trackIndex == 0 && vocalTrack.SpecialPhrases.Count + vocalTrack.Percussion.Count > 0))
            {
                return false;
            }

            
            // In the case of bad rips with overlaps, we need this to apply the correct pitch
            // instead of using `note.value`
            int vocalPitch = 0;
            var vocalPosition = DualTime.Inactive;
            var vocalNote = default(VocalNote2);

            var percussionPosition = DualTime.Inactive;

            var phrasePosition = DualTime.Inactive;
            var overdrivePosition = DualTime.Inactive;
            var rangeShiftPosition = DualTime.Inactive;
            var lyricShiftPosition = DualTime.Inactive;

            int tempoIndex = 0;
            var note = default(MidiNote);
            var position = default(DualTime);
            var stats = default(YARGMidiTrack.Stats);
            while (midiTrack.ParseEvent(ref stats))
            {
                position.Ticks = stats.Position;
                position.Seconds = sync.ConvertToSeconds(position.Ticks, ref tempoIndex);
                if (stats.Type is MidiEventType.Note_On or MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref note);
                    // Note Ons with no velocity equates to a note Off by spec
                    if (stats.Type == MidiEventType.Note_On && note.velocity > 0)
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
                        else if (note.value == VOCAL_PHRASE_1 || note.value == VOCAL_PHRASE_2)
                        {
                            // So not HARM_3, obviously
                            if (trackIndex < 2)
                            {
                                phrasePosition = position;
                                vocalTrack.SpecialPhrases.TryAppend(position);
                            }
                        }
                        // Only lead vocals (PART VOCALS & HARM_1) should handle the below values
                        else if (trackIndex == 0)
                        {
                            if (note.value == MidiLoader_Constants.OVERDRIVE)
                            {
                                overdrivePosition = position;
                                vocalTrack.SpecialPhrases.TryAppend(position);
                            }
                            else if (note.value == PERCUSSION_NOTE || note.value == PERCUSSION_NOISE)
                            {
                                percussionPosition = position;
                            }
                            else if (note.value == RANGESHIFT)
                            {
                                rangeShiftPosition = position;
                                vocalTrack.SpecialPhrases.TryAppend(position);
                            }
                            else if (note.value == LYRICSHIFT)
                            {
                                lyricShiftPosition = position;
                                vocalTrack.SpecialPhrases.TryAppend(position);
                            }
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
                        else if (note.value == VOCAL_PHRASE_1 || note.value == VOCAL_PHRASE_2)
                        {
                            if (phrasePosition.Ticks > -1)
                            {
                                var duration = position - phrasePosition;
                                var type = trackIndex == 0 ? SpecialPhraseType.LyricLine : SpecialPhraseType.HarmonyLine;
                                vocalTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(phrasePosition)
                                        .Add(type, (duration, 100));
                                phrasePosition.Ticks = -1;
                            }
                        }
                        else if (note.value == MidiLoader_Constants.OVERDRIVE)
                        {
                            if (overdrivePosition.Ticks > -1)
                            {
                                var duration = position - overdrivePosition;
                                vocalTrack.SpecialPhrases
                                        .TraverseBackwardsUntil(overdrivePosition)
                                        .Add(SpecialPhraseType.StarPower, (duration, 100));
                                overdrivePosition.Ticks = -1;
                            }
                        }
                        else if (note.value == PERCUSSION_NOTE || note.value == PERCUSSION_NOISE)
                        {
                            if (percussionPosition.Ticks > -1) unsafe
                            {
                                vocalTrack.Percussion.Append(percussionPosition)->IsPlayable = note.value == PERCUSSION_NOTE;
                            }
                            percussionPosition.Ticks = -1;
                        }
                        else if (note.value == RANGESHIFT)
                        {
                            if (rangeShiftPosition.Ticks > -1)
                            {
                                // Range shifts are instant, so manipulation of the duration is pointless
                                vocalTrack.SpecialPhrases
                                    .TraverseBackwardsUntil(rangeShiftPosition)
                                    .TryAdd(SpecialPhraseType.RangeShift, (default(DualTime), 100));
                                rangeShiftPosition.Ticks = -1;
                            }
                        }
                        else if (note.value == LYRICSHIFT)
                        {
                            if (lyricShiftPosition.Ticks > -1)
                            {
                                vocalTrack.SpecialPhrases
                                    .TraverseBackwardsUntil(lyricShiftPosition)
                                    .Add(SpecialPhraseType.LyricShift, (default(DualTime), 100));
                                lyricShiftPosition.Ticks = -1;
                            }
                        }
                    }
                }
                else if (MidiEventType.Text <= stats.Type && stats.Type <= MidiEventType.Text_EnumLimit)
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
                            // Range shifts are instant, so manipulation of the duration is pointless
                            vocalTrack.SpecialPhrases
                                .GetLastOrAppend(position)
                                .TryAdd(SpecialPhraseType.RangeShift, (default(DualTime), 100));
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
