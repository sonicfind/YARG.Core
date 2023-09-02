using System.Collections.Generic;
using YARG.Core.IO;

namespace YARG.Core.Chart
{
    public struct Midi_Phrase
    {
        public readonly SpecialPhraseType type;
        public long position;
        public int velocity;
        public Midi_Phrase(SpecialPhraseType type)
        {
            this.type = type;
            position = -1;
            velocity = 0;
        }
    }

    public class Midi_PhraseList
    {
        private readonly (int[], Midi_Phrase)[] _phrases;
        public Midi_PhraseList((int[], Midi_Phrase)[] phrases) { _phrases = phrases; }

        public bool AddPhrase(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, long position, MidiNote note)
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

        public bool AddPhrase_Off(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, long position, MidiNote note)
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
                            phrases.Traverse_Backwards_Until(phr.position).Add(new(phr.type, position - phr.position, phr.velocity));
                            phr.position = -1;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AddPhrase(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, long position, SpecialPhraseType type, byte velocity)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                ref var phr = ref _phrases[i].Item2;
                if (phr.type == type)
                {
                    phrases.Get_Or_Add_Last(position);
                    _phrases[i].Item2.position = position;
                    _phrases[i].Item2.velocity = velocity;
                    return true;
                }
            }
            return false;
        }

        public bool AddPhrase_Off(ref TimedFlatDictionary<List<SpecialPhrase_FW>> phrases, long position, SpecialPhraseType type)
        {
            for (int i = 0; i < _phrases.Length; ++i)
            {
                ref var phr = ref _phrases[i].Item2;
                if (phr.type == type)
                {
                    if (phr.position != -1)
                    {
                        phrases.Traverse_Backwards_Until(phr.position).Add(new(phr.type, position - phr.position, phr.velocity));
                        phr.position = -1;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
