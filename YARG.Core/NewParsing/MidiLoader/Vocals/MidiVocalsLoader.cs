using System;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    public class MidiVocalsLoader : MidiTrackLoader
    {
        public static VocalTrack2 LoadLeadVocals(YARGMidiTrack midiTrack, SyncTrack2 sync)
        {
            var vocalsTrack = new VocalTrack2(1);
            LoadVocalTrack(midiTrack, sync, vocalsTrack, 0);
            return vocalsTrack;
        }

        public static void LoadVocalTrack(YARGMidiTrack midiTrack, SyncTrack2 sync, VocalTrack2 track, int index)
        {
            var loader = new MidiVocalsLoader(track, index);
            int tempoIndex = 0;
            while (midiTrack.ParseEvent(true))
            {
                loader.Position.Ticks = midiTrack.Position;
                loader.Position.Seconds = sync.ConvertToSeconds(midiTrack.Position, ref tempoIndex);
                if (midiTrack.Type == MidiEventType.Note_On)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    if (loader.Note.velocity > 0)
                    {
                        loader.ParseNote_ON();
                    }
                    else
                    {
                        loader.ParseNote_Off();
                    }
                }
                else if (midiTrack.Type == MidiEventType.Note_Off)
                {
                    midiTrack.ExtractMidiNote(ref loader.Note);
                    loader.ParseNote_Off();
                }
                else if (MidiEventType.Text <= midiTrack.Type && midiTrack.Type <= MidiEventType.Text_EnumLimit)
                {
                    loader.ParseText(midiTrack.ExtractTextOrSysEx());
                }
            }
            track.TrimExcess();
        }

        private static readonly Midi_PhraseMapping LYRICLINE_1 = new(new[] { 105 },  SpecialPhraseType.LyricLine );
        private static readonly Midi_PhraseMapping LYRICLINE_2 = new(new[] { 106 },  SpecialPhraseType.LyricLine);
        private static readonly Midi_PhraseMapping HARMONYLINE = new(new[] { 0xFF }, SpecialPhraseType.HarmonyLine);
        private static readonly Midi_PhraseMapping RANGESHIFT =  new(new[] { 0 },    SpecialPhraseType.RangeShift);
        private static readonly Midi_PhraseMapping LYRICSHIFT =  new(new[] { 1 },    SpecialPhraseType.LyricShift);

        private DualTime _percussion = DualTime.Inactive;
        private DualTime _vocal = DualTime.Inactive;
        private (DualTime, string) _lyric = new(DualTime.Inactive, string.Empty);
        private VocalTrack2 _track;
        private readonly int _index;

        private readonly Midi_PhraseMapping[] PhraseMappings = new Midi_PhraseMapping[]
        {
            OverdrivePhrase, LYRICLINE_1, LYRICLINE_2, HARMONYLINE, RANGESHIFT, LYRICSHIFT
        };

        private MidiVocalsLoader(VocalTrack2 track, int index)
        {
            _track = track;
            _index = index;
        }

        private void ParseNote_ON()
        {
            if (36 <= Note.value && Note.value <= 84)
            {
                ParseVocal();
            }
            else if (_index == 0)
            {
                if (Note.value == 96 || Note.value == 97)
                {
                    _percussion = Position;
                }
                else
                {
                    AddPhrase_ON(PhraseMappings, _track.SpecialPhrases);
                }
            }
            else if (_index == 1)
            {
                if (Note.value == 105 || Note.value == 106)
                {
                    AddPhrase_ON(PhraseMappings, _track.SpecialPhrases, SpecialPhraseType.HarmonyLine, 100);
                }
            }
        }

        private void ParseNote_Off()
        {
            if (36 <= Note.value && Note.value <= 84)
            {
                ParseVocal_Off();
            }
            else if (_index == 0)
            {
                if (Note.value == 96)
                {
                    AddPercussion_Off(true);
                }
                else if (Note.value == 97)
                {
                    AddPercussion_Off(false);
                }
                else
                {
                    AddPhrase_Off(PhraseMappings, _track.SpecialPhrases);
                }
            }
            else if (_index == 1)
            {
                if (Note.value == 105 || Note.value == 106)
                {
                    AddPhrase_Off(PhraseMappings, _track.SpecialPhrases, SpecialPhraseType.HarmonyLine);
                }
            }
        }

        private void ParseText(ReadOnlySpan<byte> str)
        {
            if (str.Length == 0)
                return;

            if (str[0] != '[')
            {
                if (_lyric.Item1.Ticks != -1)
                {
                    AddVocal(_lyric.Item1);
                }

                _lyric.Item1 = _vocal.Ticks != -1 ? _vocal : Position;
                _lyric.Item2 = Encoding.UTF8.GetString(str);
            }
            else if (_index == 0)
            {
                _track.Events.GetLastOrAppend(Position).Add(Encoding.UTF8.GetString(str));
            }
        }

        private void ParseVocal()
        {
            if (_vocal.Ticks != -1 && _lyric.Item1.Ticks != -1)
            {
                var duration = Position - _vocal;
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

                ref var note = ref AddVocal(_vocal);
                note.Pitch.Binary = Note.value;
                note.Duration = DualTime.Normalize(duration);
                _lyric.Item1.Ticks = -1;
                _lyric.Item2 = string.Empty;
            }

            _vocal = Position;
            if (_lyric.Item1.Ticks != -1)
            {
                _lyric.Item1 = Position;
            }
        }

        private void ParseVocal_Off()
        {
            if (_vocal.Ticks != -1 && _lyric.Item1.Ticks != -1)
            {
                ref var note = ref AddVocal(_vocal);
                note.Pitch.Binary = Note.value;
                note.Duration = DualTime.Normalize(Position - _vocal);
                _lyric.Item1.Ticks = -1;
                _lyric.Item2 = string.Empty;
            }
            _vocal.Ticks = -1;
        }

        private ref VocalNote2 AddVocal(in DualTime vocalPos)
        {
            var vocals = _track[_index];
            if (vocals.Capacity == 0)
            {
                vocals.Capacity = 500;
            }

            return ref vocals.Append(vocalPos, new VocalNote2() { Lyric = _lyric.Item2 });
        }


        private void AddPercussion_Off(bool playable)
        {
            if (_percussion.Ticks != -1)
            {
                _track.Percussion.GetLastOrAppend(_percussion).IsPlayable = playable;
                _percussion.Ticks = -1;
            }
        }
    }
}
