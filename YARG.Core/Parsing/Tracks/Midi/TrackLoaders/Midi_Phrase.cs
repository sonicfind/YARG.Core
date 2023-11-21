using System.Collections.Generic;
using System.Linq;
using YARG.Core.IO;

namespace YARG.Core.Parsing.Midi
{
    public struct Midi_Phrase
    {
        public readonly SpecialPhraseType[] Types;
        public long position;
        public int velocity;
        public Midi_Phrase(params SpecialPhraseType[] types)
        {
            Types = types;
            position = -1;
            velocity = 0;
        }
    }

    public class Midi_PhraseList
    {
        private readonly (int[], Midi_Phrase)[] _phrases;
        public Midi_PhraseList(params (int[], Midi_Phrase)[] phrases) { _phrases = phrases; }

        public bool AddPhrase(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, long position, MidiNote note)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                foreach (int val in _phrases[i].Item1)
                {
                    if (val == note.value)
                    {
                        phrases.Get_Or_Add_Last(position);
                        _phrases[i].Item2.position = position;
                        _phrases[i].Item2.velocity = note.velocity;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AddPhrase_Off(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, long position, MidiNote note)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                foreach (int val in _phrases[i].Item1)
                {
                    if (val == note.value)
                    {
                        ref var phr = ref _phrases[i].Item2;
                        if (phr.position != -1)
                        {
                            foreach (var type in phr.Types)
                                phrases.Traverse_Backwards_Until(phr.position).TryAdd(type, new SpecialPhraseInfo(position - phr.position, phr.velocity));
                            phr.position = -1;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AddPhrase(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, long position, SpecialPhraseType phraseToAdd, byte velocity)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                ref var phr = ref _phrases[i].Item2;
                if (phr.Types.Contains(phraseToAdd))
                {
                    phrases.Get_Or_Add_Last(position);
                    _phrases[i].Item2.position = position;
                    _phrases[i].Item2.velocity = velocity;
                    return true;
                }
            }
            return false;
        }

        public bool AddPhrase_Off(ref TimedManagedFlatDictionary<Dictionary<SpecialPhraseType, SpecialPhraseInfo>> phrases, long position, SpecialPhraseType phraseToAdd)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                ref var phr = ref _phrases[i].Item2;
                if (phr.Types.Contains(phraseToAdd))
                {
                    if (phr.position != -1)
                    {
                        foreach (var type in phr.Types)
                            phrases.Traverse_Backwards_Until(phr.position).TryAdd(type, new SpecialPhraseInfo(position - phr.position, phr.velocity));
                        phr.position = -1;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
