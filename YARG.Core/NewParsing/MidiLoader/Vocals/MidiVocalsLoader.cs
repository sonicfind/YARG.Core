using System;
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
            var vocals = vocalTrack[trackIndex];
            if (vocals.Count > 0 || (trackIndex == 0 && vocalTrack.SpecialPhrases.Count + vocalTrack.Percussion.Count > 0))
            {
                return false;
            }

            // In the case of bad rips with overlaps, we need this to apply the correct pitch
            // instead of using `note.value`
            int vocalPitch = 0;
            var vocalPosition = DualTime.Inactive;
            bool lyricApplied = false;

            var percussionPosition = DualTime.Inactive;

            var phrasePosition = DualTime.Inactive;
            var overdrivePosition = DualTime.Inactive;
            var rangeShiftPosition = DualTime.Inactive;
            var lyricShiftPosition = DualTime.Inactive;

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
                            if (vocalPosition.Ticks > -1 && lyricApplied)
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

                                ref var vocalNote = ref vocals.Last();
                                vocalNote.Duration = DualTime.Normalize(position - vocalPosition);
                                if (note.value != GH_TALKIE)
                                {
                                    vocalNote.Pitch.Binary = vocalPitch;
                                }
                                else if (vocalNote.TalkieState == TalkieState.None)
                                {
                                    vocalNote.Pitch.Reset();
                                    vocalNote.TalkieState = TalkieState.Talkie;
                                }
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
                                vocalTrack.SpecialPhrases.GetLastOrAppend(position);
                            }
                        }
                        // Only lead vocals (PART VOCALS & HARM_1) should handle the below values
                        else if (trackIndex == 0)
                        {
                            if (note.value == MidiLoader_Constants.OVERDRIVE)
                            {
                                overdrivePosition = position;
                                vocalTrack.SpecialPhrases.GetLastOrAppend(position);
                            }
                            else if (note.value == PERCUSSION_NOTE || note.value == PERCUSSION_NOISE)
                            {
                                percussionPosition = position;
                            }
                            else if (note.value == RANGESHIFT)
                            {
                                rangeShiftPosition = position;
                                vocalTrack.SpecialPhrases.GetLastOrAppend(position);
                            }
                            else if (note.value == LYRICSHIFT)
                            {
                                lyricShiftPosition = position;
                                vocalTrack.SpecialPhrases.GetLastOrAppend(position);
                            }
                        }
                    }
                    // NoteOff from this point
                    else
                    {
                        if ((VOCAL_MIN <= note.value && note.value <= VOCAL_MAX) || note.value == GH_TALKIE)
                        {
                            if (vocalPosition.Ticks > -1 && lyricApplied)
                            {
                                ref var vocalNote = ref vocals.Last();
                                vocalNote.Duration = DualTime.Normalize(position - vocalPosition);
                                if (note.value != GH_TALKIE)
                                {
                                    vocalNote.Pitch.Binary = note.value;
                                }
                                else if (vocalNote.TalkieState == TalkieState.None)
                                {
                                    vocalNote.Pitch.Reset();
                                    vocalNote.TalkieState = TalkieState.Talkie;
                                }
                            }
                            vocalPosition.Ticks = -1;
                            lyricApplied = false;
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

                        if (vocals.Capacity == 0)
                        {
                            vocals.Capacity = 500;
                        }

                        var state = TalkieState.None;
                        if (lyric.Length > 0)
                        {
                            state = lyric[^1] switch
                            {
                                '#' or '*' => TalkieState.Talkie,
                                '^'        => TalkieState.Lenient,
                                _          => TalkieState.None,
                            };
                        }

                        ref var vocalNote = ref vocals.Append(position);
                        vocalNote.Lyric = lyric;
                        vocalNote.TalkieState = state;
                        lyricApplied = true;
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
                            var ev = Encoding.ASCII.GetString(str);
                            vocalTrack.Events
                                .GetLastOrAppend(position)
                                .Add(ev);
                        }
                    }
                }
            }
            return true;
        }
    }
}
