using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Parsing.Vocal;

namespace YARG.Core.Parsing.Midi
{
    public class MidiVocalLoader : MidiTrackLoader<VocalTrack_FW>
    {
        internal static readonly int[] LYRICLINE_1 = { 105 };
        internal static readonly int[] LYRICLINE_2 = { 106 };
        internal static readonly int[] HARMONYLINE = { 0xFF };
        internal static readonly int[] RANGESHIFT = { 0 };
        internal static readonly int[] LYRICSHIFT = { 1 };

        private DualTime percussion = DualTime.Inactive;
        private DualTime vocal = DualTime.Inactive;
        private (DualTime, string) lyric = new(DualTime.Inactive, string.Empty);
        private readonly int index;

        private MidiVocalLoader(VocalTrack_FW track, int index)
            : base(track, new Midi_PhraseList(
                (LYRICLINE_1,                     new(SpecialPhraseType.LyricLine, SpecialPhraseType.FaceOff_Player1)),
                (LYRICLINE_2,                     new(SpecialPhraseType.LyricLine, SpecialPhraseType.FaceOff_Player2)),
                (MidiTrackLoader.OverdrivePhrase, new(SpecialPhraseType.StarPower)),
                (RANGESHIFT,                      new(SpecialPhraseType.RangeShift)),
                (LYRICSHIFT,                      new(SpecialPhraseType.LyricShift)),
                (HARMONYLINE,                     new(SpecialPhraseType.HarmonyLine))
            ))
        {
            this.index = index;
        }

        public static VocalTrack_FW LoadLeadVocals(YARGMidiTrack midiTrack, SyncTrack_FW sync)
        {
            MidiVocalLoader loader = new(new VocalTrack_FW(1), 0);
            return loader.Process(sync, midiTrack);
        }

        public static void LoadHarmonyVocals(VocalTrack_FW track, int index, YARGMidiTrack midiTrack, SyncTrack_FW sync)
        {
            MidiVocalLoader loader = new(track, index);
            loader.Process(sync, midiTrack);
        }

        protected override void ParseNote_ON(YARGMidiTrack midiTrack)
        {
            if (IsNote(note.value))
                ParseVocal(note.value);
            else if (index == 0)
            {
                if (note.value == 96 || note.value == 97)
                    AddPercussion();
                else
                    AddPhrase(ref track.SpecialPhrases, note);
            }
            else if (index == 1)
            {
                if (note.value == 105 || note.value == 106)
                    AddHarmonyLine(ref track.SpecialPhrases);
            }
        }

        protected override void ParseNote_Off(YARGMidiTrack midiTrack)
        {
            if (IsNote(note.value))
                ParseVocal_Off(note.value);
            else if (index == 0)
            {
                if (note.value == 96)
                    AddPercussion_Off(true);
                else if (note.value == 97)
                    AddPercussion_Off(false);
                else
                    AddPhrase_Off(ref track.SpecialPhrases, note);
            }
            else if (index == 1)
            {
                if (note.value == 105 || note.value == 106)
                    AddHarmonyLine_Off(ref track.SpecialPhrases);
            }
        }

        protected override void ParseText(ReadOnlySpan<byte> str)
        {
            if (str.Length == 0)
                return;

            if (str[0] != '[')
            {
                if (lyric.Item1.ticks != -1)
                    AddVocal(lyric.Item1);

                lyric.Item1 = vocal.ticks != -1 ? vocal : position;
                lyric.Item2 = Encoding.UTF8.GetString(str);
            }
            else if (index == 0)
                track.Events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }

        private void ParseVocal(int pitch)
        {
            if (vocal.ticks != -1 && lyric.Item1.ticks != -1)
            {
                var duration = position - vocal;
                if (duration.ticks > 240)
                {
                    long newticks = duration.ticks - 120;
                    duration.seconds = (newticks * duration.seconds / duration.ticks);
                    duration.ticks = newticks;
                }
                else
                {
                    duration.ticks /= 2;
                    duration.seconds /= 2;
                }

                ref var note = ref AddVocal(vocal);
                note.Pitch.Binary = pitch;
                note.Duration = new NormalizedDuration(duration);
                lyric.Item1.ticks = -1;
                lyric.Item2 = string.Empty;
            }

            vocal = position;
            if (lyric.Item1.ticks != -1)
                lyric.Item1 = position;
        }

        private void ParseVocal_Off(int pitch)
        {
            if (vocal.ticks != -1 && lyric.Item1.ticks != -1)
            {
                ref var note = ref AddVocal(vocal);
                note.Pitch.Binary = pitch;
                note.Duration = new NormalizedDuration(position - vocal);
                lyric.Item1.ticks = -1;
                lyric.Item2 = string.Empty;
            }
            vocal.ticks = -1;
        }

        private ref VocalNote_FW AddVocal(in DualTime vocalPos)
        {
            var vocals = track[index];
            if (vocals.Capacity == 0)
                vocals.Capacity = 500;

            return ref vocals.Add(vocalPos, new(lyric.Item2));
        }

        public void AddPercussion()
        {
            percussion = position;
        }

        private void AddPercussion_Off(bool playable)
        {
            if (percussion.ticks != -1)
            {
                track.Percussion.Get_Or_Add_Last(percussion).IsPlayable = playable;
                percussion.ticks = -1;
            }
        }

        private void AddPhrase(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, MidiNote note)
        {
            this.phrases.AddPhrase(ref phrases, position, note);
        }

        private void AddPhrase_Off(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, MidiNote note)
        {
            this.phrases.AddPhrase_Off(ref phrases, position, note);
        }

        private void AddHarmonyLine(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases)
        {
            this.phrases.AddPhrase(ref phrases, position, SpecialPhraseType.HarmonyLine, 100);
        }

        private void AddHarmonyLine_Off(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases)
        {
            this.phrases.AddPhrase_Off(ref phrases, position, SpecialPhraseType.HarmonyLine);
        }

        private static bool IsNote(int value) { return 36 <= value && value <= 84; }
    }
}
