using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YARG.Core.IO;

namespace YARG.Core.NewParsing.Midi
{
    using SortedPhraseList = YARGManagedSortedList<DualTime, Dictionary<SpecialPhraseType, SpecialPhraseInfo>>;

    public abstract class MidiTrackLoader
    {
        internal static readonly byte[] SYSEXTAG = Encoding.ASCII.GetBytes("PS");
        internal static Midi_PhraseMapping OverdrivePhrase = new(new[] { 116 }, SpecialPhraseType.StarPower);

        public static void SetMultiplierNote(int note)
        {
            OverdrivePhrase.MidiValues[0] = note;
        }

        internal DualTime Position = default;
        internal MidiNote Note = default;

        internal bool AddPhrase_ON(Midi_PhraseMapping[] mappings, SortedPhraseList phrases)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var map = ref mappings[i];
                foreach (int val in map.MidiValues)
                {
                    if (val == Note.value)
                    {
                        phrases.GetLastOrAppend(Position);
                        map.Phrase.Position = Position;
                        map.Phrase.Velocity = Note.velocity;
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool AddPhrase_Off(Midi_PhraseMapping[] mappings, SortedPhraseList phrases)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var map = ref mappings[i];
                foreach (int val in map.MidiValues)
                {
                    if (val == Note.value)
                    {
                        ref var phr = ref map.Phrase;
                        if (phr.Position.Ticks != -1)
                        {
                            foreach (var type in phr.Types)
                            {
                                phrases.TraverseBackwardsUntil(phr.Position)
                                       .TryAdd(type, new SpecialPhraseInfo(Position - phr.Position, phr.Velocity));
                            }
                            phr.Position.Ticks = -1;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool AddPhrase_ON(Midi_PhraseMapping[] mappings, SortedPhraseList phrases, SpecialPhraseType phraseToAdd, byte velocity)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var phr = ref mappings[i].Phrase;
                if (phr.Types.Contains(phraseToAdd))
                {
                    phrases.GetLastOrAppend(Position);
                    phr.Position = Position;
                    phr.Velocity = velocity;
                    return true;
                }
            }
            return false;
        }

        internal bool AddPhrase_Off(Midi_PhraseMapping[] mappings, SortedPhraseList phrases, SpecialPhraseType phraseToAdd)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                ref var phr = ref mappings[i].Phrase;
                if (phr.Types.Contains(phraseToAdd))
                {
                    if (phr.Position.Ticks != -1)
                    {
                        foreach (var type in phr.Types)
                        {
                            phrases.TraverseBackwardsUntil(phr.Position)
                                   .TryAdd(type, new SpecialPhraseInfo(Position - phr.Position, phr.Velocity));
                        }
                        phr.Position.Ticks = -1;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
