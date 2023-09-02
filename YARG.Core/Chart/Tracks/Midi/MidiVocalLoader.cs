using System;
using System.Collections.Generic;
using System.Text;
using YARG.Core.IO;
using YARG.Core.Chart.FlatDictionary;

namespace YARG.Core.Chart
{
    public class MidiVocalLoader : MidiLoader<VocalTrack_FW>
    {
        internal static readonly int[] LYRICLINE = { 105, 106 };
        internal static readonly int[] HARMONYLINE = { 0xFF };
        internal static readonly int[] RANGESHIFT = { 0 };
        internal static readonly int[] LYRICSHIFT = { 1 };

        private long percussion = -1;
        private long vocal = -1;
        private (long, string) lyric = new(-1, string.Empty);
        private readonly int index;

        private MidiVocalLoader(VocalTrack_FW track, int index) : base(track, new(new (int[], Midi_Phrase)[] {
                (LYRICLINE,       new(SpecialPhraseType.LyricLine)),
                (IMidLoader.overdrivePhrase, new(SpecialPhraseType.StarPower)),
                (RANGESHIFT,      new(SpecialPhraseType.RangeShift)),
                (LYRICSHIFT,      new(SpecialPhraseType.LyricShift)),
                (HARMONYLINE,     new(SpecialPhraseType.HarmonyLine)),
            }))
        {
            this.index = index;
        }

        public static VocalTrack_FW LoadLeadVocals(YARGMidiTrack midiTrack)
        {
            MidiVocalLoader loader = new(new VocalTrack_FW(1), 0);
            return loader.Process(midiTrack);
        }

        public static void LoadHarmonyVocals(VocalTrack_FW track, int index, YARGMidiTrack midiTrack)
        {
            MidiVocalLoader loader = new(track, index);
            loader.Process(midiTrack);
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
                    AddPhrase(ref track.specialPhrases, note);
            }
            else if (index == 1)
            {
                if (note.value == 105 || note.value == 106)
                    AddHarmonyLine(ref track.specialPhrases);
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
                    AddPhrase_Off(ref track.specialPhrases, note);
            }
            else if (index == 1)
            {
                if (note.value == 105 || note.value == 106)
                    AddHarmonyLine_Off(ref track.specialPhrases);
            }
        }

        protected override void ParseText(ReadOnlySpan<byte> str)
        {
            if (str.Length == 0)
                return;

            if (str[0] != '[')
            {
                if (lyric.Item1 != -1)
                    AddVocal(lyric.Item1);

                lyric.Item1 = vocal != -1 ? vocal : position;
                lyric.Item2 = Encoding.UTF8.GetString(str);
            }
            else if (index == 0)
                track.events.Get_Or_Add_Last(position).Add(Encoding.UTF8.GetString(str));
        }

        private void ParseVocal(int pitch)
        {
            if (vocal != -1 && lyric.Item1 != -1)
            {
                long duration = position - vocal;
                if (duration > 240)
                    duration -= 120;
                else
                    duration /= 2;

                ref var note = ref AddVocal(vocal);
                note.Binary = pitch;
                note.duration = duration;
                lyric.Item1 = -1;
                lyric.Item2 = string.Empty;
            }

            vocal = position;
            if (lyric.Item1 != -1)
                lyric.Item1 = position;
        }

        private void ParseVocal_Off(int pitch)
        {
            if (vocal != -1 && lyric.Item1 != -1)
            {
                ref var note = ref AddVocal(vocal);
                note.Binary = pitch;
                note.duration = position - vocal;
                lyric.Item1 = -1;
                lyric.Item2 = string.Empty;
            }
            vocal = -1;
        }

        private ref Vocal AddVocal(long vocalPos)
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
            if (percussion != -1)
            {
                track.percussion.Get_Or_Add_Last(percussion).IsPlayable = playable;
                percussion = -1;
            }
        }

        private void AddPhrase(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, MidiNote note)
        {
            this.phrases.AddPhrase(ref phrases, position, note);
        }

        private void AddPhrase_Off(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, MidiNote note)
        {
            this.phrases.AddPhrase_Off(ref phrases, position, note);
        }

        private void AddHarmonyLine(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases)
        {
            this.phrases.AddPhrase(ref phrases, position, SpecialPhraseType.HarmonyLine, 100);
        }

        private void AddHarmonyLine_Off(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases)
        {
            this.phrases.AddPhrase_Off(ref phrases, position, SpecialPhraseType.HarmonyLine);
        }

        private static bool IsNote(int value) { return 36 <= value && value <= 84; }
    }
}
